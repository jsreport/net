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

            AppDomain.CurrentDomain.DomainUnload += DomainUnloadOrProcessExit;
            AppDomain.CurrentDomain.ProcessExit += DomainUnloadOrProcessExit;
        }

        /// <summary>
        /// To avoid orphans of nodejs processes jsreport server kills itself when no ping is comming from .NET process.
        /// EmbeddedReportingServer takes care of sending regular ping to jsreport server.
        /// PingTimeout specifies time how to keep jsreport nodejs process runing when no ping is comming from .NET
        /// </summary>
        public TimeSpan PingTimeout { get; set; }

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

            bool done = false;
            var client = new HttpClient();
            client.BaseAddress = new Uri(EmbeddedServerUri);

            var tcs = new TaskCompletionSource<object>();

            Task.Run(() =>
                {
                    while (true)
                    {
                        if (_stopping || _stopped)
                            return;

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

        private IEnumerable<string> ValidateUniquenes(IEnumerable<string> files)
        {
            var firstNonUnique = files.Select(Path.GetFileName).GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            
            if (firstNonUnique != null)
                throw new InvalidOperationException("Non unique item found during jsreprot synchronization: " + firstNonUnique);

            return files;
        }

        /// <summary>
        /// Synchronize all *.jsrep files into jsreport server including images and schema json files
        /// </summary>
        public async Task SynchronizeTemplatesAsync()
        {
            string path = AssemblyDirectory;

            ODataClient client = CreateODataClient();

            await SynchronizeImagesAsync(client, path).ConfigureAwait(false);
            await SynchronizeSchemasAsync(client, path).ConfigureAwait(false);

            foreach (string reportFilePath in ValidateUniquenes(Directory.GetFiles(path, "*.jsrep", SearchOption.AllDirectories)))
            {
                string reportName = Path.GetFileNameWithoutExtension(reportFilePath);

                string content = File.ReadAllText(reportFilePath + ".html");
                string helpers = File.ReadAllText(reportFilePath + ".js");


                var serializer = new XmlSerializer(typeof (ReportDefinition));
                var reportDefinition = serializer.Deserialize(new StreamReader(reportFilePath)) as ReportDefinition;

                Template template =
                    await
                    (client.For<Template>().Filter(x => x.name == reportName).FindEntryAsync()).ConfigureAwait(false);

                dynamic operation = new ExpandoObject();
                operation.name = reportName;
                operation.shortid = reportName;
                operation.engine = reportDefinition.Engine;
                operation.recipe = reportDefinition.Recipe;

                if (!string.IsNullOrEmpty(reportDefinition.Schema))
                    operation.dataItemId = reportDefinition.Schema;

                operation.content = content;
                operation.helpers = helpers;

                if (reportDefinition.Phantom != null && reportDefinition.Phantom.IsDirty)
                {
                    operation.phantom = new ExpandoObject();

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
                    await ((Task) client.For<Template>().Set(operation).InsertEntryAsync()).ConfigureAwait(false);
                }
                else
                {
                    operation._id = template._id;
                    await
                        ((Task)
                         client.For<Template>().Filter(x => x.name == reportName).Set(operation).UpdateEntryAsync())
                            .ConfigureAwait(false);
                }
            }
        }

        private async Task SynchronizeImagesAsync(ODataClient client, string path)
        {
            foreach (string imagePath in ValidateUniquenes(Directory.GetFiles(path, "*.jsrep.png", SearchOption.AllDirectories)))
            {
                string imageName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(imagePath));
                dynamic operation = new ExpandoObject();
                operation.content = File.ReadAllBytes(imagePath);
                operation.shortid = imageName;
                operation.name = operation.shortid;

                var image = await (client.For<Image>().Filter(x => x.name == imageName).FindEntryAsync()).ConfigureAwait(false);

                if (image == null)
                {
                    await ((Task)client.For<Image>().Set(operation).InsertEntryAsync()).ConfigureAwait(false);
                }
                else
                {
                    operation._id = image._id;
                    await ((Task)client.For<Image>().Filter(x => x.name == imageName).Set(operation).UpdateEntryAsync()).ConfigureAwait(false);
                }
            }
        }

        private async Task SynchronizeSchemasAsync(ODataClient client, string path)
        {
            foreach (string dataItemPath in ValidateUniquenes(Directory.GetFiles(path, "*.jsrep.json", SearchOption.AllDirectories)))
            {
                var dataItemName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(dataItemPath));
                dynamic operation = new ExpandoObject();
                operation.dataJson = File.ReadAllText(dataItemPath);
                operation.shortid = dataItemName;
                operation.name = operation.shortid;

                var dataItem = await (client.For<DataItem>("data").Filter(x => x.name == dataItemName).FindEntryAsync()).ConfigureAwait(false);

                if (dataItem == null)
                {
                    await ((Task)client.For<DataItem>("data").Set(operation).InsertEntryAsync()).ConfigureAwait(false);
                }
                else
                {
                    operation._id = dataItem._id;
                    await ((Task)client.For<DataItem>("data").Filter(x => x.name == dataItemName).Set(operation).UpdateEntryAsync()).ConfigureAwait(false);
                }
            }
        }

        public void CleanServerData()
        {
            InitializeServerPath();

            string dataPath = Path.Combine(AbsolutePathToServer, "jsreport-net-embedded", "data");
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