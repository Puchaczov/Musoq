using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static TableBuildResult ApplyCteContextPruning(
        TableBuildResult result,
        string definitionName,
        CteDefinitionPruningPlan pruningPlan)
    {
        var canDropDynamicProjectedContexts = result.Supported &&
                                              CanDropDynamicProjectedContexts(result.RowShape);
        if (!result.Supported ||
            (!pruningPlan.CanDropContexts(definitionName) && !canDropDynamicProjectedContexts) ||
            result.RowShape.Contexts.Count == 0 ||
            (!canDropDynamicProjectedContexts && !ContainsStoredCteIndex(result.Nodes)))
        {
            return result;
        }

        var oldShape = result.RowShape;
        var rowShape = oldShape with
        {
            Contexts = [],
            RequiresRowBase = false,
            SupportsGeneratedFieldAccess = oldShape.SupportsGeneratedFieldAccess ||
                                           CanUseProjectedGeneratedFieldAccess(oldShape)
        };
        var prunedPayloadTypeNames = CollectCteSidecarPayloadTypeNames(result.Nodes, result.Table, oldShape);
        var shapes = result.Shapes
            .Select(shape => shape switch
            {
                GeneratedRowShape generated when string.Equals(generated.TypeName, oldShape.TypeName, StringComparison.Ordinal) =>
                    rowShape,
                HashPayloadShape payload when prunedPayloadTypeNames.Contains(payload.TypeName) =>
                    payload with { Contexts = [] },
                _ => shape
            })
            .ToArray();
        var nodes = PruneCteContexts(new ExecutionBlock(result.Nodes), result.Table, oldShape, rowShape).Nodes;

        return result with { Shapes = shapes, Nodes = nodes, RowShape = rowShape };
    }

    private static bool CanDropDynamicProjectedContexts(GeneratedRowShape rowShape)
    {
        return rowShape.Contexts.Count > 0 &&
               rowShape.Contexts.All(static context => DynamicEntityBoundary.IsStringObjectDictionaryContext(context.Type.ClrType)) &&
               CanUseProjectedGeneratedFieldAccess(rowShape);
    }

    private static bool CanUseProjectedGeneratedFieldAccess(GeneratedRowShape rowShape)
    {
        return rowShape.Fields.All(static field => field.AccessStrategy is GeneratedFieldAccess);
    }

    private static ExecutionBlock PruneCteContexts(
        ExecutionBlock block,
        ExecutionVariable table,
        GeneratedRowShape oldShape,
        GeneratedRowShape rowShape)
    {
        return new ExecutionBlock(block.Nodes.Select(node => PruneCteContexts(node, table, oldShape, rowShape)).ToArray());
    }

    private static ExecutionNode PruneCteContexts(
        ExecutionNode node,
        ExecutionVariable table,
        GeneratedRowShape oldShape,
        GeneratedRowShape rowShape)
    {
        return node switch
        {
            ExecutionCreateTable create when IsTarget(create.Table, table, create.RowShape, oldShape) =>
                create with { RowShape = rowShape },
            ExecutionAppendRow append when IsTarget(append.Table, table, append.RowShape, oldShape) =>
                append with { RowShape = rowShape, Contexts = [], ContextLayout = null },
            ExecutionCreateGeneratedRow create when IsShape(create.RowShape, oldShape) =>
                create with { RowShape = rowShape, Contexts = [], ContextLayout = null },
            ExecutionMaterializeList materialize when IsShape(materialize.GeneratedRowShape, oldShape) =>
                materialize with { GeneratedRowShape = rowShape },
            ExecutionMaterializeFilteredList materialize when IsShape(materialize.GeneratedRowShape, oldShape) =>
                materialize with { GeneratedRowShape = rowShape },
            ExecutionForEach loop => loop with { Body = PruneCteContexts(loop.Body, table, oldShape, rowShape) },
            ExecutionForEachWithOrdinality loop => loop with { Body = PruneCteContexts(loop.Body, table, oldShape, rowShape) },
            ExecutionForEachIndexed loop => loop with { Body = PruneCteContexts(loop.Body, table, oldShape, rowShape) },
            ExecutionIf branch => branch with { Body = PruneCteContexts(branch.Body, table, oldShape, rowShape) },
            ExecutionScopedBlock scopedBlock => scopedBlock with { Body = PruneCteContexts(scopedBlock.Body, table, oldShape, rowShape) },
            ExecutionHashProbe probe => probe with
            {
                Body = PruneCteContexts(probe.Body, table, oldShape, rowShape),
                NoMatchBody = probe.NoMatchBody == null ? null : PruneCteContexts(probe.NoMatchBody, table, oldShape, rowShape)
            },
            ExecutionKeySetProbe probe => probe with
            {
                Body = PruneCteContexts(probe.Body, table, oldShape, rowShape),
                NoMatchBody = probe.NoMatchBody == null ? null : PruneCteContexts(probe.NoMatchBody, table, oldShape, rowShape)
            },
            ExecutionAsOfProbe probe => probe with
            {
                Body = PruneCteContexts(probe.Body, table, oldShape, rowShape),
                NoMatchBody = probe.NoMatchBody == null ? null : PruneCteContexts(probe.NoMatchBody, table, oldShape, rowShape)
            },
            ExecutionRangeProbe probe => probe with { Body = PruneCteContexts(probe.Body, table, oldShape, rowShape) },
            ExecutionCteSidecarAppendRewriteCandidate { AppendRow: var append } candidate
                when IsTarget(append.Table, table, append.RowShape, oldShape) => candidate with
            {
                AppendRow = append with { RowShape = rowShape, Contexts = [], ContextLayout = null },
                Indexes = PruneCteSidecarPayloadContexts(candidate.Indexes)
            },
            _ => node
        };
    }

    private static HashSet<string> CollectCteSidecarPayloadTypeNames(
        IReadOnlyList<ExecutionNode> nodes,
        ExecutionVariable table,
        GeneratedRowShape rowShape)
    {
        return ExecutionIrAnalysis
            .CollectNodes<ExecutionCteSidecarAppendRewriteCandidate>(new ExecutionBlock(nodes))
            .Where(candidate => IsTarget(candidate.AppendRow.Table, table, candidate.AppendRow.RowShape, rowShape))
            .SelectMany(static candidate => candidate.Indexes)
            .Select(static index => index.PayloadShape?.TypeName)
            .Where(static typeName => !string.IsNullOrWhiteSpace(typeName))
            .ToHashSet(StringComparer.Ordinal)!;
    }

    private static IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> PruneCteSidecarPayloadContexts(
        IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> indexes)
    {
        ExecutionCteSidecarAppendIndexSpec[]? rewritten = null;

        for (var index = 0; index < indexes.Count; index++)
        {
            var current = indexes[index];
            var pruned = PruneCteSidecarPayloadContexts(current);
            if (ReferenceEquals(pruned, current) && rewritten == null)
                continue;

            if (rewritten == null)
            {
                rewritten = new ExecutionCteSidecarAppendIndexSpec[indexes.Count];
                for (var prefix = 0; prefix < index; prefix++)
                    rewritten[prefix] = indexes[prefix];
            }

            rewritten[index] = pruned;
        }

        return rewritten ?? indexes;
    }

    private static ExecutionCteSidecarAppendIndexSpec PruneCteSidecarPayloadContexts(
        ExecutionCteSidecarAppendIndexSpec index)
    {
        if (index.PayloadShape is not { Contexts.Count: > 0 } payloadShape)
            return index;

        var payloadValues = index.PayloadValues
            .Take(payloadShape.Fields.Count)
            .ToArray();

        return index with
        {
            PayloadShape = payloadShape with { Contexts = [] },
            PayloadValues = payloadValues
        };
    }

    private static bool ContainsStoredCteIndex(IReadOnlyList<ExecutionNode> nodes)
    {
        return nodes.Any(static node => node is ExecutionCteSidecarIndexStoreCandidate);
    }

    private static bool IsTarget(
        ExecutionVariable actualTable,
        ExecutionVariable expectedTable,
        GeneratedRowShape actualShape,
        GeneratedRowShape expectedShape)
    {
        return string.Equals(actualTable.Name, expectedTable.Name, StringComparison.Ordinal) &&
               IsShape(actualShape, expectedShape);
    }

    private static bool IsShape(GeneratedRowShape? actual, GeneratedRowShape expected)
    {
        return actual != null && string.Equals(actual.TypeName, expected.TypeName, StringComparison.Ordinal);
    }
}
