using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using jsreport.Client;
using Simple.OData.Client;

namespace jsreport.Embedded
{
    public class EmbeddedReportingServer
    {
        private readonly long _port;
        private bool _stopped;
        private bool _stopping;

        public EmbeddedReportingServer(long port = 2000)
        {
            _port = port;
            EmbeddedServerUri = "http://localhost:" + port;
            PingTimeout = new TimeSpan(0,0,0,120);
        }

        public bool ManualLifeControll { get; set; }

        public TimeSpan PingTimeout { get; set; }

        public string BinPath { get; set; }

        public string EmbeddedServerUri { get; set; }

        public Process Worker { get; set; }

        public string AssemblyDirectory
        {
            get
            {
                if (BinPath != null)
                    return BinPath;

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public async Task<object> StartAsync()
        {
            if (!File.Exists(Path.Combine(AssemblyDirectory, "jsreport-net-embedded", "server.js")))
            {
                Decompress();
            }

            StartWorker();

            return await StartPingingWrapperProcessAsync().ConfigureAwait(false);
        }

        public async Task<object> StopAsync()
        {
            var done = false;

            //there are some issues when killing child process started from visual studio
            //we rather stop pinging jsreport server and let it die for timeout
            //Worker.Kill();

            var tcs = new TaskCompletionSource<object>();

            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        client.GetAsync("/api/alive").Wait();
                    }
                    catch (Exception e)
                    {
                        _stopped = true;
                        if (!done)
                        {
                            done = true;
                            _stopping = false;
                            tcs.SetResult(new object());
                        }

                        return;
                    }
                    Thread.Sleep(500);
                }
            });

            _stopping = true;
            return await tcs.Task.ConfigureAwait(false);
        }

        private void StartWorker()
        {
            Worker = new Process();
            Worker.StartInfo.FileName = Path.Combine(AssemblyDirectory, "node.exe");
            Worker.StartInfo.WorkingDirectory = AssemblyDirectory;
            Worker.StartInfo.Arguments = "jsreport-net-embedded/server.js " + "--httpPort=" + _port + " --pingTimeout=" + PingTimeout.TotalSeconds;
            Worker.StartInfo.UseShellExecute = false;
            Worker.StartInfo.CreateNoWindow = true;
            Worker.Start();
        }

        private async Task<object> StartPingingWrapperProcessAsync()
        {
            _stopped = false;
            var done = false;
            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);

            var tcs = new TaskCompletionSource<object>();

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (!_stopped && !_stopping)
                        {
                            var response = client.GetAsync("/api/alive").Result;
                            response.EnsureSuccessStatusCode();

                            if (!done)
                            {
                                done = true;
                                //hread.Sleep(1000);
                                tcs.SetResult(new object());
                            }
                          
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(500);
                    }
                }
            });

            return await tcs.Task.ConfigureAwait(false);
        }

        private void Decompress()
        {
            var fileToDecompress = new FileInfo(Path.Combine(AssemblyDirectory, "jsreport-net-embedded.zip"));

            if (!fileToDecompress.Exists)
                throw new InvalidOperationException(fileToDecompress.FullName + " file not found.");

            var dirWithJsReportContent =
                new DirectoryInfo(Path.Combine(fileToDecompress.Directory.FullName, "jsreport-net-embedded"));
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

        public async Task<object> SynchronizeLocalTemplatesAsync()
        {
            string path = AssemblyDirectory;

            ODataClient client = CreateODataClient();

            foreach (string reportFilePath in Directory.GetFiles(path, "*.jsrep", SearchOption.AllDirectories))
            {
                string reportName = Path.GetFileNameWithoutExtension(reportFilePath);

                string content = File.ReadAllText(reportFilePath + ".html");
                string helpers = File.ReadAllText(reportFilePath + ".js");


                var serializer = new XmlSerializer(typeof (ReportDefinition));
                var reportDefinition = serializer.Deserialize(new StreamReader(reportFilePath)) as ReportDefinition;

                Template template = await ((Task<Template>)client.For<Template>().Filter(x => x.name == reportName).FindEntryAsync()).ConfigureAwait(false);

                dynamic operation = new ExpandoObject();
                operation.name = reportName;
                operation.shortid = reportName;
                operation.engine = reportDefinition.Engine;
                operation.recipe = reportDefinition.Recipe;
                operation.content = content;
                operation.helpers = helpers;

                if (reportDefinition.Phantom != null && reportDefinition.Phantom.IsDirty)
                {
                    operation.phantom = new ExpandoObject() as dynamic;
                    if (reportDefinition.Phantom.Margin != null)
                        operation.phantom.margin = reportDefinition.Phantom.Margin;
                    
                    if (reportDefinition.Phantom.Header != null)
                        operation.phantom.header = reportDefinition.Phantom.Header;
                    
                    if (reportDefinition.Phantom.HeaderHeight != null)
                        operation.phantom.headerHeight = reportDefinition.Phantom.HeaderHeight;
                    
                    if (reportDefinition.Phantom.Footer != null)
                        operation.phantom.footer = reportDefinition.Phantom.Footer;
                    
                    if (reportDefinition.Phantom.FooterHeight != null)
                        operation.phantom.footerHeight = reportDefinition.Phantom.FooterHeight;
                    
                    if (reportDefinition.Phantom.Orientation != null)
                        operation.phantom.orientation = reportDefinition.Phantom.Orientation;
                    
                    if (reportDefinition.Phantom.Format != null)
                        operation.phantom.format = reportDefinition.Phantom.Format;
                    
                    if (reportDefinition.Phantom.Width != null)
                        operation.phantom.width = reportDefinition.Phantom.Width;
                    
                    if (reportDefinition.Phantom.Height != null)
                        operation.phantom.height = reportDefinition.Phantom.Height;
                }

                if (template == null)
                {
                    await ((Task)client.For<Template>().Set(operation).InsertEntryAsync()).ConfigureAwait(false);
                }
                else
                {
                    operation._id = template._id;
                    await ((Task)client.For<Template>().Filter(x => x.name == reportName).Set(operation).UpdateEntryAsync()).ConfigureAwait(false);
                }
            }

            return new object();
        }

        public void CleanServerData()
        {
            string dataPath = Path.Combine(AssemblyDirectory, "data");
            if (Directory.Exists(dataPath))
                Directory.Delete(dataPath, true);
        }

        public ODataClient CreateODataClient()
        {
            return new ODataClient(new ODataClientSettings
            {
                UrlBase = EmbeddedServerUri + "/odata",
            });
        }
    }
}