using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace JsReport.Query
{
    public class ReportQueryProvider : IQueryProvider
    {
        public ReportingService ReportingService { get; set; }

        public ReportQueryProvider(ReportingService reportingService)
        {
            ReportingService = reportingService;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var makeGenericType = typeof(ReportQuery<>).MakeGenericType(expression.Type);
            var args = new object[]
				{
					ReportingService, this, expression, 
				};
            return (IQueryable)Activator.CreateInstance(makeGenericType, args);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new ReportQuery<TElement>(ReportingService, this, expression);
        }

        public object Execute(Expression expression)
        {
            VisitExpression(expression);
            return null;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new System.NotImplementedException();
        }

        private void VisitExpression(Expression expression)
        {
            if (expression is BinaryExpression)
            {
                VisitBinaryExpression((BinaryExpression)expression);
            }
            else
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.MemberAccess:
                        VisitMemberAccess((MemberExpression)expression, true);
                        break;
                    case ExpressionType.Not:
                        var unaryExpressionOp = ((UnaryExpression)expression).Operand;
                        switch (unaryExpressionOp.NodeType)
                        {
                            case ExpressionType.MemberAccess:
                                VisitMemberAccess((MemberExpression)unaryExpressionOp, false);
                                break;
                            case ExpressionType.Call:
                                // probably a call to !In() or !string.IsNullOrEmpty()
                                VisitMethodCall((MethodCallExpression)unaryExpressionOp, negated: true);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(unaryExpressionOp.NodeType.ToString());
                        }
                        break;
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        VisitExpression(((UnaryExpression)expression).Operand);
                        break;
                    default:
                        if (expression is MethodCallExpression)
                        {
                            VisitMethodCall((MethodCallExpression)expression);
                        }
                        else if (expression is LambdaExpression)
                        {
                            VisitExpression(((LambdaExpression)expression).Body);
                        }
                        break;
                }
            }

        }

        private void VisitMemberAccess(MemberExpression expression, bool negated)
        {
            
        }

        private void VisitMethodCall(MethodCallExpression expression, bool negated = false)
        {
            var declaringType = expression.Method.DeclaringType;
            Debug.Assert(declaringType != null);
            if (declaringType != typeof(string) && expression.Method.Name == "Equals")
            {
                switch (expression.Arguments.Count)
                {
                    case 1:
                        VisitEquals(Expression.MakeBinary(ExpressionType.Equal, expression.Object, expression.Arguments[0]));
                        break;
                    case 2:
                        VisitEquals(Expression.MakeBinary(ExpressionType.Equal, expression.Arguments[0], expression.Arguments[1]));
                        break;
                    default:
                        throw new ArgumentException("Can't understand Equals with " + expression.Arguments.Count + " arguments");
                }
                return;
            }
            
            if (declaringType == typeof(Queryable))
            {
                VisitQueryableMethodCall(expression);
                return;
            }

            //if (declaringType == typeof(String))
            //{
            //    VisitStringMethodCall(expression);
            //    return;
            //}

            //if (declaringType == typeof(Enumerable))
            //{
            //    VisitEnumerableMethodCall(expression, negated);
            //    return;
            //}
            
            //if (declaringType.IsGenericType &&
            //    declaringType.GetGenericTypeDefinition() == typeof(List<>))
            //{
            //    VisitListMethodCall(expression);
            //    return;
            //}
            
            var method = declaringType.Name + "." + expression.Method.Name;
            throw new NotSupportedException(string.Format("Method not supported: {0}. Expression: {1}.", method, expression));
	
        }

        private void VisitQueryableMethodCall(MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "OfType":
                    VisitExpression(expression.Arguments[0]);
                    break;
                case "Where":
                    {
                        VisitExpression(expression.Arguments[0]);
                        VisitExpression(((UnaryExpression) expression.Arguments[1]).Operand);

                        break;
                    }
            }
        }

        private void VisitEquals(BinaryExpression expression)
        {
            //var constantExpression = expression.Right as ConstantExpression;
            //if (constantExpression != null && true.Equals(constantExpression.Value))
            //{
            //    VisitExpression(expression.Left);
            //    return;
            //}


            //if (constantExpression != null && false.Equals(constantExpression.Value) &&
            //    expression.Left.NodeType != ExpressionType.MemberAccess)
            //{
            //    VisitExpression(expression.Left);
            //    return;
            //}

            //var methodCallExpression = expression.Left as MethodCallExpression;
            
            //if (methodCallExpression != null && methodCallExpression.Method.Name == "CompareString" &&
            //    expression.Right.NodeType == ExpressionType.Constant &&
            //    Equals(((ConstantExpression)expression.Right).Value, 0))
            //{
            //    var expressionMemberInfo = GetMember(methodCallExpression.Arguments[0]);

            //    luceneQuery.WhereEquals(
            //        new WhereParams
            //        {
            //            FieldName = expressionMemberInfo.Path,
            //            Value = GetValueFromExpression(methodCallExpression.Arguments[1], GetMemberType(expressionMemberInfo)),
            //            IsAnalyzed = true,
            //            AllowWildcards = false
            //        });
            //    return;
            //}

            //if (IsMemberAccessForQuerySource(expression.Left) == false && IsMemberAccessForQuerySource(expression.Right))
            //{
            //    VisitEquals(Expression.Equal(expression.Right, expression.Left));
            //    return;
            //}

            //var memberInfo = GetMember(expression.Left);

            //luceneQuery.WhereEquals(new WhereParams
            //{
            //    FieldName = memberInfo.Path,
            //    Value = GetValueFromExpression(expression.Right, GetMemberType(memberInfo)),
            //    IsAnalyzed = true,
            //    AllowWildcards = false,
            //    IsNestedPath = memberInfo.IsNestedPath
            //});    
        }

        private void VisitBinaryExpression(BinaryExpression expression)
        {
            
        }
    }
}