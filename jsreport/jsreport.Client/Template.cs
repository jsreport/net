using Newtonsoft.Json;

namespace jsreport.Client
{
    public class Template
    {
        public string _id { get; set; }

        public string shortid { get; set; }
        
        public string content { get; set; }
        
        public string helpers { get; set; }

        public string engine { get; set; }

        public string recipe { get; set; }

        [JsonIgnore()]
        public object additional { get; set; }
    }
}