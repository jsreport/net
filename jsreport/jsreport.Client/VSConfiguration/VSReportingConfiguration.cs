using System;
using System.Collections.Generic;

namespace jsreport.Client.VSConfiguration
{
    public class VSReportingConfiguration : IVSReportingConfiguration
    {
        public VSReportingConfiguration()
        {
            SampleData = new Dictionary<string, object>();
            DynamicSampleData = new Dictionary<string, Func<object>>();
        }

        public string RemoteServerUri { get; private set; }
        public string RemoteServerUsername { get; private set; }
        public string RemoteServerPassword { get; private set; }
        public bool UseEmbeddedServer { get { return string.IsNullOrEmpty(RemoteServerUri); } }

        public IDictionary<string, object> SampleData { get; private set; }
        public IDictionary<string, Func<object>> DynamicSampleData { get; private set; }

        public IVSReportingConfiguration UseRemoteServer(string uri, string username = null, string password = null)
        {
            RemoteServerUri = uri;
            RemoteServerUsername = username;
            RemoteServerPassword = password;

            return this;
        }

        public IVSReportingConfiguration UseEmbedded()
        {
            RemoteServerUri = null;
            return this;
        }

        public IVSReportingConfiguration RegisterSampleData(string name, object sampleData)
        {
            SampleData.Add(name, sampleData);
            return this;
        }

        public IVSReportingConfiguration RegisterSampleData(string name, Func<object> fnSampleData)
        {
            DynamicSampleData.Add(name, fnSampleData);
            return this;
        }
    }
}