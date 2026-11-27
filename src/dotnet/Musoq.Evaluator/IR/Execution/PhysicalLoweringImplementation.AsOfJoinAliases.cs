namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool CanUseAsOfProbeSource(RowShape sourceShape, ExecutionVariable sourceVariable)
    {
        if (sourceShape is ValuesRowShape)
            return !string.IsNullOrWhiteSpace(sourceVariable.GeneratedRowTypeName);
        var rowType = sourceVariable.Type.ResolveClrType();
        return sourceShape is SourceEntityShape or TableRowShape
            && !rowType.IsValueType
            && !DynamicEntityBoundary.IsDynamicMetaObjectProvider(rowType);
    }

    private static ExecutionExpression ReplaceExecutionAlias(
        ExecutionExpression expression,
        string fromAlias,
        ExecutionVariable candidate
    )
    {
        var replaced = new ExecutionRowAliasRewriter(
            fromAlias,
            candidate,
            StringComparison.OrdinalIgnoreCase
        ).RewriteExpression(expression);
        if (ReferencesExecutionAlias(replaced, fromAlias))
        {
            throw new NotSupportedException(
                $"Execution IR ASOF join lowering cannot rewrite right-side expression {expression.GetType().Name} for candidate row probing."
            );
        }
        return replaced;
    }
}
