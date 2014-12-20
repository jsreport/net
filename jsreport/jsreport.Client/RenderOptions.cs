using Newtonsoft.Json;

namespace jsreport.Client
{
    public class RenderOptions
    {
        [JsonProperty("timeout")]
        public int? timeout { get; set; }

        [JsonProperty("preview")]
        public bool? preview { get; set; }

        /// <summary>
        /// Any additional dynamic attributes, Value is copied into options root before sending to jsreport.
        /// </summary>
        [JsonIgnore()]
        public object additional { get; set; }
    }
}