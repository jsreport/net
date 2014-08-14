using System;

namespace jsreport.Client.VSConfiguration
{
    public interface IVSReportingConfiguration
    {
        IVSReportingConfiguration UseRemoteServer(string uri, string username = null, string password = null);

        IVSReportingConfiguration UseEmbedded();

        IVSReportingConfiguration RegisterSchema(string name, object schema);
        IVSReportingConfiguration RegisterSchema(string name, Func<object> fnGetSchema);
    }
}