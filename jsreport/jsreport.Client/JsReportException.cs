using System;
using System.Net.Http;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace jsreport.Client
{
    /// <summary>
    /// Exception used when communication with jsreport fails
    /// </summary>
    [Serializable]
    public class JsReportException : Exception
    {
        public JsReportException()
        {
        }

        public JsReportException(string message, string responseContent, string responseErrorMessage,
                                 HttpResponseMessage response) : base(message)
        {
            ResponseContent = responseContent;
            ResponseErrorMessage = responseErrorMessage;
            Response = response;
            ResponseContent = Response.Content.ReadAsStringAsync().Result;
        }

        public JsReportException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Parsed error message from jsreport response
        /// </summary>
        public string ResponseErrorMessage { get; set; }

        /// <summary>
        /// Original response from jsreport server
        /// </summary>
        [IgnoreDataMember]
        public HttpResponseMessage Response { get; set; }

        /// <summary>
        /// Raw string response content from jsreport server
        /// </summary>
        public string ResponseContent { get; set; }

        public static JsReportException Create(string message, HttpResponseMessage response)
        {
            string responseContent = response.Content.ReadAsStringAsync().Result;
            var responseMessage = JObject.Parse(responseContent)["message"].Value<string>();

            return new JsReportException(message + " " + responseMessage, responseContent, responseMessage, response);
        }

        public override string ToString()
        {
            return ResponseErrorMessage;
        }
    }
}