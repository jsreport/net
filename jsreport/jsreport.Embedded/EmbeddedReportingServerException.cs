using System;
using System.Runtime.Serialization;

namespace jsreport.Embedded
{
    public class EmbeddedReportingServerException : Exception
    {
        public EmbeddedReportingServerException()
        {
        }

        public EmbeddedReportingServerException(string message) : base(message)
        {
        }

        public EmbeddedReportingServerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EmbeddedReportingServerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public string ServerStandardOutput { get; set; }
        public string ServerErrorOutput { get; set; }
    }
}