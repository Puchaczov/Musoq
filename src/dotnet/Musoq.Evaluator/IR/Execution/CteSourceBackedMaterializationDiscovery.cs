using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteSourceBackedMaterializationDiscovery
{
    public static bool TryFindStoredRowsLoop(
        ExecutionBlock body,
        out int loopIndex,
        [NotNullWhen(true)] out ExecutionForEach? loop,
        out int tableIndex)
    {
        loopIndex = -1;
        loop = null;
        tableIndex = -1;

        for (var index = 0; index < body.Nodes.Count; index++)
        {
            if (body.Nodes[index] is not ExecutionForEach { Source: ExecutionStoredTableRows storedRows } current)
                continue;

            if (loop != null)
                return false;

            loopIndex = index;
            loop = current;
            tableIndex = storedRows.TableIndex;
        }

        return loop != null;
    }

    public static bool TryResolveCteName(
        IReadOnlyDictionary<string, int> cteIndexes,
        int tableIndex,
        [NotNullWhen(true)] out string? cteName)
    {
        foreach (var (name, index) in cteIndexes)
        {
            if (index == tableIndex)
            {
                cteName = name;
                return true;
            }
        }

        cteName = null;
        return false;
    }

    public static bool TryExtractSourceMaterialization(
        IReadOnlyList<ExecutionNode> builtNodes,
        int tableIndex,
        IReadOnlyDictionary<string, SourceInteractionPlan> sourceInteractionPlans,
        [NotNullWhen(true)] out CteSourceBackedMaterialization? materialization)
    {
        materialization = null;
        if (builtNodes.Count < 3 ||
            builtNodes[^1] is not ExecutionStoreTable storeTable ||
            storeTable.TableIndex != tableIndex ||
            builtNodes[^2] is not ExecutionSourceLoop loop ||
            builtNodes[^3] is not ExecutionCreateTable createTable ||
            !string.Equals(createTable.Table.Name, storeTable.Table.Name, StringComparison.Ordinal) ||
            !TryFindSingleTargetAppend(loop.Body, createTable, out var appendRow) ||
            !TryResolveLoopRowsVariable(loop.Source, out var rowsVariable) ||
            !TryResolveSourceScan(builtNodes, rowsVariable, out var sourceScan) ||
            !CanUseSourceBackedFusion(sourceScan, sourceInteractionPlans) ||
            HasUnsafeMappedExpression(appendRow))
        {
            return false;
        }

        materialization = new CteSourceBackedMaterialization(
            builtNodes.Count - 3,
            builtNodes.Count - 1,
            createTable,
            loop,
            appendRow);
        return true;
    }

    private static bool TryFindSingleTargetAppend(
        ExecutionBlock block,
        ExecutionCreateTable createTable,
        [NotNullWhen(true)] out ExecutionAppendRow? appendRow)
    {
        var matches = ExecutionIrAnalysis
            .CollectNodes<ExecutionAppendRow>(block)
            .Where(append => string.Equals(append.Table.Name, createTable.Table.Name, StringComparison.Ordinal) &&
                             string.Equals(append.RowShape.TypeName, createTable.RowShape.TypeName, StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        appendRow = matches.Length == 1 ? matches[0] : null;
        return appendRow != null;
    }

    private static bool TryResolveLoopRowsVariable(
        ExecutionExpression source,
        [NotNullWhen(true)] out ExecutionVariable? rowsVariable)
    {
        rowsVariable = source switch
        {
            ExecutionRowStream rows => rows.Variable,
            _ => null
        };

        return rowsVariable != null;
    }

    private static bool TryResolveSourceScan(
        IReadOnlyList<ExecutionNode> builtNodes,
        ExecutionVariable rowsVariable,
        [NotNullWhen(true)] out ExecutionSourceScan? sourceScan)
    {
        sourceScan = builtNodes
            .OfType<ExecutionSourceScan>()
            .LastOrDefault(scan => string.Equals(scan.Rows.Name, rowsVariable.Name, StringComparison.Ordinal));

        return sourceScan != null;
    }

    private static bool CanUseSourceBackedFusion(
        ExecutionSourceScan sourceScan,
        IReadOnlyDictionary<string, SourceInteractionPlan> sourceInteractionPlans)
    {
        if (string.IsNullOrWhiteSpace(sourceScan.Binding.RuntimeContextId))
            return false;

        sourceInteractionPlans.TryGetValue(sourceScan.Binding.RuntimeContextId, out var plan);
        return plan is
        {
            Confidence: not PlanningConfidence.Low,
            ArgumentMode: SourceArgumentMode.ConstantArguments,
            ShapeKind: SourceShapeKind.KnownClr or SourceShapeKind.ExternallyProvidedType or SourceShapeKind.InferredMetadata
        };
    }

    private static bool HasUnsafeMappedExpression(ExecutionAppendRow appendRow)
    {
        return appendRow.Values.Select(static value => value.Value)
            .Concat(appendRow.Contexts)
            .Concat(ExecutionIrAnalysis.GetContextLayoutExpressions(appendRow.ContextLayout))
            .SelectMany(ExecutionIrAnalysis.FlattenExpressions)
            .Any(static expression => expression is ExecutionMethodCall);
    }
}
