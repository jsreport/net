using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JsReport
{
    public class Template
    {
        public string _id { get; set; }

        public string shortid { get; set; }
        
        public string html { get; set; }
        
        public string helpers { get; set; }

        public string engine { get; set; }

        public string recipe { get; set; }

        [JsonIgnore()]
        public object additional { get; set; }
    }
}