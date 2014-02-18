using System;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using JsReport.Query;
using System.Collections.Generic;

namespace JsReport
{
    public class ReportingService
    {
        public Uri ServiceUri { get; set; }
       
        public int Timeout { get; set; }

        public ReportingService(string serviceUri)
        {
            ServiceUri = new Uri(serviceUri);
            Timeout = 5000;
        }

        public async Task<Template> CreateTemplateAsync(Template template)
        {
            //var client = new HttpClient {BaseAddress = ServiceUri};

            //var response = await client.PostAsJsonAsync("/Template", template);

            //response.EnsureSuccessStatusCode();

            //return await response.Content.ReadAsAsync<Template>();
            return await Task.Run(() =>
                {
                    var context = new DataServiceContext(new Uri(ServiceUri + "odata/"));
                    context.AddObject("templates", template);
                    context.SaveChanges();
                    return template;
                });
        }    

        public async Task<Report> RenderAsync(RenderRequest request)
        {
            request.Options = request.Options ?? new RenderOptions();
            request.Options.Async = true;

            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.PostAsJsonAsync("/Report", request);

            response.EnsureSuccessStatusCode();

            var report = await response.Content.ReadAsAsync<Report>();

            report.Template = request.Template;

            return report;
        }

        public async Task<Report> RenderPreviewAsync(RenderRequest request)
        {
            request.Options = request.Options ?? new RenderOptions();
            request.Options.Async = false;

            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.PostAsJsonAsync("/report", request);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new JsReportException("Unable to render template. ", response);

            response.EnsureSuccessStatusCode();            

            var stream = await response.Content.ReadAsStreamAsync();

            return new Report
                {
                    Content = stream,
                    ContentType = response.Content.Headers.ContentType,
                    FileExtension = response.Headers.Single(k => k.Key == "File-Extension").Value.First()
                };
        }

        public async Task<Stream> ReadReportStreamAsync(Report report) 
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            return await client.GetStreamAsync("/report/" + report.Id + "/content");
        }

        public async Task<IEnumerable<string>> GetRecipesAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.GetAsync("/recipe");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task<IEnumerable<string>> GetEnginesAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.GetAsync("/engine");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<string>>();
        }

        public async Task RecreateTemplatesAsync(RecreateTemplatesOptionsEnum options)
        {
            var templates = (await GetTemplatesAsync()).ToList();

            if (options == RecreateTemplatesOptionsEnum.DropCreate)
            {
                await Task.WhenAll(templates.Select(DeleteTemplateAsync).ToArray());
            }

            await Task.WhenAll(Directory.EnumerateFiles(AssemblyDirectory, "*.jsrep").ToList()
                .Select(f => RecreateOneTempateAsync(options, f, templates)).ToArray());
        }

        private async Task RecreateOneTempateAsync(RecreateTemplatesOptionsEnum options, string f, IEnumerable<Template> templates)
        {
            var html = File.ReadAllText(f + ".html");
            var js = File.ReadAllText(f + ".js");

            var rd = new XmlDocument();
            rd.LoadXml(File.ReadAllText(f));

            var template = new Template
                {
                    helpers = js,
                    html = html,
                    name = Path.GetFileNameWithoutExtension(f),
                    engine = rd.SelectSingleNode("/ReportDefinition/Engine").Value
                };

            if (templates.Any(t => t.name == template.name) && options == RecreateTemplatesOptionsEnum.Update)
            {
                await UpdateTemplateAsync(template);
            }
            else
            {
                await CreateTemplateAsync(template);
            }
        }

        public async Task<IEnumerable<Template>> GetTemplatesAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.GetAsync("/template");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<Template>>();
        }

        static public string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public ReportQuery<Report> QueryReport()
        {
            var context = new DataServiceContext(ServiceUri);
            var q = context.CreateQuery<Template>("templates");
            var r = q.ToList();

            return null;
            //return new ReportQuery<Report>(this, new ReportQueryProvider(this),  null);
        }

        public DataServiceQuery<Template> QueryTemplate()
        {
            var context = new DataServiceContext(new Uri(ServiceUri + "odata/"));
            return context.CreateQuery<Template>("templates");
        }

        public async void DeleteReportAsync(Report report)
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.DeleteAsync("/report/" + report.Id);

            response.EnsureSuccessStatusCode();
        }

        public async Task<Template> UpdateTemplateAsync(Template template)
        {
            //var client = new HttpClient { BaseAddress = ServiceUri };

            //var response = await client.PutAsJsonAsync("/template/" + template._id, template);

            //response.EnsureSuccessStatusCode();

            //return await response.Content.ReadAsAsync<Template>();


            return await Task.Run(() =>
                {
                    var context = new DataServiceContext(new Uri(ServiceUri + "odata/"));
                    context.AttachTo("templates", template);
                    context.ChangeState(template, EntityStates.Modified);
                    context.SaveChanges();

                    return template;
                });
        }

        public async Task DeleteTemplateAsync(Template template)
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            var response = await client.DeleteAsync("/template/" + template._id);

            response.EnsureSuccessStatusCode();
        }

        public async Task<string> GetServerVersionAsync()
        {
            var client = new HttpClient { BaseAddress = ServiceUri };

            return await client.GetStringAsync("/version");
        }
    }
}
