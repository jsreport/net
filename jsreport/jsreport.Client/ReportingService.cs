using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
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

        public ReportingService(string serviceUri, string username, string password) : this(serviceUri)
        {
            _username = username;
            _password = password;
        }

        public ReportingService(string serviceUri)
        {
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
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid },
                data = data
            }).ConfigureAwait(false);
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

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return new Report
                {
                    Content = stream,
                    ContentType = response.Content.Headers.ContentType,
                    FileExtension =  response.Headers.Any(k => k.Key == "File-Extension") ? response.Headers.Single(k => k.Key == "File-Extension").Value.First() : null,
                    PermanentLink =
                        response.Headers.Any(k => k.Key == "Permanent-Link")
                            ? response.Headers.Single(k => k.Key == "Permanent-Link").Value.First()
                            : null
                };
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
    }
}