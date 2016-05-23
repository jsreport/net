using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace jsreport.Local
{
    /// <summary>
    /// Direct rendering inside server-less local jsreport
    /// </summary>
    public interface ILocalReportingService
    {
        /// <summary>
        /// The simpliest rendering using template name and input data
        /// </summary>
        /// <param name="templateName">template name can be taken from jsreport studio or from folder name in VS</param>
        /// <param name="data">any json serializable object</param>
        Report Render(string templateName, object data);
        

        /// <summary>
        /// Complex rendering, see the 
        /// </summary>
        /// <param name="request">Complex dynamic rendering request. See the API dialog in jsreport studio for details</param>
        /// <returns>Report result promise</returns>
        Report Render(object request);

		/// <summary>
		/// Decompress the jsreport.zip and copy server.js and prod.config.json from jsreport/production folder
		/// </summary>
        void Initialize();
    }
}