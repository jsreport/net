using System;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Simple.OData.Client;
using jsreport.Client.Entities;

namespace jsreport.Client.Test
{
    [TestFixture]
    public class ReportingServiceIntegrationTest
    {
        private ReportingService _reportingService;
        private ODataClient _oDataClient;

        [SetUp]
        public void SetUp()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            _reportingService = new ReportingService("https://local.net:3000/", "pofider@pofider.com", "password");
            _oDataClient = new ODataClient(new ODataClientSettings()
            {
                UrlBase = "https://local.net:3000/odata",
                BeforeRequest = (r) =>
                {
                    var encoded =
                    System.Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes("pofider@pofider.com:password"));
                    r.Headers["Authorization"] = "Basic " + encoded;
                }
            });
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
            var report = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template() { shortid = "ekVX9G9crc" },
                options = new RenderOptions() { saveResult = true }
            });

            var loadedReport = await _reportingService.ReadReportAsync(report.PermanentLink);

            var reader = new StreamReader(loadedReport.Content);

            var str = reader.ReadToEnd();
            Assert.IsNotNull(str);
        }

        [Test]
        public async void render_with_additional_overrides()
        {
            var report = await _reportingService.RenderAsync(new RenderRequest()
            {
                template = new Template()
                {
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

            var entry = _oDataClient.For(x.templates)
                             .Filter(x.shortid == "g1xcKBanJc")
                             .FindEntry();

            Assert.IsNotNull(entry.name);
        }

        [Test]
        public void odata_update_should_work()
        {
            dynamic x = ODataDynamic.Expression;

            var entry = _oDataClient.For(x.templates)
                             .Filter(x.shortid == "g1xcKBanJc")
                             .FindEntry();

            _oDataClient
                .For(x.templates)
                .Key(entry._id)
                .Set(new { name = "foo", _id = entry._id })
                .UpdateEntry();

            entry = _oDataClient.For(x.templates)
                           .Filter(x.shortid == "g1xcKBanJc")
                           .FindEntry();

            Assert.AreEqual("foo", entry.name);
        }

        [Test]
        public void odata_delete_should_work()
        {
            dynamic x = ODataDynamic.Expression;

            var entry = _oDataClient.For(x.templates)
                             .Filter(x.shortid == "ek-9DnfCt")
                             .FindEntry();

            _oDataClient.For(x.templates)
                .Key(entry._id)
                .DeleteEntry();
        }
    }
}
