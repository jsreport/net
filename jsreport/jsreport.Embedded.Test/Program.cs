using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using jsreport.Client;

namespace jsreport.Embedded.Test
{

    public class Program
    {
        public static void Main()
        {
            var embededReportingServer = new EmbeddedReportingServer() { PingTimeout = new TimeSpan(0,0,300)};
            embededReportingServer.StartAsync().Wait();
            embededReportingServer.SynchronizeLocalTemplatesAsync().Wait();
            //Thread.Sleep(300000);

            var result = new ReportingService("http://localhost:2000").RenderAsync("Report1", null).Result;

            Console.WriteLine("Done");
            embededReportingServer.StopAsync().Wait();
        }
    }
}
