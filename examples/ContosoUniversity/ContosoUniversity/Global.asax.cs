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
        public static IEmbeddedReportingServer EmbeddedReportingServer { get; private set; }

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
            EmbeddedReportingServer = new EmbeddedReportingServer(){ RelativePathToServer = "../App_Data"};

            //wait for nodejs server to start
            EmbeddedReportingServer.StartAsync().Wait();
            //synchronize local *.jsrep files with embedded server
            EmbeddedReportingServer.ReportingService.SynchronizeTemplatesAsync().Wait();

            //alternatively you can also use a remote jsreport server on prem or even jsreportonline
            //var reportingService = new ReportingService("http://localhost:2000")
            //reportingService.SynchronizeTemplatesAsync().Wait();
        }
    }
}
