using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using jsreport.Client;

namespace jsreport.Embedded
{
    /// <summary>
    ///     Class able to start jsreport nodejs server allong with .net process, synchronize local templates with it and mange it's lifecycle
    /// </summary>
    public class EmbeddedReportingServer : IEmbeddedReportingServer
    {
        public const string PACKAGE_VERSION = "0.8.1";
        private readonly long _port;
        private bool _disposed;
        private bool _stopped = true;
        private bool _stopping;
        
        public EmbeddedReportingServer(long port = 2000)
        {
            _port = port;
            EmbeddedServerUri = "http://localhost:" + port;
            StartTimeout = new TimeSpan(0, 0, 0, 20);
            PingTimeout = new TimeSpan(0, 0, 0, 30);
            StopTimeout = new TimeSpan(0, 0, 0, 3);

            AppDomain.CurrentDomain.DomainUnload += DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit += DomainUnloadOrProcessExit;
        }

        /// <summary>
        ///     Visual Studio prevents from properly killing jsreport server when debugging ends.
        ///     To avoid orphans of nodejs processes jsreport server kills itself when no ping is comming from .NET process during debug.
        ///     EmbeddedReportingServer takes care of sending regular ping to jsreport server.
        ///     PingTimeout specifies time how to keep jsreport nodejs process runing when no ping is comming from .NET
        /// </summary>
        public TimeSpan PingTimeout { get; set; }


        //used in visual studio tools to override AssemblyDirectory, because there we get some visual studio location folder...
        public string BinPath { get; set; }

        public Process Worker { get; set; }

        public TimeSpan StartTimeout { get; set; }
        public TimeSpan StopTimeout { get; set; }
        public object Configuration { get; set; }

        public string AssemblyDirectory
        {
            get
            {
                if (BinPath != null)
                    return BinPath;

                string codeBase = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
                return Path.GetDirectoryName(codeBase);
            }
        }

    
        public string ServerStandardOutput { get; set; }
        public string ServerErrorOutput { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }

        private IReportingService _reportingService;
        /// <summary>
        ///     Shortcut to new ReportingService(EmbeddedServerUri)
        /// </summary>
        public IReportingService ReportingService
        {
            get { return _reportingService = _reportingService ?? new ReportingService(EmbeddedServerUri)
                {
                    ReportsDirectory = AssemblyDirectory,
                    Username = Username,
                    Password = Password
                }; }
        }

        /// <summary>
        ///     Full uri to running jsreport server like http://localhost:2000/
        /// </summary>
        public string EmbeddedServerUri { get; set; }

        /// <summary>
        ///     Relative path (from bin) to directory where the jsreport server should be exreacted  and where it should run
        ///     You want to use something like ../App_Data for web applications and just null for other types of applications
        ///     where jsreport can stay in bin folder
        /// </summary>
        public string RelativePathToServer { get; set; }

        /// <summary>
        ///     Takes precedence over RelativePathToServer and specifies directory where jsreport server should be extracted and run
        /// </summary>
        public string AbsolutePathToServer { get; set; }

        /// <summary>
        ///     Extracts jsreport-net-embedded.zip into path to server directory, starts jsreport using nodejs from bin folder
        /// </summary>
        public async Task StartAsync()
        {
            InitializeServerPath();
            await StopAsync().ConfigureAwait(false);

            Decompress();

            StartWorker();

            await WaitForStarted().ConfigureAwait(false);
        }



