using System.IO;
using System.Net.Http.Headers;

namespace jsreport.Client
{
    public class Report
    {
        public Stream Content { get; set; }

        public MediaTypeHeaderValue ContentType { get; set; }
        
        public string FileExtension { get; set; }

        public string PermanentLink { get; set; }
    }
}