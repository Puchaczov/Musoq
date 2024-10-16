using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public interface IAwareExpressionVisitor : IScopeAwareExpressionVisitor, IQueryPartAwareExpressionVisitor
{
    void SetTheMostInnerIdentifierOfDotNode(IdentifierNode node);
    
    void SetOperatorLeftFinished();
}