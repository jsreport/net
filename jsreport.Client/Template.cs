using System;
using System.Data.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JsReport
{
    [DataServiceKey("_id")] 
    public class Template
    {
        public string _id { get; set; }

        public string name { get; set; }

        public string html { get; set; }
        
        public string helpers { get; set; }

        public string engine { get; set; }

        public int generatedReportsCounter { get; set; }

        public DateTime modificationDate { get; set; }
    }
}