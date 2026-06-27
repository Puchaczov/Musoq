using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record CteSidecarIndexBuild(
        CteSidecarIndexSpec Spec,
        ExecutionVariable Index,
        HashPayloadShape? PayloadShape);

    private TableBuildResult ApplyCteSidecarIndexes(
        TableBuildResult result,
        IReadOnlyList<CteSidecarIndexSpec> specs)
    {
        if (specs.Count == 0)
            return result;

        var block = new ExecutionBlock(result.Nodes);
        if (ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(block).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(block).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionParallelSingleKeyAggregateLoop>(block).Any())
        {
            return UnsupportedSelectedCteSidecarIndexes(
                specs,
                "selected CTE sidecar indexes cannot be materialized from a parallel CTE producer.");
        }

        var nodes = result.Nodes.ToList();
        var createTableIndex = nodes.FindIndex(node =>
            node is ExecutionCreateTable createTable &&
            string.Equals(createTable.Table.Name, result.Table.Name, StringComparison.Ordinal));
        if (createTableIndex < 0 || nodes[createTableIndex] is not ExecutionCreateTable createTable)
        {
            return UnsupportedSelectedCteSidecarIndexes(
                specs,
                $"selected CTE sidecar indexes require a materialized CTE table '{result.Table.Name}'.");
        }

        var canUseHashPayloads = CanUseCteSidecarHashPayloads(block, result.Table.Name, result.RowShape);
        var builds = specs
            .OrderBy(static spec => spec.IndexSlot)
            .Select(spec => CreateCteSidecarIndexBuild(result.Table.Name, result.RowShape, spec, canUseHashPayloads))
            .ToArray();
        var transformed = TransformCteSidecarAppendBlock(
            new ExecutionBlock(nodes),
            result.Table,
            result.RowShape,
            builds);
        if (transformed.AppendCount == 0)
        {
            return UnsupportedSelectedCteSidecarIndexes(
                specs,
                "selected CTE sidecar indexes require at least one producer append row to rewrite.");
        }

        nodes = transformed.Block.Nodes.ToList();
        var capacityHint = createTable.CapacityHint ?? transformed.CapacityHint;
        if (createTable.CapacityHint == null && capacityHint != null)
        {
            createTable = createTable with { CapacityHint = capacityHint };
            nodes[createTableIndex] = createTable;
        }

        var createIndexSpecs = builds
            .Select(build => CreateCteSidecarCreateSpec(
                build,
                result.RowShape,
                CreateCteSidecarCapacityCandidate(capacityHint, build.Index)))
            .ToArray();
        nodes.Insert(
            createTableIndex + 1,
            new ExecutionCteSidecarIndexBuildCandidate(createIndexSpecs));
        nodes.AddRange(builds.Select(build => new ExecutionCteSidecarIndexStoreCandidate(
            build.Index,
            build.Spec.IndexSlot,
            ToExecutionIndexKind(build.Spec.Kind),
            build.Spec.KeyType,
            build.Spec.Kind == CteSidecarIndexKind.Hash ? typeof(Row) : null,
            build.Spec.Kind == CteSidecarIndexKind.Hash
                ? build.PayloadShape?.TypeName ?? result.RowShape.TypeName
                : null)));

        var payloadShapes = builds
            .Select(static build => build.PayloadShape)
            .OfType<HashPayloadShape>()
            .ToArray();

        return result with { Nodes = nodes, Shapes = [..result.Shapes, ..payloadShapes] };
    }

    private static TableBuildResult UnsupportedSelectedCteSidecarIndexes(
        IReadOnlyList<CteSidecarIndexSpec> specs,
        string reason)
    {
        var slots = string.Join(", ", specs.Select(static spec => spec.IndexSlot.ToString(CultureInfo.InvariantCulture)));
        return TableBuildResult.Unsupported(
            $"Execution IR CTE sidecar lowering cannot silently drop planner-selected sidecar index slot(s) [{slots}]: {reason}");
    }

    private static ExecutionCteSidecarIndexCreateSpec CreateCteSidecarCreateSpec(
        CteSidecarIndexBuild build,
        GeneratedRowShape rowShape,
        ExecutionCapacityHint? capacityHint)
    {
        return build.Spec.Kind switch
        {
            CteSidecarIndexKind.Hash => new ExecutionCteSidecarIndexCreateSpec(
                build.Index,
                ExecutionCteSidecarIndexKind.Hash,
                build.Spec.KeyType,
                capacityHint,
                typeof(Row),
                build.PayloadShape?.TypeName ?? rowShape.TypeName),
            CteSidecarIndexKind.KeySet => new ExecutionCteSidecarIndexCreateSpec(
                build.Index,
                ExecutionCteSidecarIndexKind.KeySet,
                build.Spec.KeyType,
                capacityHint),
            _ => throw UnsupportedShape.Of($"CTE sidecar index kind {build.Spec.Kind}")
        };
    }

    private static CteSidecarAppendTransformResult TransformCteSidecarAppendBlock(
        ExecutionBlock block,
        ExecutionVariable targetTable,
        GeneratedRowShape rowShape,
        IReadOnlyList<CteSidecarIndexBuild> builds)
    {
        var nodes = new List<ExecutionNode>(block.Nodes.Count);
        var appendCount = 0;
        ExecutionCapacityHint? capacityHint = null;

        foreach (var node in block.Nodes)
        {
            if (node is ExecutionAppendRow appendRow &&
                string.Equals(appendRow.Table.Name, targetTable.Name, StringComparison.Ordinal) &&
                string.Equals(appendRow.RowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal))
            {
                nodes.Add(new ExecutionCteSidecarAppendRewriteCandidate(
                    appendRow,
                    CreateCteSidecarAppendIndexes(appendRow, builds)));
                appendCount++;
                continue;
            }

            var nested = TransformCteSidecarAppendNode(node, targetTable, rowShape, builds);
            nodes.Add(nested.Node);
            appendCount += nested.AppendCount;
            capacityHint ??= nested.CapacityHint;
        }

        return new CteSidecarAppendTransformResult(new ExecutionBlock(nodes), appendCount, capacityHint);
    }

    private static CteSidecarAppendNodeTransformResult TransformCteSidecarAppendNode(
        ExecutionNode node,
        ExecutionVariable targetTable,
        GeneratedRowShape rowShape,
        IReadOnlyList<CteSidecarIndexBuild> builds)
    {
        switch (node)
        {
            case ExecutionForEach forEach:
            {
                var body = TransformCteSidecarAppendBlock(forEach.Body, targetTable, rowShape, builds);
                var capacityHint = body.CapacityHint ??
                                   (body.AppendCount > 0 ? CreateRowsCapacityCandidate(targetTable, forEach.Source) : null);
                return new CteSidecarAppendNodeTransformResult(
                    forEach with { Body = body.Block },
                    body.AppendCount,
                    capacityHint);
            }
            case ExecutionForEachWithOrdinality forEach:
            {
                var body = TransformCteSidecarAppendBlock(forEach.Body, targetTable, rowShape, builds);
                var capacityHint = body.CapacityHint ??
                                   (body.AppendCount > 0 ? CreateRowsCapacityCandidate(targetTable, forEach.Source) : null);
                return new CteSidecarAppendNodeTransformResult(
                    forEach with { Body = body.Block },
                    body.AppendCount,
                    capacityHint);
            }
            case ExecutionForEachIndexed forEachIndexed:
            {
                var body = TransformCteSidecarAppendBlock(forEachIndexed.Body, targetTable, rowShape, builds);
                var capacityHint = body.CapacityHint ??
                                   (body.AppendCount > 0 ? CreateRowsCapacityCandidate(targetTable, new ExecutionRowStream(forEachIndexed.Source, ExecutionRowStreamKind.Rows)) : null);
                return new CteSidecarAppendNodeTransformResult(
                    forEachIndexed with { Body = body.Block },
                    body.AppendCount,
                    capacityHint);
            }
            case ExecutionIf branch:
            {
                var body = TransformCteSidecarAppendBlock(branch.Body, targetTable, rowShape, builds);
                return new CteSidecarAppendNodeTransformResult(
                    branch with { Body = body.Block },
                    body.AppendCount,
                    body.CapacityHint);
            }
            case ExecutionHashProbe hashProbe:
            {
                var body = TransformCteSidecarAppendBlock(hashProbe.Body, targetTable, rowShape, builds);
                var noMatchBlock = hashProbe.NoMatchBody;
                var noMatchAppendCount = 0;
                ExecutionCapacityHint? noMatchCapacityHint = null;
                if (hashProbe.NoMatchBody != null)
                {
                    var noMatch = TransformCteSidecarAppendBlock(hashProbe.NoMatchBody, targetTable, rowShape, builds);
                    noMatchBlock = noMatch.Block;
                    noMatchAppendCount = noMatch.AppendCount;
                    noMatchCapacityHint = noMatch.CapacityHint;
                }

                return new CteSidecarAppendNodeTransformResult(
                    hashProbe with { Body = body.Block, NoMatchBody = noMatchBlock },
                    body.AppendCount + noMatchAppendCount,
                    body.CapacityHint ?? noMatchCapacityHint);
            }
            case ExecutionKeySetProbe keySetProbe:
            {
                var body = TransformCteSidecarAppendBlock(keySetProbe.Body, targetTable, rowShape, builds);
                var noMatchBlock = keySetProbe.NoMatchBody;
                var noMatchAppendCount = 0;
                ExecutionCapacityHint? noMatchCapacityHint = null;
                if (keySetProbe.NoMatchBody != null)
                {
                    var noMatch = TransformCteSidecarAppendBlock(keySetProbe.NoMatchBody, targetTable, rowShape, builds);
                    noMatchBlock = noMatch.Block;
                    noMatchAppendCount = noMatch.AppendCount;
                    noMatchCapacityHint = noMatch.CapacityHint;
                }

                return new CteSidecarAppendNodeTransformResult(
                    keySetProbe with { Body = body.Block, NoMatchBody = noMatchBlock },
                    body.AppendCount + noMatchAppendCount,
                    body.CapacityHint ?? noMatchCapacityHint);
            }
            case ExecutionAsOfProbe asOfProbe:
            {
                var body = TransformCteSidecarAppendBlock(asOfProbe.Body, targetTable, rowShape, builds);
                var noMatchBlock = asOfProbe.NoMatchBody;
                var noMatchAppendCount = 0;
                ExecutionCapacityHint? noMatchCapacityHint = null;
                if (asOfProbe.NoMatchBody != null)
                {
                    var noMatch = TransformCteSidecarAppendBlock(asOfProbe.NoMatchBody, targetTable, rowShape, builds);
                    noMatchBlock = noMatch.Block;
                    noMatchAppendCount = noMatch.AppendCount;
                    noMatchCapacityHint = noMatch.CapacityHint;
                }

                return new CteSidecarAppendNodeTransformResult(
                    asOfProbe with { Body = body.Block, NoMatchBody = noMatchBlock },
                    body.AppendCount + noMatchAppendCount,
                    body.CapacityHint ?? noMatchCapacityHint);
            }
            case ExecutionRangeProbe rangeProbe:
            {
                var body = TransformCteSidecarAppendBlock(rangeProbe.Body, targetTable, rowShape, builds);
                return new CteSidecarAppendNodeTransformResult(
                    rangeProbe with { Body = body.Block },
                    body.AppendCount,
                    body.CapacityHint);
            }
            default:
                return new CteSidecarAppendNodeTransformResult(node, 0, null);
        }
    }

    private sealed record CteSidecarAppendTransformResult(
        ExecutionBlock Block,
        int AppendCount,
        ExecutionCapacityHint? CapacityHint);

    private sealed record CteSidecarAppendNodeTransformResult(
        ExecutionNode Node,
        int AppendCount,
        ExecutionCapacityHint? CapacityHint);
}
