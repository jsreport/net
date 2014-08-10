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
    [TestFixture]
    public class EmbeddedReportingServerTest
    {
        private EmbeddedReportingServer _embeddedReportingServer;
        private ReportingService _reportingService = new ReportingService("http://localhost:2000");
        private static readonly object _locker = new object();

        [SetUp]
        public void SetUp()
        {
            Monitor.Enter(_locker);
            _embeddedReportingServer = new EmbeddedReportingServer() {PingTimeout = new TimeSpan(0, 0, 10)};
            _embeddedReportingServer.CleanServerData();
            _embeddedReportingServer.StartAsync().Wait();
            
            _reportingService = new ReportingService("http://localhost:2000");
        }

        [TearDown]
        public void TearDown()
        {
            _embeddedReportingServer.StopAsync().Wait();
            Monitor.Exit(_locker);
        }

        [Test]
        public async void should_throw_valid_exception_when_invalid_engine()
        {
            try
            {
                var result = await _reportingService.RenderAsync(new RenderRequest()
                {
                    template = new Template()
                    {
                        content = "foo",
                        engine = "NOT_EXISTING",
                        recipe = "phantom-pdf"
                    }
                });
            }
            catch (JsReportException ex)
            {
                Assert.IsTrue(ex.ResponseErrorMessage.Contains("NOT_EXISTING"));
            }
        }

        [Test]
        public async void html_report()
        {
            var result = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template()
                {
                    content = "foo",
                    recipe = "html"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("foo", reader.ReadToEnd());
            }
        }

        [Test]
        public async void phantom_pdf_report()
        {
            var result = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template()
                {
                    content = "foo",
                    recipe = "phantom-pdf"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }

        [Test]
        public async void synchronize_multiple_times_should_update()
        {
            File.WriteAllText("Report2.jsrep.html", "before");
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            File.WriteAllText("Report2.jsrep.html", "update");
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var template = await _embeddedReportingServer.CreateODataClient()
                                    .For<Template>()
                                    .Filter(t => t.name == "Report2")
                                    .FindEntryAsync();

            Assert.AreEqual("update", template.content);
        }

        [Test]
        public async void synchronize_and_render_html()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report1", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("foo", reader.ReadToEnd());
            }
        }

        [Test]
        public async void synchronize_and_render_phantom()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report2", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }

        [Test]
        public async void synchronize_images()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();
          
            var image = await _embeddedReportingServer.CreateODataClient()
                                    .For<Image>()
                                    .Filter(t => t.name == "Image1")
                                    .FindEntryAsync();

            Assert.IsNotNull(image.content);
        }

        [Test]
        public async void multiple_image_synchronize_should_update()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var images = await _embeddedReportingServer.CreateODataClient()
                                    .For<Image>()
                                    .Filter(t => t.name == "Image1")
                                    .FindEntriesAsync();

            Assert.AreEqual(1, images.Count());
        }

        [Test]
        public async void synchronize_and_use_images()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync(new RenderRequest()
                {
                    template = new Template()
                        {
                            content = "<img src='{#image Image1}' />",
                            recipe = "html",
                            engine = "jsrender"
                        }
                });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().Contains("base64"));
            }
        }

        [Test]
        public async void synchronize_data_items()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var dataItem = await _embeddedReportingServer.CreateODataClient()
                                    .For<DataItem>("data")
                                    .Filter(t => t.name == "ReportSchema1")
                                    .FindEntryAsync();

            Assert.IsNotNull(dataItem.dataJson);
        }

        [Test]
        public async void multiple_data_synchronize_should_update()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var dataItems = await _embeddedReportingServer.CreateODataClient()
                                    .For<DataItem>("data")
                                    .Filter(t => t.name == "ReportSchema1")
                                    .FindEntriesAsync();

            Assert.AreEqual(1, dataItems.Count());
        }

        [Test]
        public async void synchronize_and_use_schema()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report4", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("Hello world", reader.ReadToEnd());
            }
        }

        [Test]
        public async void synchronize_and_use_schema_from_child_directory()
        {
            await _embeddedReportingServer.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync(new RenderRequest()
                {
                    template = new Template()
                        {
                            content = "{{{foo}}}",
                            recipe = "html",
                            engine = "handlebars",
                            additional = new
                                {
                                    dataItemId = "NestedSchema"
                                }
                        }
                });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("nested", reader.ReadToEnd());
            }
        }
    }
}