        /// <summary>
        ///     Sends kill signal to jsreport server and wait for it's exit
        /// </summary>
        public async Task StopAsync()
        {
            bool done = false;
            _stopping = true;

            //there are some issues when killing child process started from visual studio
            //we send signal throught custom extension and wait until ping will timout

            var tcs = new TaskCompletionSource<object>();


            try
            {
                List<Process> alreadyRunningProcess =
                    Process.GetProcessesByName("node")
                           .Where(p => GetMainModuleFilePath(p.Id) == Path.Combine(AbsolutePathToServer, "node.exe"))
                           .ToList();

                if (alreadyRunningProcess.Any())
                {
                    alreadyRunningProcess.ForEach(p => p.Kill());
                }
            }
            catch (Exception e)
            {
            }

            var client = CreateClient();
            client.Timeout = new TimeSpan(0, 0, 0, 0, 500);

            var timeoutSw = new Stopwatch();
            timeoutSw.Start();

            Task.Run(() =>
                {
                    while (true)
                    {
                        if (!done && timeoutSw.Elapsed > StopTimeout)
                        {
                            tcs.SetResult(new object());
                        }

                        try
                        {
                            HttpResponseMessage response = client.GetAsync("/api/alive").Result;
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception e)
                        {
                            if (!done)
                            {
                                _stopped = true;
                                _stopping = false;
                                done = true;
                                tcs.SetResult(new object());
                            }

                            return;
                        }
                        Thread.Sleep(500);
                    }
                });

            await tcs.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            AppDomain.CurrentDomain.DomainUnload -= DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit -= DomainUnloadOrProcessExit;

            if (!_stopped && !_stopping)
                StopAsync().Wait();

            _disposed = true;
        }

        private void InitializeServerPath()
        {
            if (RelativePathToServer == null)
            {
                if (IsWebApp(AppDomain.CurrentDomain))
                {
                    RelativePathToServer = Path.Combine("../App_Data/jsreport", "app");
                }
                else
                {
                    RelativePathToServer = Path.Combine("jsreport", "app");
                }
            }

            if (AbsolutePathToServer == null)
            {
                AbsolutePathToServer = Path.GetFullPath(Path.Combine(AssemblyDirectory, RelativePathToServer));
            }

            if (!Directory.Exists(AbsolutePathToServer))
            {
                Directory.CreateDirectory(AbsolutePathToServer);
            }
        }

        private string GetMainModuleFilePath(int processId)
        {
            string wmiQueryString = "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = " + processId;
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            {
                using (ManagementObjectCollection results = searcher.Get())
                {
                    ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
                    if (mo != null)
                    {
                        return mo["ExecutablePath"] == null ? "" : (string) mo["ExecutablePath"];
                    }
                }
            }
            return "";
        }

        private void StartWorker()
        {
            Worker = new Process()
            {
                StartInfo = new ProcessStartInfo(Path.Combine(AbsolutePathToServer, "node.exe"))
                {
                    FileName = Path.Combine(AbsolutePathToServer, "node.exe"),
                    WorkingDirectory = AbsolutePathToServer,
                    Arguments = "server.js " + " --httpPort=" + _port + SerializeConfiguration(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            Worker.StartInfo.EnvironmentVariables.Remove("NODE_ENV");
            Worker.StartInfo.EnvironmentVariables.Add("NODE_ENV", "production");

            Worker.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        ServerStandardOutput += e.Data;
                    }
                };
            Worker.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        ServerErrorOutput += e.Data;
                    }
                };

            Worker.Start();

            Worker.BeginOutputReadLine();
            Worker.BeginErrorReadLine();
        }

        private string SerializeConfiguration()
        {
            if (Configuration == null)
                return "";

            return InnerSerializeConfiguration(Configuration, "");
        }

