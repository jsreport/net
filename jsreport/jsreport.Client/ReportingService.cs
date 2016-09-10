using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using jsreport.Client.Entities;

namespace jsreport.Client
{
    /// <summary>
    /// jsreport API .net Wrapper
    /// </summary>
    public class ReportingService : IReportingService
    {
        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        public string Username { get; set; }
       
        /// <summary>
        /// Boolean to indicate if compression should be enabled or not
        /// </summary>
        public bool Compression { get; set; }

        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        public string Password { get; set; }
        public Uri ServiceUri { get; set; }

        /// <summary>
        /// Timeout for http client requests
        /// </summary>
        public TimeSpan? HttpClientTimeout { get; set; }
        
        public ReportingService(string serviceUri, string username, string password) : this(serviceUri)
        {
            Username = username;
            Password = password;
        }

        public ReportingService(string serviceUri)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true; 
            ServiceUri = new Uri(serviceUri);
        }


        protected virtual HttpClient CreateClient()
        {
            var client = new HttpClient() {BaseAddress = ServiceUri};

            if (Username != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(String.Format("{0}:{1}",Username,Password))));
            }

            if (Compression)
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            if (HttpClientTimeout != null)
                client.Timeout = HttpClientTimeout.Value;

            return client;
        }


        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(string templateShortid, object data, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(templateShortid))
                throw new ArgumentNullException("templateShortid cannot be null");
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid },
                data = data
            }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderByNameAsync(string templateName, string jsonData, CancellationToken ct = new CancellationToken())
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentNullException("templateName cannot be null");
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { name = templateName },
                data = string.IsNullOrEmpty(jsonData) ? (object)null : JObject.Parse(jsonData)
            }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template name</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderByNameAsync(string templateName, object data, CancellationToken ct = new CancellationToken())
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentNullException("templateName cannot be null");
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { name = templateName },
                data = data
            }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Overload for more sophisticated rendering.
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process <see cref="RenderRequest"/></param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(RenderRequest request, CancellationToken ct = default(CancellationToken))
        {
            return await RenderAsync((object)request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public Task<Report> RenderAsync(string templateShortid, string jsonData, CancellationToken ct = default(CancellationToken))
        {
            return RenderAsync(templateShortid, string.IsNullOrEmpty(jsonData) ? (object) null : JObject.Parse(jsonData), ct);
        }


        /// <summary>
        /// Specify comnpletely the rendering requests, see http://jsreport.net/learn/api for details
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        public async Task<Report> RenderAsync(object request, CancellationToken ct = default(CancellationToken))
        {
            var client = CreateClient();

            var response =
                await
                client.PostAsync("api/report",
                                 new StringContent(ValidateAndSerializeRequest(request), Encoding.UTF8,
                                                   "application/json"), ct).ConfigureAwait(false);
            

            if (response.StatusCode != HttpStatusCode.OK)
                throw JsReportException.Create("Unable to render template. ", response);

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

        private string ValidateAndSerializeRequest(object request)
        {
            if (request is RenderRequest)
            {
                ((RenderRequest)request).options = ((RenderRequest)request).options ?? new RenderOptions();
                ((RenderRequest)request).CopyDynamicAttributes();

                ((RenderRequest)request).Validate();
            }

            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };

            return JsonConvert.SerializeObject(request, settings);
        }

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new Report
            {
                Response = response,
                Content = stream,
                ContentType = response.Content.Headers.ContentType,
                FileExtension = response.Headers.Any(k => k.Key == "File-Extension") ? response.Headers.Single(k => k.Key == "File-Extension").Value.First() : null,
            };
        }
    }
}