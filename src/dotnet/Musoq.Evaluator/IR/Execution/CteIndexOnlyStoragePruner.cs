using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal static class CteIndexOnlyStoragePruner
{
    public static ExecutionPlan Apply(
        ExecutionPlan plan,
        IReadOnlyList<ExecutionCteIndexOnlyStorageCandidate> candidates)
    {
        var body = plan.Body;
        var shapes = plan.Shapes;

        foreach (var candidate in candidates)
        {
            body = RemoveIndexOnlyCteRowStorage(
                body,
                candidate.TableName,
                candidate.RowTypeName,
                candidate.KeepPayloadRows);

            if (!candidate.KeepPayloadRows)
            {
                shapes = shapes
                    .Where(shape => shape is not GeneratedRowShape generated ||
                                    !string.Equals(generated.TypeName, candidate.RowTypeName, StringComparison.Ordinal))
                    .ToArray();
            }
        }

        return ReferenceEquals(body, plan.Body) && ReferenceEquals(shapes, plan.Shapes)
            ? plan
            : plan with { Body = body, Shapes = shapes };
    }

    private static ExecutionBlock RemoveIndexOnlyCteRowStorage(
        ExecutionBlock block,
        string tableName,
        string rowTypeName,
        bool keepPayloadRows)
    {
        var nodes = new List<ExecutionNode>(block.Nodes.Count);
        var changed = false;

        foreach (var node in block.Nodes)
        {
            var rewritten = RemoveIndexOnlyCteRowStorage(node, tableName, rowTypeName, keepPayloadRows);
            if (rewritten == null)
            {
                changed = true;
                continue;
            }

            changed |= !ReferenceEquals(rewritten, node);
            nodes.Add(rewritten);
        }

        return changed ? block with { Nodes = nodes } : block;
    }

    private static ExecutionNode? RemoveIndexOnlyCteRowStorage(
        ExecutionNode node,
        string tableName,
        string rowTypeName,
        bool keepPayloadRows)
    {
        switch (node)
        {
            case ExecutionCteIndexOnlyStorageCandidate candidate
                when string.Equals(candidate.TableName, tableName, StringComparison.Ordinal) &&
                     string.Equals(candidate.RowTypeName, rowTypeName, StringComparison.Ordinal):
                return null;
            case ExecutionCreateTable createTable
                when string.Equals(createTable.Table.Name, tableName, StringComparison.Ordinal) &&
                     string.Equals(createTable.RowShape.TypeName, rowTypeName, StringComparison.Ordinal):
                return null;
            case ExecutionEnsureTableCapacity ensureCapacity
                when string.Equals(ensureCapacity.Table.Name, tableName, StringComparison.Ordinal):
                return null;
            case ExecutionAppendExistingRow appendRow
                when string.Equals(appendRow.Table.Name, tableName, StringComparison.Ordinal) &&
                     string.Equals(appendRow.Row.GeneratedRowTypeName, rowTypeName, StringComparison.Ordinal):
                return null;
            case ExecutionAppendRow appendRow
                when string.Equals(appendRow.Table.Name, tableName, StringComparison.Ordinal) &&
                     string.Equals(appendRow.RowShape.TypeName, rowTypeName, StringComparison.Ordinal):
                return null;
            case ExecutionCreateGeneratedRow createRow
                when !keepPayloadRows &&
                     string.Equals(createRow.RowShape.TypeName, rowTypeName, StringComparison.Ordinal):
                return null;
            case ExecutionForEach loop:
                return loop with
                {
                    Body = RemoveIndexOnlyCteRowStorage(loop.Body, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionForEachWithOrdinality loop:
                return loop with
                {
                    Body = RemoveIndexOnlyCteRowStorage(loop.Body, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionForEachIndexed loop:
                return loop with
                {
                    Body = RemoveIndexOnlyCteRowStorage(loop.Body, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionParallelBlock parallel:
                return parallel with
                {
                    Tasks = parallel.Tasks
                        .Select(task => task with
                        {
                            Body = RemoveIndexOnlyCteRowStorage(task.Body, tableName, rowTypeName, keepPayloadRows)
                        })
                        .ToArray(),
                    Merge = parallel.Merge with
                    {
                        Body = RemoveIndexOnlyCteRowStorage(parallel.Merge.Body, tableName, rowTypeName, keepPayloadRows)
                    }
                };
            case ExecutionParallelFilterProjectLoop parallelProject:
                return parallelProject with
                {
                    ProjectionBody = RemoveIndexOnlyCteRowStorage(parallelProject.ProjectionBody, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                return parallelAggregate with
                {
                    AggregateBody = RemoveIndexOnlyCteRowStorage(parallelAggregate.AggregateBody, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionIf branch:
                return branch with
                {
                    Body = RemoveIndexOnlyCteRowStorage(branch.Body, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionHashProbe probe:
                return probe with
                {
                    Body = RemoveIndexOnlyCteRowStorage(probe.Body, tableName, rowTypeName, keepPayloadRows),
                    NoMatchBody = probe.NoMatchBody == null
                        ? null
                        : RemoveIndexOnlyCteRowStorage(probe.NoMatchBody, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionKeySetProbe probe:
                return probe with
                {
                    Body = RemoveIndexOnlyCteRowStorage(probe.Body, tableName, rowTypeName, keepPayloadRows),
                    NoMatchBody = probe.NoMatchBody == null
                        ? null
                        : RemoveIndexOnlyCteRowStorage(probe.NoMatchBody, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionAsOfProbe probe:
                return probe with
                {
                    Body = RemoveIndexOnlyCteRowStorage(probe.Body, tableName, rowTypeName, keepPayloadRows),
                    NoMatchBody = probe.NoMatchBody == null
                        ? null
                        : RemoveIndexOnlyCteRowStorage(probe.NoMatchBody, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionRangeProbe probe:
                return probe with
                {
                    Body = RemoveIndexOnlyCteRowStorage(probe.Body, tableName, rowTypeName, keepPayloadRows)
                };
            case ExecutionFusedCteProducer producer:
                return producer with
                {
                    Body = RemoveIndexOnlyCteRowStorage(producer.Body, tableName, rowTypeName, keepPayloadRows)
                };
            default:
                return node;
        }
    }
}
