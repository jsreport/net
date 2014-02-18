using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Simple.OData.Client;

namespace JsReport
{
    public class ReportingService
    {
        public Uri ServiceUri { get; set; }
       
        public int Timeout { get; set; }

        public ReportingService(string serviceUri)
        {
            ServiceUri = new Uri(serviceUri);
            Timeout = 5000;
        }

        public async Task<Report> RenderAsync(RenderRequest request)
        {
            request.Options = request.Options ?? new RenderOptions();
            request.CopyToDynamicTemplate();

            var client = new HttpClient { BaseAddress = ServiceUri };

            var settings = new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
            var response = await client.PostAsync("/report", new StringContent(JsonConvert.SerializeObject(request, settings), Encoding.UTF8, "application/json"));

            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to render template. ", response);

            response.EnsureSuccessStatusCode();            
            
            return await ReportFromResponse(response);
        }

        private static async Task<Report> ReportFromResponse(HttpResponseMessage response)
        {
            var stream = await response.Content.ReadAsStreamAsync();

            return new Report
                {
                    Content = stream,
                    ContentType = response.Content.Headers.ContentType,
                    FileExtension = response.Headers.Single(k => k.Key == "File-Extension").Value.First(),
                    PermanentLink = response.Headers.Any(k => k.Key == "Permanent-Link") ? response.Headers.Single(k => k.Key == "Permanent-Link").Value.First(): null
                };
        }

        public async Task<Report> ReadReportAsync(string permanentLink) 
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response =  await client.GetAsync(permanentLink);
            
            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to retrieve report content. ", response);

            response.EnsureSuccessStatusCode();

            return await ReportFromResponse(response);
        }

        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.GetAsync("/recipe");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.GetAsync("/engine");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task<string> GetServerVersionAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            return await client.GetStringAsync("/version");
        }

        public ODataClient CreateODataClient()
        {
            return new ODataClient(ServiceUri.ToString() + "odata");
        }
    }
}
