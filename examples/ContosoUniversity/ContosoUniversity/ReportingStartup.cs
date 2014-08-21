using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Web;
using ContosoUniversity.DAL;
using jsreport.Client.VSConfiguration;

namespace ContosoUniversity
{
    public class ReportingStartup
    {
        public void Configure(IVSReportingConfiguration configuration)
        {
            var db = new SchoolContext("Data Source=(LocalDb)\\v11.0;Initial Catalog=ContosoUniversity2;Integrated Security=SSPI;");

            //register dynamic sample data as an action loading data from local db
            configuration.RegisterSampleData("departments", db.QueryDepartmentsForReport);
            configuration.RegisterSampleData("students", db.QueryStudentsReport);
        }
    }
}