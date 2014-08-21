using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Simple.OData.Client;
using jsreport.Client;
using jsreport.Client.Entities;

namespace jsreport.Embedded
{
    /// <summary>
    /// Class able to start jsreport nodejs server allong with .net process, synchronize local templates with it and mange it's lifecycle
    /// </summary>
    public class EmbeddedReportingServer : IEmbeddedReportingServer
    {
        private readonly long _port;
        private bool _stopping;
        private bool _stopped = true;
        private bool _disposed;

        public EmbeddedReportingServer(long port = 2000)
        {
            _port = port;
            EmbeddedServerUri = "http://localhost:" + port;
            PingTimeout = new TimeSpan(0, 0, 0, 120);
            RelativePathToServer = "";
            StartTimeout = new TimeSpan(0, 0, 0, 10);

            AppDomain.CurrentDomain.DomainUnload += DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit += DomainUnloadOrProcessExit;
        }

        /// <summary>
        /// To avoid orphans of nodejs processes jsreport server kills itself when no ping is comming from .NET process.
        /// EmbeddedReportingServer takes care of sending regular ping to jsreport server.
        /// PingTimeout specifies time how to keep jsreport nodejs process runing when no ping is comming from .NET
        /// </summary>
        public TimeSpan PingTimeout { get; set; }

        /// <summary>
        /// Shortcut to new ReportingService(EmbeddedServerUri)
        /// </summary>
        public IReportingService ReportingService {
            get { return new ReportingService(EmbeddedServerUri) { ReportsDirectory = AssemblyDirectory }; }
        }

        //used in visual studio tools to override AssemblyDirectory, because there we get some visual studio location folder...
        public string BinPath { get; set; }

        /// <summary>
        /// Full uri to running jsreport server like http://localhost:2000/
        /// </summary>
        public string EmbeddedServerUri { get; set; }

        public Process Worker { get; set; }

        /// <summary>
        /// Relative path (from bin) to directory where the jsreport server should be exreacted  and where it should run
        /// You want to use something like ../App_Data for web applications and just null for other types of applications 
        /// where jsreport can stay in bin folder
        /// </summary>
        public string RelativePathToServer { get; set; }

        /// <summary>
        /// Takes precedence over RelativePathToServer and specifies directory where jsreport server should be extracted and run
        /// </summary>
        public string AbsolutePathToServer { get; set; }

        public TimeSpan StartTimeout { get; set; }

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

        /// <summary>
        /// Extracts jsreport-net-embedded.zip into path to server directory, starts jsreport using nodejs from bin folder
        /// </summary>
        public async Task StartAsync()
        {
            InitializeServerPath();

            if (!File.Exists(Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "server.js")))
            {
                Decompress();
            }

            StartWorker();

            await StartPingingWrapperProcessAsync().ConfigureAwait(false);
        }

        private void InitializeServerPath()
        {
            if (AbsolutePathToServer == null)
            {
                AbsolutePathToServer = Path.Combine(AssemblyDirectory, RelativePathToServer);
            }
        }

        /// <summary>
        /// Sends kill signal to jsreport server and wait for it's exit
        /// </summary>
        public async Task StopAsync()
        {
            bool done = false;
            _stopping = true;

            //there are some issues when killing child process started from visual studio
            //we send signal throught custom extension and wait until ping will timout
            //Worker.Kill();

            var tcs = new TaskCompletionSource<object>();

            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);
            client.Timeout = new TimeSpan(0, 0, 0, 0, 500);

            try
            {
                client.PostAsync("/api/kill", new StringContent("")).Wait();
            }
            catch (Exception e)
            {
            }

            Task.Run(() =>
                {
                    while (true)
                    {
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

        private void StartWorker()
        {
            Worker = new Process();
            Worker.StartInfo.FileName = Path.Combine(AssemblyDirectory, "node.exe");
            Worker.StartInfo.WorkingDirectory = Path.Combine(AbsolutePathToServer, "jsreport-net-embedded");
            Worker.StartInfo.Arguments = "server.js " + "--httpPort=" + _port + " --pingTimeout=" +
                                         PingTimeout.TotalSeconds;
            Worker.StartInfo.UseShellExecute = false;
            Worker.StartInfo.CreateNoWindow = true;
            Worker.Start();
        }

        private async Task StartPingingWrapperProcessAsync()
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
                    while (true)
                    {
                        if (_stopping || _stopped)
                            return;

                        if (!done && timeoutSw.Elapsed > StartTimeout)
                        {
                            await StopAsync();
                            tcs.SetException(new InvalidOperationException("Timeout during starting jsreport server."));
                            return;
                        }

                        try
                        {
                            HttpResponseMessage response = client.GetAsync("/api/alive").Result;
                            response.EnsureSuccessStatusCode();

                            var res = response.Content.ReadAsStringAsync().Result;

                            if (!done)
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
                dirWithJsReportContent.Delete(true);
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
    }
}