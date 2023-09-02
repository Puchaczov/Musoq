using System;
using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public interface IAwareExpressionVisitor : IScopeAwareExpressionVisitor, IQueryPartAwareExpressionVisitor
    {
    }
}