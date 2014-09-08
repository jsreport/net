using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using jsreport.Client;
using jsreport.Client.Entities;

namespace jsreport.MVC
{
    public class JsReportFilterAttribute : Attribute, IActionFilter
    {
        protected IReportingService ReportingService { get; set; }

        public JsReportFilterAttribute(IReportingService reportingService)
        {
            ReportingService = reportingService;
        }

        public JsReportFilterAttribute()
        {
        }

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

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(context.Controller.ControllerContext, viewName);
                ViewContext viewContext = new ViewContext(context.Controller.ControllerContext, viewResult.View, context.Controller.ViewData, context.Controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        protected virtual async Task<Report> RenderReport(ActionExecutedContext context, EnableJsReportAttribute jsreportAttribute, string htmlContent)
        {
            var output = await ReportingService.RenderAsync(new RenderRequest()
            {
                template = new Template()
                {
                    content = RemoveVisualStudioBrowserLink(htmlContent),
                    recipe = "phantom-pdf",
                    phantom = new Phantom()
                    {
                            margin = jsreportAttribute.Margin,
                            headerHeight = jsreportAttribute.HeaderHeight,
                            header = jsreportAttribute.HeaderPartialView != null ? RenderPartialViewToString(context, jsreportAttribute.HeaderPartialView, null) : null,
                            footerHeight = jsreportAttribute.FooterHeight,
                            footer = jsreportAttribute.FooterPartialView != null ? RenderPartialViewToString(context, jsreportAttribute.FooterPartialView, null) : null
                    }
                }
            }).ConfigureAwait(false);

            context.HttpContext.Response.ContentType = output.ContentType.MediaType;

            return output;
        }

        //https://github.com/jsreport/net/issues/1
        protected virtual string RemoveVisualStudioBrowserLink(string content)
        {
            var start = content.IndexOf("<!-- Visual Studio Browser Link -->", System.StringComparison.Ordinal);
            var end = content.IndexOf("<!-- End Browser Link -->", System.StringComparison.Ordinal);

            if (start > -1 && end > -1)
            {
                return content.Remove(start, end - start);
            }

            return content;
        }


        private bool ShouldUseJsReport(ActionExecutedContext filterContext, out EnableJsReportAttribute attr)
        {
            var enableJsReport = false;
            attr = null;

            if (filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(EnableJsReportAttribute), true))
            {
                attr =
                    (EnableJsReportAttribute)
                    filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(
                        typeof(EnableJsReportAttribute), true).First();
                enableJsReport = true;
            }

            if (filterContext.ActionDescriptor.IsDefined(typeof(EnableJsReportAttribute), true))
            {
                attr =
                    (EnableJsReportAttribute)
                    filterContext.ActionDescriptor.GetCustomAttributes(typeof(EnableJsReportAttribute), true).First();
                enableJsReport = true;
            }

            return enableJsReport;
        }
    }
}