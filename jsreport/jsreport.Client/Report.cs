using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace jsreport.Client
{
    /// <summary>
    /// Output of jsreport rendering process
    /// </summary>
    [Serializable]
    public class Report
    {
        /// <summary>
        /// Stream with report
        /// </summary>
        public Stream Content { get; set; }

        /// <summary>
        /// Report content type like application/pdf
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; set; }
        
        /// <summary>
        /// Report file extension like "html" or "pdf"
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Optional pernament link to report
        /// </summary>
        public string PermanentLink { get; set; }

        public HttpResponseMessage Response { get; set; }
    }
}