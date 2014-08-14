using System;
using System.Collections.Generic;

namespace jsreport.Client.VSConfiguration
{
    public class VSReportingConfiguration : IVSReportingConfiguration
    {
        public VSReportingConfiguration()
        {
            Schemas = new Dictionary<string, object>();
            DynamicSchemas = new Dictionary<string, Func<object>>();
        }

        public string RemoteServerUri { get; private set; }
        public string RemoteServerUsername { get; private set; }
        public string RemoteServerPassword { get; private set; }
        public bool UseEmbeddedServer { get { return string.IsNullOrEmpty(RemoteServerUri); } }

        public IDictionary<string, object> Schemas { get; private set; }
        public IDictionary<string, Func<object>> DynamicSchemas { get; private set; }

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

        public IVSReportingConfiguration RegisterSchema(string name, object schema)
        {
            Schemas.Add(name, schema);
            return this;
        }

        public IVSReportingConfiguration RegisterSchema(string name, Func<object> fnGetSchema)
        {
            DynamicSchemas.Add(name, fnGetSchema);
            return this;
        }
    }
}