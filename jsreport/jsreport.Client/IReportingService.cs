using System;
using System.Collections.Generic;
using System.Threading;
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
        Task<Report> RenderAsync(string templateShortid, object data, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderByNameAsync(string templateName, string jsonData, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template name</param>
        /// <param name="data">any json serializable object</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderByNameAsync(string templateName, object data, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// The simpliest rendering using template shortid and input data
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport studio or from filename in jsreport embedded</param>
        /// <param name="jsonData">any json string</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(string templateShortid, string jsonData, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// Specify comnpletely the rendering requests, see http://jsreport.net/learn/api for details
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process <see cref="RenderRequest"/></param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(RenderRequest request, CancellationToken ct = default(CancellationToken));

        /// <summary>
        /// Specify comnpletely the rendering requests, see http://jsreport.net/learn/api for details
        /// </summary>
        /// <param name="request">ram name="request">Description of rendering process</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(object request, CancellationToken ct = default(CancellationToken));
        

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
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        string Username { get; set; }

        /// <summary>
        /// Boolean to indicate if compression should be enabled or not
        /// </summary>
        bool Compression { get; set; }

        /// <summary>
        /// Credentials for jsreport having authentication enabled
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// Timeout for http client requests
        /// </summary>
        TimeSpan? HttpClientTimeout { get; set; }
    }
}