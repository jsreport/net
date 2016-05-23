using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsreport.Local
{
    /// <summary>
    /// Wrapper for report Stream including some additional information
    /// </summary>
    public class Report
    {
        public Stream Content { get; set; }
    }
}
