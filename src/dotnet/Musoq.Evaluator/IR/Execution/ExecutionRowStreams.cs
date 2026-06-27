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
            ExecutionRowStream stream => new ExecutionRowStream(variable, stream.Kind),
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
        if (sourceShape is not ExpandoAdapterShape expando)
        {
            return new ExecutionForEach(source, sourceRows, loopBody);
        }

        var resolver = new ExecutionVariable(CreateResolverVariableName(source.Name), expando.RuntimeType);
        var adapter = new ExecutionVariable(source.Name, typeof(object));
        var body = new ExecutionBlock(
            [
                new ExecutionAdaptExpando(adapter, resolver, expando),
                ..loopBody.Nodes
            ]);

        return new ExecutionForEach(resolver, sourceRows, body);
    }

    public static ExecutionNode CreateForEachWithOrdinality(
        RowShape sourceShape,
        ExecutionExpression sourceRows,
        ExecutionVariable source,
        ExecutionVariable ordinal,
        ExecutionBlock loopBody)
    {
        if (sourceShape is not ExpandoAdapterShape expando)
        {
            return new ExecutionForEachWithOrdinality(source, sourceRows, ordinal, loopBody);
        }

        var resolver = new ExecutionVariable(CreateResolverVariableName(source.Name), expando.RuntimeType);
        var adapter = new ExecutionVariable(source.Name, typeof(object));
        var body = new ExecutionBlock(
            [
                new ExecutionAdaptExpando(adapter, resolver, expando),
                ..loopBody.Nodes
            ]);

        return new ExecutionForEachWithOrdinality(resolver, sourceRows, ordinal, body);
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

    public static ExecutionNode CreateMaterializeExpandoList(
        ExecutionExpression source,
        ExecutionVariable buffer,
        ExpandoAdapterShape shape,
        ExecutionExpression? predicate)
    {
        return new ExecutionMaterializeExpandoList(source, buffer, shape, predicate);
    }

    private static string CreateResolverVariableName(string alias)
    {
        return $"{alias}Resolver";
    }
}
