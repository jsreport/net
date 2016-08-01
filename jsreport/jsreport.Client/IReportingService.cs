using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jsreport.Client
{
    /// <summary>
    /// jsreport API .net Wrapper
    /// </summary>
    public interface IReportingService
    {
        /// <summary>
        /// Uri to jsreport server like http://localhost:2000/ or https://subdomain.jsreportonline.net 
        /// </summary>
        Uri ServiceUri { get; set; }

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(string templateShortid, object data);

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(string templateShortid, string jsonData);

        Task<Report> RenderAsync(object request);

        /// <summary>
        /// Overload for more sophisticated rendering.
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process <see cref="RenderRequest"/></param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(RenderRequest request);

        /// <summary>
        /// Reads previously rendered report. see http://jsreport.net/learn/reports
        /// </summary>
        /// <param name="permanentLink">link Report.PernamentLink from previously rendered report</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> ReadReportAsync(string permanentLink);

        /// <summary>
        /// Request list of recipes registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        Task<IEnumerable<string>> GetRecipesAsync();

        /// <summary>
        /// Request list of engines registered in jsreport server
        /// </summary>
        /// <returns>list of recipes names</returns>
        Task<IEnumerable<string>> GetEnginesAsync();

        /// <summary>
        /// Request jsreport package version
        /// </summary>
        /// <returns></returns>
        Task<string> GetServerVersionAsync();

        /// <summary>
        /// Synchronize all *.jsrep files into jsreport server including images and sample json files
        /// </summary>
        Task SynchronizeTemplatesAsync();

        Task CreateOrUpdateSampleData(string name, string content);

        string Username { get; set; }
        string Password { get; set; }

        TimeSpan? HttpClientTimeout { get; set; }
    }
}