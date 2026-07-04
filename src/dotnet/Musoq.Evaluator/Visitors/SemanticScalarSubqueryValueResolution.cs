using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool TryResolveDeferredScalarSubqueryValue(AccessMethodNode node, ArgsListNode args)
    {
        if (!node.IsScalarSubqueryValueWrapper)
            return false;

        if (args.Args is not [var wrappedExpression])
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);

        if (BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(wrappedExpression))
        {
            PushDeferredScalarSubqueryValue(wrappedExpression);
            return true;
        }

        node.IsScalarSubqueryValueWrapper = false;
        return false;
    }

    private void PushDeferredScalarSubqueryValue(Node node)
    {
        Nodes.Push(node);
    }
}
