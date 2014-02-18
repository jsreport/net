using Newtonsoft.Json;

namespace JsReport
{
    public class RenderOptions
    {
        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        [JsonProperty("async")]
        public bool Async { get; set; }

        [JsonProperty("recipe")]
        public string Recipe { get; set; }
    }
}