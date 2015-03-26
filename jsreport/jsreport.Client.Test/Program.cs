using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Simple.OData.Client;
using jsreport.Client.Entities;
using jsreport.Embedded;

namespace jsreport.Client.Test
{

    public class Program
    {
        public static void Main()
        {
            //var embededReportingServer = new EmbeddedReportingServer();
            //embededReportingServer.StartAsync().Wait();

            //var reportingService = new ReportingService(embededReportingServer.EmbeddedServerUri);
            //reportingService.SynchronizeTemplatesAsync().Wait();


            //var settings = new ODataClientSettings { UrlBase = "http://localhost:1337/odata" };
            //var client = new ODataClient(settings);
            //var res = client.GetEntryAsync("users").Result;


            //http://localhost:1337/odata

            var rs = new ReportingService("http://localhost:4000", "admin", "password");

            Parallel.ForEach(Enumerable.Range(1, 1000), new ParallelOptions() {
                MaxDegreeOfParallelism = 3 }, i =>
            {
                Console.WriteLine(i);
                var rep = rs.RenderAsync("X1ltdPq7t", null).Result;
            });

            Console.WriteLine("Done");
            

            //rs.SynchronizeTemplatesAsync().Wait();
            //rs.SynchronizeTemplatesAsync().Wait();
            //rs.SynchronizeTemplatesAsync().Wait();


            //var result = rs.RenderAsync("Report1", null).Result;

            Console.WriteLine("Done");
            //embededReportingServer.StopAsync().Wait();
        }
    }
}
