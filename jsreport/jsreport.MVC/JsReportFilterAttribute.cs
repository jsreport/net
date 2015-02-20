using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using jsreport.Client;
using jsreport.Client.Entities;

namespace jsreport.MVC
{
    public class JsReportFilterAttribute : Attribute, IActionFilter
    {
        public JsReportFilterAttribute(IReportingService reportingService)
        {
            ReportingService = reportingService;
        }

        public JsReportFilterAttribute()
        {
        }

        protected IReportingService ReportingService { get; set; }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            EnableJsReportAttribute attr;
            if (ShouldUseJsReport(filterContext, out attr))
            {                
                filterContext.HttpContext.Response.Filter = new JsReportStream(filterContext, attr, RenderReport);
            }
        }

        protected virtual string RenderPartialViewToString(ActionExecutedContext context, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = context.Controller.ControllerContext.RouteData.GetRequiredString("action");

            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(context.Controller.ControllerContext,
                                                                                  viewName);
                var viewContext = new ViewContext(context.Controller.ControllerContext, viewResult.View,
                                                  context.Controller.ViewData, context.Controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        protected virtual async Task<Report> RenderReport(ActionExecutedContext context,
                                                          EnableJsReportAttribute jsreportAttribute, string htmlContent)
        {
            Report output = await ReportingService.RenderAsync(new RenderRequest
                {
                    template = new Template
                        {
                            content = RemoveVisualStudioBrowserLink(htmlContent),
                            recipe = jsreportAttribute.Recipe ?? "phantom-pdf",
                            phantom = new Phantom
                                {
                                    margin = jsreportAttribute.Margin,
                                    headerHeight = jsreportAttribute.HeaderHeight,
                                    header =
                                        jsreportAttribute.HeaderPartialView != null
                                            ? RenderPartialViewToString(context, jsreportAttribute.HeaderPartialView,
                                                                        null)
                                            : null,
                                    footerHeight = jsreportAttribute.FooterHeight,
                                    footer =
                                        jsreportAttribute.FooterPartialView != null
                                            ? RenderPartialViewToString(context, jsreportAttribute.FooterPartialView,
                                                                        null)
                                            : null,
                                    orientation = jsreportAttribute.Orientation,
                                    width = jsreportAttribute.Width,
                                    height = jsreportAttribute.Height,
                                    format = jsreportAttribute.Format
                                }
                        }
                }).ConfigureAwait(false);
           
            AddResponseHeaders(context, jsreportAttribute, output);

            return output;
        }

        protected virtual void AddResponseHeaders(ActionExecutedContext context, EnableJsReportAttribute jsreportAttribute,
                                               Report output)
        {
            foreach (var httpResponseHeader in output.Response.Headers)
            {
                if (httpResponseHeader.Key.ToLower() == "connection" || httpResponseHeader.Key.ToLower() == "transfer-encoding")
                    continue;


                context.HttpContext.Response.AddHeader(httpResponseHeader.Key,
                                                       string.Join(";", httpResponseHeader.Value));
            }

            foreach (var httpContentHeader in output.Response.Content.Headers)
            {
                if (jsreportAttribute.ContentDisposition != null && httpContentHeader.Key.ToLower() == "content-disposition")
                {
                    context.HttpContext.Response.AddHeader(httpContentHeader.Key, jsreportAttribute.ContentDisposition);
                }
                else
                {
                    context.HttpContext.Response.AddHeader(httpContentHeader.Key,
                                                           string.Join(";", httpContentHeader.Value));
                }
            }

            context.HttpContext.Response.ContentType = output.ContentType.MediaType;
        }

        //https://github.com/jsreport/net/issues/1
        protected virtual string RemoveVisualStudioBrowserLink(string content)
        {
            int start = content.IndexOf("<!-- Visual Studio Browser Link -->", StringComparison.Ordinal);
            int end = content.IndexOf("<!-- End Browser Link -->", StringComparison.Ordinal);

            if (start > -1 && end > -1)
            {
                return content.Remove(start, end - start);
            }

            return content;
        }


        private bool ShouldUseJsReport(ActionExecutedContext filterContext, out EnableJsReportAttribute attr)
        {
            bool enableJsReport = false;
            attr = null;

            if (filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof (EnableJsReportAttribute), true))
            {
                attr =
                    (EnableJsReportAttribute)
                    filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(
                        typeof (EnableJsReportAttribute), true).First();
                enableJsReport = true;
            }

            if (filterContext.ActionDescriptor.IsDefined(typeof (EnableJsReportAttribute), true))
            {
                attr =
                    (EnableJsReportAttribute)
                    filterContext.ActionDescriptor.GetCustomAttributes(typeof (EnableJsReportAttribute), true).First();
                enableJsReport = true;
            }

            return enableJsReport;
        }
    }
}