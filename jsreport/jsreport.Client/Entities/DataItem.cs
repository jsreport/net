using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jsreport.Client.Entities
{
    /// <summary>
    /// Allows to store example json schemas representing future report input data
    /// See data extension
    /// </summary>
    public class DataItem
    {
        public string _id { get; set; }
        public string shortid { get; set; }
        public string name { get; set; }
        public string dataJson { get; set; }
        public DateTimeOffset? creationDate { get; set; }
        public DateTimeOffset? modificationDate { get; set; }
    }
}
