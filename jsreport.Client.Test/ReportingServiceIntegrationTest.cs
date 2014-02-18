using System;
using System.IO;
using System.Linq;
using System.Net;
using JsReport;
using NUnit.Framework;
using Simple.OData.Client;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceIntegrationTest
    {
        private ReportingService _reportingService;

        [SetUp]
        public void SetUp()
        {
            _reportingService = new ReportingService("https://localhost:3000/");
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
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
        public async void render_and_store_result()
        {
            var report = await _reportingService.RenderAsync(new RenderRequest() {
                Template = new Template() { shortid = "ek-9DnfCt" },
                Options = new RenderOptions() { SaveResult = true }
            });

            var loadedReport = await _reportingService.ReadReportAsync(report.PermanentLink);

            var reader = new StreamReader(loadedReport.Content);

            var str = reader.ReadToEnd();
            Assert.IsNotNull(str);
        }

        [Test]
        public async void render_with_additional_overrides()
        {
            var report = await _reportingService.RenderAsync(new RenderRequest() { 
                Template = new Template() { 
                    recipe = "phantom-pdf",
                    shortid = "ek-9DnfCt",

                    additional = new
                    {
                        phantom = new
                        {
                            header = "tx",
                            margin = "2cm"
                        }
                    }
                }
            });
            
            var reader = new StreamReader(report.Content);

            var str = reader.ReadToEnd();
            Assert.IsNotNull("tx");
        }

        [Test]
        public void odata_search_should_work()
        {
            dynamic x = ODataDynamic.Expression;

            var entry = _reportingService.CreateODataClient()
                             .For(x.templates)
                             .Filter(x.shortid == "xkz45vhMCt")
                             .FindEntry();

            Assert.IsNotNull(entry.name);
        }


        [Test]
        public void odata_update_should_work()
        {
            var client = _reportingService.CreateODataClient();            

            dynamic x = ODataDynamic.Expression;

            var entry = client.For(x.templates)
                             .Filter(x.shortid == "xkz45vhMCt")
                             .FindEntry();

            client
                .For(x.templates)
                .Key(entry._id)
                .Set(new { name = "foo", _id = entry._id })
                .UpdateEntry();

            entry = client.For(x.templates)
                           .Filter(x.shortid == "xkz45vhMCt")
                           .FindEntry();

            Assert.AreEqual("foo", entry.name);
        }

        [Test]
        public void odata_delete_should_work()
        {
            var client = _reportingService.CreateODataClient();

            dynamic x = ODataDynamic.Expression;

            var entry = client.For(x.templates)
                             .Filter(x.shortid == "ek-9DnfCt")
                             .FindEntry();

            client.For(x.templates)
                .Key(entry._id)
                .DeleteEntry();
        }
    }
}
