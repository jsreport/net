using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace JsReport.Query
{
    public class ReportQuery<T> : IQueryable<T> 
    {
        public ReportingService ReportingService { get; set; }

        public ReportQuery(ReportingService reportingService, ReportQueryProvider provider, Expression expression)
        {
            ReportingService = reportingService;
            Provider = provider;
            Expression = expression ?? Expression.Constant(this);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var execute = Provider.Execute(Expression);
            return ((IEnumerable<T>)execute).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression { get; private set; }
        public Type ElementType { get; private set; }
        public IQueryProvider Provider { get; private set; }
    }
}