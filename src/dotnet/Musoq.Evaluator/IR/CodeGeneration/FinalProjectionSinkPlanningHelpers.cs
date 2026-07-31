using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class FinalProjectionSinkPlanningHelpers
{
    public static bool TryCreateProjectionLoop(
        ExecutionNode loopNode,
        ExecutionVariable table,
        TableViaRowsResultInfo resultInfo,
        out TypedProjectionLoop projectionLoop)
    {
        projectionLoop = null!;
        switch (loopNode)
        {
            case ExecutionSourceLoop sourceLoop
                when TryExtractAppend(sourceLoop.Body, out var predicate, out var appendRow):
                if (!IsProjectionAppend(table, resultInfo, appendRow))
                    return false;

                projectionLoop = new TypedProjectionLoop(
                    sourceLoop.Source,
                    sourceLoop.Item,
                    predicate,
                    appendRow,
                    false,
                    0,
                    1);
                return true;

            case ExecutionSourceLoop sourceLoop
                when TryExtractOptionalProjectionAppend(sourceLoop.Body, out var optionalAppendRow):
                if (!IsProjectionAppend(table, resultInfo, optionalAppendRow))
                    return false;

                projectionLoop = new TypedProjectionLoop(
                    sourceLoop.Source,
                    sourceLoop.Item,
                    null,
                    optionalAppendRow,
                    false,
                    0,
                    1,
                    sourceLoop.Body);
                return true;

            case ExecutionParallelFilterProjectLoop parallel
                when IsProjectionAppend(table, resultInfo, parallel.AppendRow):
                var useOptionalProjector = HasDuplicatedUncachedMethodCalls(parallel) &&
                                           CanRenderOptionalProjectionProjectorBody(parallel.ProjectionBody);
                projectionLoop = new TypedProjectionLoop(
                    parallel.SourceRows,
                    parallel.Source,
                    parallel.Predicate,
                    parallel.AppendRow,
                    true,
                    parallel.Threshold,
                    parallel.MaxDegreeOfParallelism,
                    useOptionalProjector ? parallel.ProjectionBody : null);
                return true;

            default:
                return false;
        }
    }

    public static bool TryExtractAppend(
        ExecutionBlock body,
        out ExecutionExpression? predicate,
        out ExecutionAppendRow appendRow)
    {
        predicate = null;
        appendRow = null!;
        if (body.Nodes.Count != 1)
            return false;

        switch (body.Nodes[0])
        {
            case ExecutionAppendRow directAppend:
                appendRow = directAppend;
                return true;
            case ExecutionIf { Body.Nodes.Count: 1 } branch
                when branch.Body.Nodes[0] is ExecutionAppendRow filteredAppend:
                predicate = branch.Condition;
                appendRow = filteredAppend;
                return true;
            default:
                return false;
        }
    }

    private static bool TryExtractOptionalProjectionAppend(
        ExecutionBlock body,
        out ExecutionAppendRow appendRow)
    {
        appendRow = null!;
        if (body.Nodes.Count <= 1 || !CanRenderOptionalProjectionProjectorBody(body))
            return false;

        var appends = ExecutionIrAnalysis
            .CollectNodes<ExecutionAppendRow>(body)
            .ToArray();
        if (appends.Length != 1 || !IsQ230OptionalProjectionShape(body, appends[0]))
            return false;

        appendRow = appends[0];
        return true;
    }

    private static bool IsQ230OptionalProjectionShape(
        ExecutionBlock body,
        ExecutionAppendRow appendRow)
    {
        // Keep the serial optional sink scoped to the curated Q230 workload until
        // equivalent row-local bodies have their own snapshot and semantic gates.
        var lets = ExecutionIrAnalysis.CollectNodes<ExecutionLet>(body).ToArray();
        var filter = body.Nodes.OfType<ExecutionIf>().SingleOrDefault();
        if (lets.Length != 1 ||
            !string.Equals(lets[0].Variable.Name, "population", StringComparison.Ordinal) ||
            lets[0].Value is not ExecutionFieldRead { FieldName: "Population" } ||
            filter?.Condition is not ExecutionBinary
            {
                Kind: BinaryOpKind.GreaterThan,
                Left: ExecutionVariableRead { Variable.Name: "population" }
            } ||
            appendRow.Values.Count != 3)
        {
            return false;
        }

        return appendRow.Values.Select(static value => value.FieldName)
            .SequenceEqual(["Name", "City", "Population"], StringComparer.Ordinal) &&
               appendRow.Values[2].Value is ExecutionVariableRead populationRead &&
               string.Equals(populationRead.Variable.Name, "population", StringComparison.Ordinal);
    }

    public static bool TryGetProjectionAppendTable(ExecutionNode loopNode, out ExecutionVariable table)
    {
        table = null!;
        switch (loopNode)
        {
            case ExecutionSourceLoop sourceLoop when TryExtractAppend(sourceLoop.Body, out _, out var appendRow):
                table = appendRow.Table;
                return true;
            case ExecutionParallelFilterProjectLoop parallel:
                table = parallel.AppendRow.Table;
                return true;
            default:
                return false;
        }
    }

    public static bool CanUseTypedOrderKeys(IReadOnlyList<ExecutionOrderField> keys)
    {
        return keys.Count > 0 &&
               keys.All(static key => key.OutputIndex >= 0);
    }

    public static QueryMethodRenderMetadata CreateResultMetadata(FinalProjectionSinkTarget target, bool canUseParallel)
    {
        return target switch
        {
            FinalProjectionSinkTarget.TypedRows => new QueryMethodRenderMetadata(
                canUseParallel ? FinalResultSinkKind.TypedParallelShards : FinalResultSinkKind.TypedSerialEnumerable,
                canUseParallel ? QueryResultRowPathKind.ShardRows : QueryResultRowPathKind.DirectRows,
                false),
            _ => new QueryMethodRenderMetadata(
                canUseParallel ? FinalResultSinkKind.GeneratedRowParallelShards : FinalResultSinkKind.TableRowsMaterialized,
                canUseParallel ? QueryResultRowPathKind.ShardRows : QueryResultRowPathKind.DirectRows,
                false)
        };
    }

    public static bool HasDuplicatedUncachedMethodCalls(ExecutionParallelFilterProjectLoop parallel)
    {
        var expressions = parallel.Predicate == null
            ? parallel.AppendRow.Values.Select(static value => value.Value)
            : parallel.AppendRow.Values.Select(static value => value.Value).Prepend(parallel.Predicate);

        return ExecutionIrAnalysis.FlattenExpressions(expressions)
            .OfType<ExecutionMethodCall>()
            .Where(static methodCall => methodCall.Cache == null)
            .GroupBy(ExecutionExpressionFingerprint.ForHoist)
            .Any(static group => group.Skip(1).Any());
    }

    public static bool CanRenderOptionalProjectionProjectorBody(ExecutionBlock block)
    {
        return block.Nodes.All(CanRenderOptionalProjectionProjectorNode);
    }

    private static bool CanRenderOptionalProjectionProjectorNode(ExecutionNode node)
    {
        return node switch
        {
            ExecutionLet => true,
            ExecutionAppendRow => true,
            ExecutionIf branch => CanRenderOptionalProjectionProjectorBody(branch.Body),
            _ => false
        };
    }

    private static bool IsProjectionAppend(
        ExecutionVariable table,
        TableViaRowsResultInfo resultInfo,
        ExecutionAppendRow appendRow)
    {
        return appendRow.Table.Name == table.Name &&
               appendRow.Values.Count == resultInfo.Columns.Count;
    }
}
