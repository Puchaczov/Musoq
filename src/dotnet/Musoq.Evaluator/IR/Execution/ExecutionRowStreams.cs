namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionRowStreams
{
    public static bool IsChunked(ExecutionExpression rows)
    {
        return rows is ExecutionRowStream { Kind: ExecutionRowStreamKind.Chunks };
    }

    public static bool IsScalar(ExecutionExpression rows)
    {
        return rows is ExecutionScalarRowStream;
    }

    public static ExecutionExpression RebindLike(
        ExecutionExpression rows,
        ExecutionVariable variable)
    {
        return rows switch
        {
            ExecutionRowStream stream => new ExecutionRowStream(variable, stream.Kind)
                { HasDirectScalarElements = stream.HasDirectScalarElements, IsValueEnumerable = stream.IsValueEnumerable },
            ExecutionScalarRowStream => new ExecutionScalarRowStream(variable),
            _ => new ExecutionVariableRead(variable)
        };
    }

    public static ExecutionSourceLoop CreateForEach(
        RowShape sourceShape,
        ExecutionExpression sourceRows,
        ExecutionVariable source,
        ExecutionBlock loopBody)
    {
        RequireNativeShape(sourceShape);
        return new ExecutionForEach(source, sourceRows, loopBody);
    }

    public static ExecutionForEachIndexed CreateForEachIndexed(
        RowShape sourceShape,
        ExecutionVariable sourceRows,
        ExecutionVariable source,
        ExecutionVariable index,
        ExecutionBlock loopBody)
    {
        RequireNativeShape(sourceShape);
        return new ExecutionForEachIndexed(source, index, sourceRows, ExecutionRowAccessMode.Direct, loopBody);
    }

    public static ExecutionNode CreateForEachWithOrdinality(
        RowShape sourceShape,
        ExecutionExpression sourceRows,
        ExecutionVariable source,
        ExecutionVariable ordinal,
        ExecutionBlock loopBody)
    {
        RequireNativeShape(sourceShape);
        return new ExecutionForEachWithOrdinality(source, sourceRows, ordinal, loopBody);
    }

    public static ExecutionNode CreateMaterializeList(
        ExecutionExpression source,
        ExecutionVariable buffer,
        GeneratedRowShape? generatedRowShape = null)
    {
        return new ExecutionMaterializeList(source, buffer, generatedRowShape);
    }

    public static ExecutionNode CreateMaterializeFilteredList(
        ExecutionExpression source,
        ExecutionVariable buffer,
        ExecutionVariable item,
        ExecutionRowAccessMode rowAccessMode,
        ExecutionExpression predicate,
        GeneratedRowShape? generatedRowShape = null)
    {
        return new ExecutionMaterializeFilteredList(source, buffer, item, rowAccessMode, predicate, generatedRowShape);
    }



    private static void RequireNativeShape(RowShape shape)
    {
        if (shape is SourceEntityShape source &&
            DynamicEntityBoundary.IsNameIndexedSource(source.EntityType.ResolveClrType()))
            throw new Musoq.Evaluator.Exceptions.SourcePlanContractException(
                "Erased dictionary iteration is unsupported; use native query-scoped rows.",
                Musoq.Parser.TextSpan.Empty);
    }
}
