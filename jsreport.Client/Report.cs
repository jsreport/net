using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JsReport
{
    public class Report
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonIgnore]
        public Template Template { get; set; }

        [JsonIgnore]
        public Stream Content { get; set; }

        [JsonIgnore]
        public MediaTypeHeaderValue ContentType { get; set; }

        [JsonIgnore]
        public string FileExtension { get; set; }

        [JsonIgnore]
        public ReportingService ReportingService { get; set; }

        public async Task<Stream> ReadStreamAsync()
        {
            return await ReportingService.ReadReportStreamAsync(this);
        }
    }
}