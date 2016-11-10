using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;

namespace jsreport.Client
{
    public class JsReportWebHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public static IReportingService ReportingService { get; set; }

        public void ProcessRequest(HttpContext context)
        {
            if (ReportingService == null)
                throw new InvalidOperationException("Missing ReportingService on JsReportWebHandler");

            var request =
                (HttpWebRequest)WebRequest.Create(ReportingService.ServiceUri.ToString().TrimEnd('/') + context.Request.Url.PathAndQuery.Replace("/jsreport.axd", ""));
            request.Method = context.Request.HttpMethod;

            ParseRequestHeaders(context, request);

            if (request.Method != "GET" && request.Method != "DELETE")
            {
                context.Request.InputStream.CopyTo(request.GetRequestStream());
            }

            try
            {
                using (var responseMerge = request.GetResponse() as HttpWebResponse)
                {
                    ProcessResponse(context, responseMerge);
                }
            }
            catch (WebException webException)
            {
                ProcessResponse(context, (HttpWebResponse) webException.Response);
            }
        }

        private static void ParseRequestHeaders(HttpContext context, HttpWebRequest request)
        {
            foreach (string header in context.Request.Headers.AllKeys)
            {
                string headerValue = context.Request.Headers.Get(header);
                switch (header)
                {
                    case "If-Modified-Since":
                        request.IfModifiedSince = DateTime.Parse(headerValue);
                        continue;
                    case "Cache":
                        continue;
                    case "Connection":
                        continue;
                    case "Content-Length":
                        request.ContentLength = long.Parse(headerValue);
                        continue;
                    case "Content-Type":
                        request.ContentType = headerValue;
                        continue;
                    case "Accept":
                        request.Accept = headerValue;
                        continue;
                    case "Host":
                        request.Host = headerValue;
                        continue;
                    case "Referer":
                        request.Referer = headerValue;
                        continue;
                    case "User-Agent":
                        request.UserAgent = headerValue;
                        continue;
                }

                if (ReportingService.Username != null)
                {
                    request.Headers.Remove("Authorization");
                    request.Headers.Add("Authorization", "Basic " + System.Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(String.Format("{0}:{1}", ReportingService.Username, ReportingService.Password))));
                }

                request.Headers.Add(header, context.Request.Headers.Get(header));
            }

            request.Referer = context.Request.Url.ToString();
        }

        private static void ProcessResponse(HttpContext context, HttpWebResponse responseMerge)
        {
            foreach (string headerKey in responseMerge.Headers.Keys)
            {
                if (headerKey.Contains("Transfer"))
                    continue;


                context.Response.AddHeader(headerKey,
                                           string.Join(";", responseMerge.Headers.GetValues(headerKey)));
            }

            context.Response.StatusCode = (int)responseMerge.StatusCode;

            if (context.Request.Path == "/jsreport.axd" || context.Request.Path == "/jsreport.axd/")
            {
                using (var reader = new StreamReader(responseMerge.GetResponseStream()))
                {
                    var content = reader.ReadToEnd();
                    var buf = System.Text.Encoding.ASCII.GetBytes(content.Replace("/studio/assets/client", "/jsreport.axd/studio/assets/client"));
                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                }
            }
            else
            {
                responseMerge.GetResponseStream().CopyTo(context.Response.OutputStream);    
            }
            
            context.Response.OutputStream.Flush();
        }
    }
}