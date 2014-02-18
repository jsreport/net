using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JsReport;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceIntegrationTest
    {
        private ReportingService _reportingService;

        [SetUp]
        public void SetUp()
        {
            _reportingService = new ReportingService("http://localhost:3000/");
        }

        [Test]
        public async void should_not_fail_when_creating_templates()
        {
            await _reportingService.CreateTemplateAsync(new Template()
                {
                    name = "testTemplate",
                    html = "foo",
                    engine = "handlebars"
                });
        }

        [Test]
        public async void create_and_get_back_template()
        {
            var template = await _reportingService.CreateTemplateAsync(new Template()
            {
                name = "testTemplate",
                html = "foo",
                helpers = "{ a: function() { return 'a'; }",
                engine = "handlebars"
            });

            var templates = await _reportingService.GetTemplatesAsync();
            
            Assert.IsTrue(templates.Any(t => t._id == template._id));
        }

        [Test]
        public async void create_update_get_back()
        {
            var template = await _reportingService.CreateTemplateAsync(new Template()
            {
                name = "testTemplate",
                html = "foo",
                helpers = "{ a: function() { return 'a'; }",
                engine = "handlebars"
            });

            template.name = "updated";

            await _reportingService.UpdateTemplateAsync(template);

            var loaded = _reportingService.QueryTemplate().Where(t => t._id == template._id).ToList().Single();
            
            Assert.AreEqual("updated", loaded.name);
        }

        [Test]
        public async void get_recipes()
        {
            var recipes = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(recipes.Count() > 2);
        }

        [Test]
        public async void get_engines()
        {
            var engines = await _reportingService.GetRecipesAsync();

            Assert.IsTrue(engines.Count() > 2);
        }

        [Test]
        public async void create_template_and_render_preview()
        {
            var template = await _reportingService.CreateTemplateAsync(new Template()
            {
                html = "t{{a}}",
                engine = "handlebars",
            });

           var report = await _reportingService.RenderPreviewAsync(new RenderRequest()
                {
                    Data = new { a = "x"},
                    Template = template,
                });

            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.AreEqual("tx", str);
        }

        [Test]
        public async void render_preview_anonymous_template()
        {
            var report = await _reportingService.RenderPreviewAsync(new RenderRequest()
            {
                Data = new { a = "x" },
                Template = new Template()
                {
                    html = "t{{a}}",
                    engine = "handlebars",
                },
            });

            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.AreEqual("tx", str);
        }

        [Test]
        public async void create_template_render_and_read_stram()
        {
            var template = await _reportingService.CreateTemplateAsync(new Template()
            {
                html = "foo",
                engine = "handlebars",
                name = "template name"
            });

            var report = await _reportingService.RenderAsync(new RenderRequest()
            {
                Template = template,
            });

            var stream = await _reportingService.ReadReportStreamAsync(report);
            Assert.AreEqual("foo", new StreamReader(stream).ReadToEnd());
        }
        
        [Test]
        public async void recreate_templates_with_drop_should_create_new_template_from_file()
        {
            await _reportingService.RecreateTemplatesAsync(RecreateTemplatesOptionsEnum.DropCreate);

            var templates = await _reportingService.GetTemplatesAsync();

            Assert.AreEqual(1, templates.Count());
        }

        [Test]
        public async void error_handling()
        {
            var template = await _reportingService.CreateTemplateAsync(new Template()
            {
                html = "{{for}}t{{a}}",
                engine = "jsrender",
            });

            try
            {
                await _reportingService.RenderPreviewAsync(new RenderRequest()
                    {
                        Data = new {a = "x"},
                        Template = template,
                    });

                Assert.Fail("Should throw exception");
            }
            catch (JsReportException e)
            {
                Assert.NotNull(e.ResponseErrorMessage);
            }
        }

        [Test]
        public void query_test()
        {
            _reportingService.QueryTemplate();
        }
    }
}
