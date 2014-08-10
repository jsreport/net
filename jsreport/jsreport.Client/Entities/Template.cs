using Newtonsoft.Json;

namespace jsreport.Client.Entities
{
    /// <summary>
    /// Main report specification entity. Required for every rendering specification
    /// </summary>
    public class Template
    {
        public Template()
        {
            
        }

        public string _id { get; set; }

        /// <summary>
        /// Unique 9 alfanum id
        /// </summary>
        public string shortid { get; set; }

        /// <summary>
        /// Used only when rendering in https://playground.jsreport.nets
        /// </summary>
        public int version { get; set; }
        
        /// <summary>
        /// Content of report, most often this is html with javasript templating engines
        /// </summary>
        public string content { get; set; }
        
        /// <summary>
        /// Javascript helper functions in format: function a() { }; function b() { };
        /// </summary>
        public string helpers { get; set; }

        /// <summary>
        /// Used javascript templating engine like "jsrender" or "handlebars"
        /// </summary>
        public string engine { get; set; }

        /// <summary>
        /// Used recipe defining rendering process like "html", "phantom-pdf" or "fop"
        /// </summary>
        public string recipe { get; set; }

        /// <summary>
        /// Readable name, does not need to be unique
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Any additional dynamic attributes, Value is copied into Template root before sending to jsreport.
        /// </summary>
        [JsonIgnore()]
        public object additional { get; set; }

        /// <summary>
        /// Optional specification for phantom-pdf
        /// </summary>
        public Phantom phantom { get; set; }
    }
}