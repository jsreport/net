using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using jsreport.Embedded;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceTest
    {
        private IReportingService _reportingService;

        private EmbeddedReportingServer _embeddedReportingServer;
        private static readonly object _locker = new object();

        [SetUp]
        public void SetUp()
        {
            Monitor.Enter(_locker);
            _embeddedReportingServer = new EmbeddedReportingServer(3000);
            _embeddedReportingServer.StartAsync().Wait();

            _reportingService = _embeddedReportingServer.ReportingService;
        }

        [TearDown]
        public void TearDown()
        {
            _embeddedReportingServer.StopAsync().Wait();
            Monitor.Exit(_locker);
        }

        [Test]
        public async void html_report()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
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
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("%PDF"));
            }
        }

        [Test]
        public async void using_stored_templates()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    name = "test"
                }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("foo"));
            }
        }

        [Test]
        public async void should_use_data()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "{{:foo}}",
                    engine = "jsrender",
                    recipe = "html"
                },
                data = new
                    {
                        foo = "hello"
                    }
            });

            using (var reader = new StreamReader(result.Content))
            {
                Assert.IsTrue(reader.ReadToEnd().StartsWith("hello"));
            }
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
        public async void should_throw_valid_exception_when_invalid_engine()
        {
            try
            {
                var result = await _reportingService.RenderAsync(new {
                    template = new {
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
        [ExpectedException(typeof(TaskCanceledException))]
        public async void httpClientTimeout_should_cancel_rendering_task()
        {
            _reportingService.HttpClientTimeout = new TimeSpan(1);
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            });
        }

        [Test]
        [ExpectedException(typeof(TaskCanceledException))]
        public async void cancel_token_should_cancel_task()
        {
            var ts = new CancellationTokenSource();
            ts.CancelAfter(1);

            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "foo",
                    engine = "none",
                    recipe = "phantom-pdf"
                }
            }, ts.Token);
        }

        [Test]
        [ExpectedException(typeof(JsReportException))]
        public async void timeout_in_rendering_should_throw_JsReportException()
        {
            var result = await _reportingService.RenderAsync(new
            {
                template = new
                {
                    content = "{{:~foo()}}",
                    helpers = "function foo() { while(true) { } }",
                    engine = "jsrender",
                    recipe = "phantom-pdf"
                }
            });
        }

        [Test]
        public async void GetServerVersionAsync_should_return_version()
        {
            var result = await _reportingService.GetServerVersionAsync();
            Assert.IsTrue(result.Contains("."));
        }

        [Test]
        public async void render_preview_should_return_excel_online()
        {
            var report = await _reportingService.RenderAsync(new
            {

                template = new { content = "<table><tr><td>a</td></tr></table>", recipe = "html-to-xlsx", engine = "jsrender" },
                options = new
                {
                    preview = true
                }
            });


            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.IsTrue(str.Contains("iframe"));
        }
     
        //until it is fixed in jsreport-core
        /*[Test]
        public async void render_a_circular_structure_should_work()
        {
            var data = new Teacher() { Name = "John"};
            data.Students = new List<Student>() { new Student() { Name = "Doe", Teachers = new List<Teacher>() { data}}};

            var report = await _reportingService.RenderAsync(new
            {
                template = new { content = "{{{help this}}}", recipe = "html", engine = "handlebars", helpers = "function help(data) { return data.Students[0].Teachers[0].Name; }" },
                data = data
            });

            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.AreEqual("John", str);
        }*/
    }


    [TestFixture]
    public class AuthenticatedReportingServiceTest
    {
        private IReportingService _reportingService;

        private EmbeddedReportingServer _embeddedReportingServer;
        private static readonly object _locker = new object();

        [SetUp]
        public void SetUp()
        {
            Monitor.Enter(_locker);
            _embeddedReportingServer = new EmbeddedReportingServer(3000)
            {
                Configuration = new
                {
                    authentication = new
                    {
                        cookieSession = new
                        {
                            secret = "dasd321as56d1sd5s61vdv32"
                        },
                        admin = new
                        {
                            username = "admin",
                            password = "password"
                        }
                    },

                },
                Username = "admin",
                Password = "password"
            };
            _embeddedReportingServer.StartAsync().Wait();

            _reportingService = _embeddedReportingServer.ReportingService;
        }

        [TearDown]
        public void TearDown()
        {
            _embeddedReportingServer.StopAsync().Wait();
            Monitor.Exit(_locker);
        }

        [Test]
        public async void should_pass_through()
        {
            await _reportingService.GetServerVersionAsync();
        }

        [Test]
        [ExpectedException(typeof(HttpRequestException))]
        public async void should_throw_without_auth()
        {
            _reportingService.Username = null;
            _reportingService.Password = null;
            await _reportingService.GetServerVersionAsync();
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
