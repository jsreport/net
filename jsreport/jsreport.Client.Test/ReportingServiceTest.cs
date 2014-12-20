using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using Simple.OData.Client;
using jsreport.Client.Entities;
using jsreport.Embedded;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceTest
    {
        private ReportingService _reportingService;

        private EmbeddedReportingServer _embeddedReportingServer;
        private static readonly object _locker = new object();

        [SetUp]
        public void SetUp()
        {
            Monitor.Enter(_locker);
            _embeddedReportingServer = new EmbeddedReportingServer(3000);
            _embeddedReportingServer.CleanServerData();
            _embeddedReportingServer.StartAsync().Wait();

            _reportingService = new ReportingService("http://localhost:3000");
        }

        [TearDown]
        public void TearDown()
        {
            _embeddedReportingServer.StopAsync().Wait();
            Monitor.Exit(_locker);
        }
     
        [Test]
        public async void get_recipes()
        {
            var recipes = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(recipes.Count() > 1);
        }

        [Test]
        public async void get_engines()
        {
            var engines = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(engines.Count() > 1);
        }


        [Test]
        public async void render_and_store_result()
        {
            var report = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template() { content = "foo", recipe = "html", engine = "jsrender" },
                options = new RenderOptions()
                {
                    additional = new
                        {
                            reports = new { save = true }
                        }
                }
            });

            var loadedReport = await _reportingService.ReadReportAsync(report.PermanentLink);

            var reader = new StreamReader(loadedReport.Content);

            var str = reader.ReadToEnd();
            Assert.IsNotNull(str);
        }

        [Test]
        public async void odata_delete_should_work()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var entry = await _reportingService.CreateODataClient().For<Template>()
                             .Filter(x => x.shortid == "Report1")
                             .FindEntryAsync();

            await _reportingService.CreateODataClient().For<Template>().Key(entry._id).DeleteEntryAsync();

            var entries = await _reportingService.CreateODataClient().For<Template>()
                            .Filter(x => x.shortid == "Report1")
                            .FindEntriesAsync();

            Assert.IsFalse(entries.Any());
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
            await _reportingService.SynchronizeTemplatesAsync();

            File.WriteAllText("Report2.jsrep.html", "update");
            await _reportingService.SynchronizeTemplatesAsync();

            var template = await _reportingService.CreateODataClient()
                                    .For<Template>()
                                    .Filter(t => t.name == "Report2")
                                    .FindEntryAsync();

            Assert.AreEqual("update", template.content);
        }

        [Test]
        public async void synchronize_and_render_html()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report1", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("foo", reader.ReadToEnd());
            }
        }

        [Test]
        public async void synchronize_and_render_phantom()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report2", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }

        [Test]
        public async void synchronize_images()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var image = await _reportingService.CreateODataClient()
                                    .For<Image>()
                                    .Filter(t => t.name == "Image1")
                                    .FindEntryAsync();

            Assert.IsNotNull(image.content);
        }

        [Test]
        public async void multiple_image_synchronize_should_update()
        {
            await _reportingService.SynchronizeTemplatesAsync();
            await _reportingService.SynchronizeTemplatesAsync();

            var images = await _reportingService.CreateODataClient()
                                    .For<Image>()
                                    .Filter(t => t.name == "Image1")
                                    .FindEntriesAsync();

            Assert.AreEqual(1, images.Count());
        }

        [Test]
        public async void synchronize_and_use_images()
        {
            await _reportingService.SynchronizeTemplatesAsync();

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
            await _reportingService.SynchronizeTemplatesAsync();

            var dataItem = await _reportingService.CreateODataClient()
                                    .For<DataItem>("data")
                                    .Filter(t => t.name == "ReportSchema1")
                                    .FindEntryAsync();

            Assert.IsNotNull(dataItem.dataJson);
        }

        [Test]
        public async void multiple_data_synchronize_should_update()
        {
            await _reportingService.SynchronizeTemplatesAsync();
            await _reportingService.SynchronizeTemplatesAsync();

            var dataItems = await _reportingService.CreateODataClient()
                                    .For<DataItem>("data")
                                    .Filter(t => t.name == "ReportSchema1")
                                    .FindEntriesAsync();

            Assert.AreEqual(1, dataItems.Count());
        }

        [Test]
        public async void synchronize_and_use_sampleData()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report4", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.AreEqual("Hello world", reader.ReadToEnd());
            }
        }

        [Test]
        public async void synchronize_and_use_sampleData_from_child_directory()
        {
            await _reportingService.SynchronizeTemplatesAsync();

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

        [Test]
        public async void render_a_circular_structure_should_work()
        {
            var data = new Teacher() { Name = "John"};
            data.Students = new List<Student>() { new Student() { Name = "Doe", Teachers = new List<Teacher>() { data}}};

            var report = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template() { content = "{{help this}}", recipe = "html", engine = "handlebars", helpers = "function help(data) { return data.Students[0].Name; }" },
                data = data
            });

            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.AreEqual("Doe", str);
        }

        [Test]
        public async void synchronize_and_render_many_images()
        {
            await _reportingService.SynchronizeTemplatesAsync();

            var result = await _reportingService.RenderAsync("Report5", null);

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }
    }

    public class Teacher
    {
        public string Name { get; set; }
        public IEnumerable<Student> Students { get; set; } 
    }

    public class Student
    {
        public string Name { get; set; }
        public IEnumerable<Teacher> Teachers { get; set; }
    }
}
