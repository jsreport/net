using System;

namespace jsreport.Client.VSConfiguration
{
    public interface IVSReportingConfiguration
    {
        IVSReportingConfiguration UseRemoteServer(string uri, string username = null, string password = null);

        IVSReportingConfiguration UseEmbedded();

        IVSReportingConfiguration RegisterSampleData(string name, object sampleData);
        IVSReportingConfiguration RegisterSampleData(string name, Func<object> fnGetSampleData);
    }
}