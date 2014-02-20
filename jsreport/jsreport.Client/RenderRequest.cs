using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Newtonsoft.Json;
using jsreport.Client;

namespace JsReport
{
    public class RenderRequest
    {
        [JsonIgnore]
        public Template Template { get; set; }

        [JsonProperty("template")]
        public dynamic DynamicTemplate { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("options")]
        public RenderOptions Options { get; set; }

        internal void CopyToDynamicTemplate()
        {
            DynamicTemplate = new ExpandoObject();

            if (Template.html != null)
                DynamicTemplate.html = Template.html;
            if (Template.helpers != null)
                DynamicTemplate.helpers = Template.helpers;
            if (Template.shortid != null)
                DynamicTemplate.shortid = Template.shortid;
            if (Template.recipe != null)
                DynamicTemplate.recipe = Template.recipe;
            if (Template.engine != null)
                DynamicTemplate.engine = Template.engine;

            if (Template.additional != null)
            {
                foreach (var p in Template.additional.GetType().GetRuntimeProperties())
                {
                    ((IDictionary<string, object>)DynamicTemplate)[p.Name] = p.GetValue(Template.additional);
                }
            }
            
        }
    }
}