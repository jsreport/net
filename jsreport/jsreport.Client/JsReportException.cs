using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace jsreport.Client
{
    public class JsReportException : Exception 
    {
        public JsReportException()
        {
        }

        public JsReportException(string message, HttpResponseMessage response) : base(message)
        {
            Response = response;
            ResponseContent = Response.Content.ReadAsStringAsync().Result;
        }

        public JsReportException(string message, Exception innerException) : base(message, innerException)
        {
        }
        
        public HttpResponseMessage Response { get; set; }

        public string ResponseContent { get; set; }

        public string ResponseErrorMessage
        {
             get
             {
                 return
                     JObject.Parse(ResponseContent)["message"].Value<string>();
             }
        }
    }
}