using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class AccessMethodCallRewriter
{
    public static AccessMethodNode Rewrite(AccessMethodNode source, ArgsListNode arguments, Node? filter)
    {
        return new AccessMethodNode(source.FunctionToken, arguments, null, source.CanSkipInjectSource,
            source.Method, source.Alias, default, source.IsDistinct)
        {
            HasFilter = source.HasFilter,
            FilterExpression = filter,
            FilterExpressionText = source.FilterExpressionText,
            IsPivotGenerated = source.IsPivotGenerated,
            IsScalarSubqueryValueWrapper = source.IsScalarSubqueryValueWrapper,
            IsCompilerGenerated = source.IsCompilerGenerated
        };
    }
}
