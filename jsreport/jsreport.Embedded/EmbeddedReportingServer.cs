using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
            RelativePathToServer = "";
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

        private IReportingService _reportingService;
        /// <summary>
        ///     Shortcut to new ReportingService(EmbeddedServerUri)
        /// </summary>
        public IReportingService ReportingService
        {
            get { return _reportingService = _reportingService ?? new ReportingService(EmbeddedServerUri) { ReportsDirectory = AssemblyDirectory }; }
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

            if (!File.Exists(Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "server.js")))
            {
                Decompress();
            }
            else
            {
                string packageFile = Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "package.json");
                if (JObject.Parse(File.ReadAllText(packageFile))["version"].Value<string>() != PACKAGE_VERSION)
                {
                    Decompress();
                }
            }

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
                           .Where(p => GetMainModuleFilePath(p.Id) == Path.Combine(AssemblyDirectory, "node.exe"))
                           .ToList();
                
                alreadyRunningProcess.AddRange(Process.GetProcessesByName("phantomjs")
                                                      .Where(
                                                          p =>
                                                          GetMainModuleFilePath(p.Id) == Path.GetFullPath(
                                                          Path.Combine(AbsolutePathToServer, "jsreport-net-embedded",
                                                                       "node_modules",
                                                                       "phantomjs", "lib", "phantom", "phantomjs.exe")))
                                                      .ToList());


                if (alreadyRunningProcess.Any())
                {
                    alreadyRunningProcess.ForEach(p => p.Kill());
                }
            }
            catch (Exception e)
            {
            }

            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);
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
            if (AbsolutePathToServer == null)
            {
                AbsolutePathToServer = Path.Combine(AssemblyDirectory, RelativePathToServer);
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
            Worker = new Process();
            Worker.StartInfo.FileName = Path.Combine(AssemblyDirectory, "node.exe");
            Worker.StartInfo.WorkingDirectory = Path.Combine(AbsolutePathToServer, "jsreport-net-embedded");
            Worker.StartInfo.Arguments = "server.js " + " --httpPort=" + _port + SerializeConfiguration() + 
                                         (Debugger.IsAttached
                                              ? (" --pingTimeout=" + PingTimeout.TotalSeconds)
                                              : "");
            Worker.StartInfo.UseShellExecute = false;
            Worker.StartInfo.CreateNoWindow = true;
            Worker.StartInfo.RedirectStandardOutput = true;
            Worker.StartInfo.RedirectStandardError = true;


            Worker.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        ServerStandardOutput += e.Data;
                    }
                };
            Worker.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
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
            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);

            var tcs = new TaskCompletionSource<object>();

            Task.Run(async () =>
                {
                    while (!done || Debugger.IsAttached)
                    {
                        if (_stopping || _stopped)
                            return;

                        if (!done && timeoutSw.Elapsed > StartTimeout)
                        {
                            await StopAsync();
                            tcs.SetException(
                                new EmbeddedReportingServerException(
                                    "Failed to start jsreport server. Examine ServerStandardOutput and ServerErrorOutput properties for details ")
                                    {
                                        ServerErrorOutput = ServerErrorOutput,
                                        ServerStandardOutput = ServerStandardOutput
                                    });
                            return;
                        }

                        try
                        {
                            HttpResponseMessage response = client.GetAsync("/api/alive").Result;
                            response.EnsureSuccessStatusCode();

                            string res = response.Content.ReadAsStringAsync().Result;

                            if (!done && ServerStandardOutput.Contains("successfully started"))
                            {
                                done = true;
                                tcs.SetResult(new object());
                            }
                        }
                        catch (Exception e)
                        {
                        }

                        Thread.Sleep(500);
                    }
                });

            await tcs.Task.ConfigureAwait(false);
        }

        private void Decompress()
        {
            var fileToDecompress = new FileInfo(Path.Combine(AssemblyDirectory, "jsreport-net-embedded.zip"));

            if (!fileToDecompress.Exists)
                throw new InvalidOperationException(fileToDecompress.FullName + " file not found.");

            var dirWithJsReportContent = new DirectoryInfo(Path.Combine(AbsolutePathToServer, "jsreport-net-embedded"));

            if (dirWithJsReportContent.Exists)
            {
                DeleteDirectory(dirWithJsReportContent.FullName);
            }

            try
            {
                ZipFile.ExtractToDirectory(fileToDecompress.FullName, dirWithJsReportContent.FullName);
            }
            catch (PathTooLongException e)
            {
                throw new PathTooLongException(
                    "It seems project hosting jsreport is too deep in the directory tree and hits limit for 256 chars long path when extracting jsreport embedded server.",
                    e);
            }
        }

        public void CleanServerData()
        {
            InitializeServerPath();

            string dataPath = Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "data");
            if (Directory.Exists(dataPath))
                Directory.Delete(dataPath, true);
        }

        private void DomainUnloadOrProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        public void DeleteDirectory(string path)
        {
            if (path == Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "data"))
                return;

            foreach (string directory in Directory.GetDirectories(path))
            {
                DeleteDirectory(directory);
            }

            if (path == Path.Combine(AbsolutePathToServer, "jsreport-net-embedded"))
            {
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
    }
}