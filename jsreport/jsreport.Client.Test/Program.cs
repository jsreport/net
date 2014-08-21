using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using jsreport.Client.Entities;
using jsreport.Embedded;

namespace jsreport.Client.Test
{

    public class Program
    {
        public static void Main()
        {
            var embededReportingServer = new EmbeddedReportingServer() { PingTimeout = new TimeSpan(0, 0, 100) };
            embededReportingServer.StartAsync().Wait();

            var reportingService = new ReportingService(embededReportingServer.EmbeddedServerUri);
            reportingService.SynchronizeTemplatesAsync().Wait();

            var rs = new ReportingService("http://localhost:2000");
            var r = rs.GetServerVersionAsync().Result;


            var result = rs.RenderAsync("Report1", null).Result;

            Console.WriteLine("Done");
            embededReportingServer.StopAsync().Wait();

        }
    }
}
