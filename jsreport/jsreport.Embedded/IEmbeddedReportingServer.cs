using System;
using System.Threading.Tasks;
using jsreport.Client;

namespace jsreport.Embedded
{
    /// <summary>
    /// Class able to start jsreport nodejs server allong with .net process, synchronize local templates with it and mange it's lifecycle
    /// </summary>
    public interface IEmbeddedReportingServer : IDisposable
    {
        /// <summary>
        /// Full uri to running jsreport server like http://localhost:2000/
        /// </summary>
        string EmbeddedServerUri { get; }

        /// <summary>
        /// Extracts jsreport-net-embedded.zip into path to server directory, starts jsreport using nodejs from bin folder
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Sends kill signal to jsreport server and wait for it's exit
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Relative path (from bin) to directory where the jsreport server should be exreacted  and where it should run
        /// You want to use something like ../App_Data for web applications and just null for other types of applications 
        /// where jsreport can stay in bin folder
        /// </summary>
        string RelativePathToServer { get; set; }

        /// <summary>
        /// Takes precedence over RelativePathToServer and specifies directory where jsreport server should be extracted and run
        /// </summary>
        string AbsolutePathToServer { get; set; }

        /// <summary>
        /// To avoid orphans of nodejs processes jsreport server kills itself when no ping is comming from .NET process.
        /// EmbeddedReportingServer takes care of sending regular ping to jsreport server.
        /// PingTimeout specifies time how to keep jsreport nodejs process runing when no ping is comming from .NET
        /// </summary>
        TimeSpan PingTimeout { get; set; }

        /// <summary>
        /// Shortcut to new ReportingService(EmbeddedServerUri)
        /// </summary>
        IReportingService ReportingService { get;}
    }
}