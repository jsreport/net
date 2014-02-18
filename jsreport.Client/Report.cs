using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JsReport
{
    public class Report
    {
        public Stream Content { get; set; }

        public MediaTypeHeaderValue ContentType { get; set; }
        
        public string FileExtension { get; set; }

        public string PermanentLink { get; set; }
    }
}