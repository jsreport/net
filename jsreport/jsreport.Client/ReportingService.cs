using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace jsreport.Client
{
    public class ReportingService 
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

        public async Task<Report> RenderAsync(string templateShortid, object data)
        {
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid },
                data = data
            }).ConfigureAwait(false);
        }

        public async Task<Report> RenderAsync(string templateShortid, int version, object data)
        {
            return await RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = templateShortid, version = version},
                data = data
            }).ConfigureAwait(false);
        }

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
                throw new JsReportException("Unable to render template. ", response);

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
                    FileExtension =  response.Headers.Any(k => k.Key == "Permanent-Link") ?
                            response.Headers.Single(k => k.Key == "File-Extension").Value.First() : null,
                    PermanentLink =
                        response.Headers.Any(k => k.Key == "Permanent-Link")
                            ? response.Headers.Single(k => k.Key == "Permanent-Link").Value.First()
                            : null
                };
        }

        public async Task<Report> ReadReportAsync(string permanentLink)
        {
            var client = CreateClient();

            var response = await client.GetAsync(permanentLink).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to retrieve report content. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response).ConfigureAwait(false);
        }

        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/recipe").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = CreateClient();

            var response = await client.GetAsync("/api/engine").ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        public async Task<string> GetServerVersionAsync()
        {
            var client = CreateClient();

            return await client.GetStringAsync("/api/version").ConfigureAwait(false);
        }
    }
}