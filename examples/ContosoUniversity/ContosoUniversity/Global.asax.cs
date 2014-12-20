using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ContosoUniversity.DAL;
using System.Data.Entity.Infrastructure.Interception;
using jsreport.Client;
using jsreport.Embedded;
using Newtonsoft.Json;
using WebGrease.Configuration;

namespace ContosoUniversity
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IReportingService ReportingService { get; private set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DbInterception.Add(new SchoolInterceptorTransientErrors());
            DbInterception.Add(new SchoolInterceptorLogging());
            
            //its important to set RelativePathToServer otherwise jsreport will run from bin folder
            //that would cause application recycle with every generated report
            var EmbeddedReportingServer = new EmbeddedReportingServer(){ RelativePathToServer = "../App_Data", StartTimeout = new TimeSpan(0,0,20)};

            //wait for nodejs server to start
            EmbeddedReportingServer.StartAsync().Wait();
            //synchronize local *.jsrep files with embedded server
            EmbeddedReportingServer.ReportingService.SynchronizeTemplatesAsync().Wait();
            ReportingService = EmbeddedReportingServer.ReportingService;

            //alternatively you can also use a remote jsreport server on prem or even jsreportonline
            //ReportingService = new ReportingService("https://test.jsreportonline.net", "username", "password");
            //ReportingService.SynchronizeTemplatesAsync().Wait();
        }
    }
}