        private string InnerSerializeConfiguration(object obj, string path)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            var result = "";
            foreach (PropertyInfo property in properties)
            {
                var value = property.GetValue(obj, null);

                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(Decimal) || property.PropertyType == typeof(String))
                {
                    result += " --";
                    if (!string.IsNullOrEmpty(path))
                    {
                        result += path + ":";
                    }
                    result += property.Name + "=" + value;
                }
                else
                {
                    result += InnerSerializeConfiguration(value,
                                                          string.IsNullOrEmpty(path)
                                                              ? property.Name
                                                              : (path + ":" + property.Name));
                }
            }

            return result;
        }

        private async Task WaitForStarted()
        {
            _stopping = false;
            _stopped = false;

            var timeoutSw = new Stopwatch();
            timeoutSw.Start();

            bool done = false;
            var client = CreateClient();

            var tcs = new TaskCompletionSource<object>();

            Task.Run(async () =>
                {
                    while (!done)
                    {
                        if (_stopping || _stopped)
                            return;

                        if (!done && timeoutSw.Elapsed > StartTimeout)
                        {
                            await StopAsync();
                            tcs.SetException(
                                new EmbeddedReportingServerException(
                                    "Failed to start jsreport server, output: " + ServerErrorOutput + ServerStandardOutput)
                                    {
                                        ServerErrorOutput = ServerErrorOutput,
                                        ServerStandardOutput = ServerStandardOutput
                                    });
                            return;
                        }

                        try
                        {
                            HttpResponseMessage response = client.GetAsync("/api/ping").Result;
                            response.EnsureSuccessStatusCode();

                            string res = response.Content.ReadAsStringAsync().Result;

                            if (!done && ServerStandardOutput.Contains("successfully started"))
                            {
                                done = true;
                                tcs.SetResult(new object());
                            }
                        }
                        catch (Exception)
                        {
                            //waiting for server to startup
                        }

                        Thread.Sleep(500);
                    }
                });

            await tcs.Task.ConfigureAwait(false);
       }

        private HttpClient CreateClient()
        {
            var client = new HttpClient() { BaseAddress = new Uri(EmbeddedServerUri) };

            if (Username != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(String.Format("{0}:{1}", Username, Password))));
            }

            return client;
        }

        private void Decompress()
        {
            var zippedFromSolution =
                new FileInfo(Path.Combine(AssemblyDirectory, "jsreport", "jsreport.zip"));
            var zippedFromApp = new FileInfo(Path.Combine(AbsolutePathToServer, "jsreport.zip"));
            var isSame = zippedFromApp.Exists && zippedFromSolution.Exists &&
                             (zippedFromApp.Length == zippedFromSolution.Length);


            if (!Directory.Exists(Path.Combine(AbsolutePathToServer, "node_modules")) || !isSame)
            {
                DeleteDirectoryContent(AbsolutePathToServer);

                var fileToDecompress =
                    new FileInfo(Path.Combine(AssemblyDirectory, "jsreport", "jsreport.zip"));

                if (!fileToDecompress.Exists)
                    throw new InvalidOperationException(fileToDecompress.FullName + " file not found.");

                new DirectoryInfo(AbsolutePathToServer).Delete(true);
                ZipFile.ExtractToDirectory(fileToDecompress.FullName, AbsolutePathToServer);
            }

            //copy report templates
            var reportsInBin = new DirectoryInfo(Path.Combine(AssemblyDirectory, "jsreport", "reports"));
            var reportsInServer = new DirectoryInfo(Path.Combine(AbsolutePathToServer, "../reports"));
            if (reportsInBin.Exists && reportsInBin.FullName != reportsInServer.FullName)
            {
                CopyFilesRecursively(reportsInBin, reportsInServer);
            }

            //copy jsreport.zip for later comparing if there was a change
            foreach (FileInfo file in new DirectoryInfo(Path.Combine(AssemblyDirectory, "jsreport")).GetFiles())
            {
                file.CopyTo(Path.Combine(AbsolutePathToServer, file.Name), true);
            }

            //copy the config files, package.json and server.js
            foreach (FileInfo file in new DirectoryInfo(Path.Combine(AssemblyDirectory, "jsreport", "app")).GetFiles())
            {
                file.CopyTo(Path.Combine(AbsolutePathToServer, file.Name), true);
            }
        }

        private static bool IsWebApp(AppDomain appDomain)
        {
            var configFile = (string)appDomain.GetData("APP_CONFIG_FILE");
            if (string.IsNullOrEmpty(configFile)) return false;
            return (
                       Path.GetFileNameWithoutExtension(configFile) ?? string.Empty
                   ).Equals(
                       "WEB",
                       StringComparison.OrdinalIgnoreCase);
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        private void DeleteDirectoryContent(string path, bool skipMainDir = true)
        {
            if (!Directory.Exists(path))
                return;

            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectoryContent(directory, false);
            }

            try
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }
            catch (IOException)
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.EnumerateFiles(path).ToList().ForEach(File.Delete);
            }

            if (skipMainDir)
            {
                return;
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }
       

        private void DomainUnloadOrProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}