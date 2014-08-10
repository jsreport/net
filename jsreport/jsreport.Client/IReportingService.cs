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
        /// The simpliest rendering using template shortid and input data used with https://playground.jsreport.net
        /// </summary>
        /// <param name="templateShortid">template shortid can be taken from jsreport playground studio</param>
        /// <param name="data">any json serializable object</param>
        /// <param name="version">template version number taken from playground</param>
        /// <exception cref="JsReportException"></exception>
        /// <returns>Report result promise</returns>
        Task<Report> RenderAsync(string templateShortid, int version, object data);

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
    }
}