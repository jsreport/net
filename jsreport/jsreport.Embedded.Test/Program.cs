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
using jsreport.Client.Entities;

namespace jsreport.Embedded.Test
{

    public class Program
    {
        public static void Main()
        {
            var embededReportingServer = new EmbeddedReportingServer();
            embededReportingServer.StartAsync().Wait();

            var reportingService = new ReportingService(embededReportingServer.EmbeddedServerUri);
            //reportingService.SynchronizeTemplatesAsync().Wait();
            //Thread.Sleep(15000);

            var rs = new ReportingService("http://localhost:2000");
            var r = rs.GetServerVersionAsync().Result;

           
            /*var result = rs.RenderAsync(new RenderRequest()
                {
                    template = new Template()
                        {
                            content = "foo",
                            recipe = "hztml",
                            engine = "jsrender"
                        }
                }).Result;*/

            Parallel.ForEach(Enumerable.Range(1, 1), new ParallelOptions() { MaxDegreeOfParallelism = 3}, i => 
                {
                    var result = rs.RenderAsync(new RenderRequest()
                        {
                            template = new Template()
                                {
                                    name = "Sample report"
                                }
                        }).Result;
                    Console.WriteLine(i);
                });

            //Console.WriteLine(new StreamReader(result.Content).ReadToEnd());
            
            Console.WriteLine("Done");
            embededReportingServer.StopAsync().Wait();
        }
    }
}
