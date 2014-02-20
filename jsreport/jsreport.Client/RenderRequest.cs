using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Newtonsoft.Json;

namespace jsreport.Client
{
    public class RenderRequest
    {
        [JsonIgnore]
        public Template template { get; set; }

        [JsonProperty("template")]
        public dynamic dynamicTemplate { get; set; }

        [JsonProperty("data")]
        public object data { get; set; }

        [JsonProperty("options")]
        public RenderOptions options { get; set; }

        internal void CopyToDynamicTemplate()
        {
            dynamicTemplate = new ExpandoObject();

            if (template.html != null)
                dynamicTemplate.html = template.html;
            if (template.helpers != null)
                dynamicTemplate.helpers = template.helpers;
            if (template.shortid != null)
                dynamicTemplate.shortid = template.shortid;
            if (template.recipe != null)
                dynamicTemplate.recipe = template.recipe;
            if (template.engine != null)
                dynamicTemplate.engine = template.engine;

            if (template.additional != null)
            {
                foreach (var p in template.additional.GetType().GetRuntimeProperties())
                {
                    ((IDictionary<string, object>)dynamicTemplate)[p.Name] = p.GetValue(template.additional);
                }
            }
            
        }
    }
}