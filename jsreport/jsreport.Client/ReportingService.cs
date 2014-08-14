using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;
using jsreport.Client.Entities;

namespace jsreport.Client
{
    /// <summary>
    /// jsreport API .net Wrapper
    /// </summary>
    public class ReportingService : IReportingService
    {
        private readonly string _username;
        private readonly string _password;
        public Uri ServiceUri { get; set; }
        //used in visual studio tools to override AssemblyDirectory, because there we get some visual studio location folder...
        public string BinPath { get; set; }

        public ReportingService(string serviceUri, string username, string password) : this(serviceUri)
        {
            _username = username;
            _password = password;
        }

        public ReportingService(string serviceUri)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true; 
            
            ServiceUri = new Uri(serviceUri);
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient() {BaseAddress = ServiceUri};

            if (_username != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(String.Format("{0}:{1}",_username,_password))));
            }

            return client;
        }


        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(string templateShortid, object data)
        {
            if (string.IsNullOrEmpty(templateShortid))
                throw new ArgumentNullException("templateShortid cannot be null");
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid },
                data = data
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(string templateShortid, string jsonData)
        {
            return RenderAsync(templateShortid, string.IsNullOrEmpty(jsonData) ? (object) null : JObject.Parse(jsonData));
        }

        /// <summary>
        /// The simpliest rendering using template shortid and input data used with https://playground.jsreport.net
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport playground studio</param>
        /// <param name="data">any json serializable object</param>
        /// <param name="version">template version number taken from playground</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(string templateShortid, int version, object data)
        {
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid, version = version},
                data = data
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Overload for more sophisticated rendering.
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process <see cref="RenderRequest"/></param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(RenderRequest request)
        {
            request.options = request.options ?? new RenderOptions();
            request.CopyToDynamicTemplate();

            request.Validate();

            var client = CreateClient();

            var settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            var response =
                await
                client.PostAsync("/api/report",
                                 new StringContent(JsonConvert.SerializeObject(request, settings), Encoding.UTF8,
                                                   "application/json")).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
                throw JsReportException.Create("Unable to render template. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads previously rendered report. see http://jsreport.net/learn/reports
        /// </summary>
        /// <param name="permanentLink">link Report.PernamentLink from previously rendered report</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> ReadReportAsync(string permanentLink)
        {
            var client = CreateClient();

            var response = await client.GetAsync(permanentLink).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
                throw JsReportException.Create("Unable to retrieve report content. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Request list of recipes registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/recipe").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        /// <summary>
        /// Request list of engines registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/engine").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        /// <summary>
        /// Request jsreport package version
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetServerVersionAsync()
        {
            var client = CreateClient();

            return await client.GetStringAsync("/api/version").ConfigureAwait(false);
        }

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new Report
            {
                Content = stream,
                ContentType = response.Content.Headers.ContentType,
                FileExtension = response.Headers.Any(k => k.Key == "File-Extension") ? response.Headers.Single(k => k.Key == "File-Extension").Value.First() : null,
                PermanentLink =
                    response.Headers.Any(k => k.Key == "Permanent-Link")
                        ? response.Headers.Single(k => k.Key == "Permanent-Link").Value.First()
                        : null
            };
        }

        public ODataClient CreateODataClient()
        {
            var settings = new ODataClientSettings { UrlBase = ServiceUri + "odata" };


            if (_username != null)
                settings.Credentials = new NetworkCredential(_username, _password);

            return new ODataClient(settings);
        }

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


                var serializer = new XmlSerializer(typeof(ReportDefinition));
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
                    await ((Task)client.For<Template>().Set(operation).InsertEntryAsync()).ConfigureAwait(false);
                }
                else
                {
                    operation._id = template._id;
                    await ((Task)client.For<Template>().Filter(x => x.name == reportName).Set(operation).UpdateEntryAsync()).ConfigureAwait(false);
                }
            }
        }

        public async Task CreateOrUpdateSchema(string name, string content)
        {
            ODataClient client = CreateODataClient();

            await CreateOrUpdateDataItemInner(name, content, client);
        }

        private static async Task CreateOrUpdateDataItemInner(string name, string content, ODataClient client)
        {
            dynamic operation = new ExpandoObject();
            operation.dataJson = content;
            operation.shortid = name;
            operation.name = operation.shortid;

            var dataItem =
                await (client.For<DataItem>("data").Filter(x => x.name == name).FindEntryAsync()).ConfigureAwait(false);

            if (dataItem == null)
            {
                await ((Task) client.For<DataItem>("data").Set(operation).InsertEntryAsync()).ConfigureAwait(false);
            }
            else
            {
                operation._id = dataItem._id;
                await
                    ((Task) client.For<DataItem>("data").Filter(x => x.name == name).Set(operation).UpdateEntryAsync())
                        .ConfigureAwait(false);
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
                await
                    CreateOrUpdateDataItemInner(
                        Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(dataItemPath)),
                        File.ReadAllText(dataItemPath), client);
            }
        }

        private IEnumerable<string> ValidateUniquenes(IEnumerable<string> files)
        {
            var firstNonUnique = files.Select(Path.GetFileName).GroupBy(f => f).Where(g => g.Count() > 1).Select(g => g.Key).FirstOrDefault();

            if (firstNonUnique != null)
                throw new InvalidOperationException("Non unique item found during jsreprot synchronization: " + firstNonUnique);

            return files;
        }
    }
}