using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace jsreport.Embedded.Test
{
    [TestFixture]
    public class EmbeddedReportingServerTest
    {
        [Test]
        public async void start_should_start_nodejs_proccess_and_stop_should_stop()
        {
            var server = new EmbeddedReportingServer();
            await server.StartAsync();
            
            Assert.IsTrue(Process.GetProcesses().Any(p => p.ProcessName == "node"));

            await server.StopAsync();

            Assert.IsFalse(Process.GetProcesses().Any(p => p.ProcessName == "node"));
        }
    }
}
