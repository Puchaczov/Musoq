using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private const string CorrelatedScalarSubqueryResultName = "__CorrelatedScalarSubqueryResult";

    private bool TryElideCorrelatedScalarSubqueryResultAccessor(AccessMethodNode node, ArgsListNode args)
    {
        if (!string.Equals(node.Name, CorrelatedScalarSubqueryResultName, StringComparison.Ordinal))
            return false;

        if (args.Args is not [var wrappedExpression])
            throw CannotResolveMethodException.CreateForNullArguments(node.Name);

        var returnType = wrappedExpression.ReturnType ?? typeof(object);
        var wrappedType = Nullable.GetUnderlyingType(returnType) ?? returnType;
        if (wrappedType.IsGenericType &&
            wrappedType.GetGenericTypeDefinition() == typeof(CorrelatedScalarSubqueryResult<>))
        {
            return false;
        }

        PushSemanticNode(wrappedExpression);
        return true;
    }

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
        PushSemanticNode(node);
    }
}
