using Newtonsoft.Json;

namespace jsreport.Client
{
    public class RenderOptions
    {
        [JsonProperty("timeout")]
        public int timeout { get; set; }

        [JsonProperty("recipe")]
        public string recipe { get; set; }

        [JsonProperty("saveResult")]
        public bool saveResult { get; set; }
    }
}