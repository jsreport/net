using System.Web;
using System.Web.Mvc;
using jsreport.Client;
using jsreport.MVC;

namespace Mvc
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new JsReportFilterAttribute(MvcApplication.EmbeddedReportingServer.ReportingService));
        }
    }
}