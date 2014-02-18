using Newtonsoft.Json;

namespace JsReport
{
    public class RenderRequest
    {
        [JsonProperty("template")]
        public Template Template { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("options")]
        public RenderOptions Options { get; set; }
        
        public RenderRequest Customize()
        {
            return this;
        }
    }
}