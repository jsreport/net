using Newtonsoft.Json;

namespace jsreport.Client
{
    public class RenderOptions
    {
        [JsonProperty("timeout")]
        public int timeout { get; set; }

        [JsonProperty("saveResult")]
        public bool saveResult { get; set; }

        [JsonProperty("preview")]
        public bool preview { get; set; }
    }
}