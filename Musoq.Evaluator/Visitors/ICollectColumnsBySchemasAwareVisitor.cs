using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors
{
    public interface ICollectColumnsBySchemasAwareVisitor : IExpressionVisitor
    {
        void SetInsideCte();
        void UnsetInsideCte();
        void IsInsideInnerCte(string name);
        void IsOutsideInnerCte(string name);
        void SetJoiningTablesPossible();
        void UnsetJoiningTablesPossible();
        void SetQueryStarts();
        void UnsetQueryStarts();
        void SetInsideSetOperator();
        void UnsetInsideSetOperator();
        void SetQueryPart(QueryPart part);
    }
}
