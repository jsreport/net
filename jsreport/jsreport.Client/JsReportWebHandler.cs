using System;
using System.Net;
using System.Net.Http;
using System.Web;

namespace jsreport.Client
{
    public class JsReportWebHandler : IHttpHandler
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public static string ServiceUri { get; set; }

        public void ProcessRequest(HttpContext context)
        {
            var url = CreateRequestUrl(context);

            var request =
                (HttpWebRequest)WebRequest.Create(ServiceUri.TrimEnd('/') + (url.StartsWith("/") ? url : ("/" + url)));
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

        private static string CreateRequestUrl(HttpContext context)
        {
            string url = context.Request.QueryString["url"] ?? "/";

            if (url.EndsWith("main_embed"))
                url += ".js";

            if (url.Contains("?"))
                url += "&";
            else
                url += "?";

            if (!url.Contains("studio=embed"))
                url += "studio=embed";

            if (!url.EndsWith("&"))
                url += "&";
            
            url += "serverUrl=" + context.Server.UrlEncode(context.Request.Url.GetLeftPart(UriPartial.Authority) + "/jsreport.axd?url=/");
            return url;
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
                        request.Referer = headerValue;
                        continue;
                }

                request.Headers.Add(header, context.Request.Headers.Get(header));
            }
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

            context.Response.StatusCode = (int) responseMerge.StatusCode;
            responseMerge.GetResponseStream().CopyTo(context.Response.OutputStream);
            context.Response.OutputStream.Flush();
        }
    }
}