using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

public sealed class ExecutionPlanOperatorCatalog
{
    private readonly IReadOnlyList<ExecutionPlanOperatorDescriptor> _operators;
    private readonly IReadOnlySet<string> _nodeOperatorIds;
    private readonly string _annotatedExecutionPlanText;
    private readonly IReadOnlyDictionary<ExecutionNode, ExecutionPlanOperatorDescriptor> _operatorsByNode;

    private ExecutionPlanOperatorCatalog(
        IReadOnlyList<ExecutionPlanOperatorDescriptor> operators,
        string annotatedExecutionPlanText,
        IReadOnlyDictionary<ExecutionNode, ExecutionPlanOperatorDescriptor>? operatorsByNode = null)
    {
        _operators = operators;
        _annotatedExecutionPlanText = annotatedExecutionPlanText;
        _operatorsByNode = operatorsByNode ??
                           new Dictionary<ExecutionNode, ExecutionPlanOperatorDescriptor>(
                               ExecutionNodeReferenceComparer.Instance);
        _nodeOperatorIds = _operatorsByNode.Values
            .Select(static descriptor => descriptor.Id)
            .ToHashSet(StringComparer.Ordinal);
    }

    public IReadOnlyList<ExecutionPlanOperatorDescriptor> Operators => _operators;

    public string AnnotatedExecutionPlanText => _annotatedExecutionPlanText;

    internal IEnumerable<ExecutionPlanOperatorDescriptor> NodeOperators =>
        _operators.Where(descriptor => _nodeOperatorIds.Contains(descriptor.Id));

    internal bool TryGetDescriptor(
        ExecutionNode node,
        out ExecutionPlanOperatorDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _operatorsByNode.TryGetValue(node, out descriptor!);
    }

    public static ExecutionPlanOperatorCatalog Create(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var operators = new List<ExecutionPlanOperatorDescriptor>();
        var operatorsByNode = new Dictionary<ExecutionNode, ExecutionPlanOperatorDescriptor>(
            ExecutionNodeReferenceComparer.Instance);
        var annotatedPlan = new StringBuilder();
        var nodeDescriptions = ExecutionPlanPrinter.CaptureNodeDescriptions(plan);
        var operatorIndex = 1;

        foreach (var entry in EnumerateStructuredEntries(plan, nodeDescriptions))
        {
            var descriptor = new ExecutionPlanOperatorDescriptor(
                $"op{operatorIndex.ToString(CultureInfo.InvariantCulture)}",
                entry.DisplayName,
                entry.NodeKind,
                ResolveRowCountStrategy(entry.NodeKind))
            {
                OperationId = entry.OperationId
            };

            operators.Add(descriptor);
            if (entry.Node is { } node)
                operatorsByNode.Add(node, descriptor);

            annotatedPlan
                .Append(entry.IsRoot ? string.Empty : "    ")
                .Append('[')
                .Append(descriptor.Id)
                .Append("] ")
                .AppendLine(descriptor.DisplayName);
            operatorIndex++;
        }

        return new ExecutionPlanOperatorCatalog(
            operators,
            annotatedPlan.ToString().TrimEnd(),
            operatorsByNode);
    }

    public static ExecutionPlanOperatorCatalog Create(string executionPlanText)
    {
        if (string.IsNullOrWhiteSpace(executionPlanText))
            return new ExecutionPlanOperatorCatalog(Array.Empty<ExecutionPlanOperatorDescriptor>(), string.Empty);

        var operators = new List<ExecutionPlanOperatorDescriptor>();
        var builder = new StringBuilder();
        using var reader = new StringReader(executionPlanText);
        var operatorIndex = 1;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine();
                continue;
            }

            var id = $"op{operatorIndex.ToString(CultureInfo.InvariantCulture)}";
            var indentLength = line.Length - line.TrimStart().Length;
            var indent = line[..indentLength];
            var content = line[indentLength..];
            var nodeKind = ExtractNodeKind(content);

            operators.Add(new ExecutionPlanOperatorDescriptor(
                id,
                content,
                nodeKind,
                ResolveRowCountStrategy(nodeKind)));

            builder
                .Append(indent)
                .Append('[')
                .Append(id)
                .Append("] ")
                .AppendLine(content);

            operatorIndex++;
        }

        return new ExecutionPlanOperatorCatalog(operators, builder.ToString().TrimEnd());
    }

    private static IEnumerable<StructuredOperatorEntry> EnumerateStructuredEntries(
        ExecutionPlan plan,
        IReadOnlyDictionary<ExecutionNode, ExecutionNodePrintDescription> descriptions)
    {
        yield return new StructuredOperatorEntry(
            null,
            $"ExecutionPlan [{plan.Identifier}]",
            "ExecutionPlan",
            IsRoot: true);
        yield return new StructuredOperatorEntry(null, "Shapes", "Shapes");

        foreach (var shape in plan.Shapes)
        {
            var shapeKind = shape switch
            {
                SourceEntityShape => "SourceEntity",
                GeneratedRowShape => "Generated",
                GeneratedRecordShape => "GeneratedRecord",
                HashPayloadShape => "HashPayload",
                AggregateGroupShape => "AggregateGroup",
                TableRowShape => "TableRow",
                ExpandoAdapterShape => "ExpandoAdapter",
                _ => "UnknownShape"
            };
            yield return new StructuredOperatorEntry(null, shapeKind, shapeKind);

            foreach (var field in shape.Fields)
                yield return new StructuredOperatorEntry(null, field.Name, field.Name);
        }

        yield return new StructuredOperatorEntry(null, "Body", "Body");

        foreach (var entry in EnumerateStructuredBlockEntries(plan.Body, descriptions))
            yield return entry;
    }

    private static IEnumerable<StructuredOperatorEntry> EnumerateStructuredBlockEntries(
        ExecutionBlock block,
        IReadOnlyDictionary<ExecutionNode, ExecutionNodePrintDescription> descriptions)
    {
        foreach (var node in block.Nodes)
        {
            var description = descriptions.TryGetValue(node, out var captured)
                ? captured
                : new ExecutionNodePrintDescription(node.GetType().Name, GetFallbackNodeKind(node));
            yield return new StructuredOperatorEntry(
                node,
                description.DisplayName,
                description.NodeKind,
                ExecutionOperationCatalog.Resolve(node).Value);

            foreach (var entry in EnumerateStructuredNodeEntries(node, descriptions))
                yield return entry;
        }
    }

    private static IEnumerable<StructuredOperatorEntry> EnumerateStructuredNodeEntries(
        ExecutionNode node,
        IReadOnlyDictionary<ExecutionNode, ExecutionNodePrintDescription> descriptions)
    {
        if (!ExecutionNodeRegistry.TryGetDescriptor(node, out var descriptor))
            yield break;

        if (node is ExecutionRecursiveCteAppend)
            yield break;

        switch (node)
        {
            case ExecutionParallelBlock parallel:
                foreach (var task in parallel.Tasks)
                {
                    yield return Label($"ParallelTask [{task.Name} -> {task.Output.Name}]");
                    foreach (var entry in EnumerateStructuredBlockEntries(task.Body, descriptions))
                        yield return entry;
                }

                yield return Label("ParallelMerge");
                foreach (var entry in EnumerateStructuredBlockEntries(parallel.Merge.Body, descriptions))
                    yield return entry;
                yield break;
            case ExecutionRecursiveCte recursiveCte:
                yield return Label("Anchor");
                foreach (var entry in EnumerateStructuredBlockEntries(recursiveCte.Anchor, descriptions))
                    yield return entry;

                if (recursiveCte.InvariantSetup.Nodes.Count > 0)
                {
                    yield return Label("InvariantSetup");
                    foreach (var entry in EnumerateStructuredBlockEntries(recursiveCte.InvariantSetup, descriptions))
                        yield return entry;
                }

                yield return Label("RecursiveMember");
                foreach (var entry in EnumerateStructuredBlockEntries(recursiveCte.RecursiveMember, descriptions))
                    yield return entry;
                yield break;
            case ExecutionHashProbe hashProbe:
                foreach (var entry in EnumerateStructuredBlockEntries(hashProbe.Body, descriptions))
                    yield return entry;
                if (hashProbe.NoMatchBody is { Nodes.Count: > 0 } noMatchBody)
                {
                    yield return Label("HashProbeNoMatch");
                    foreach (var entry in EnumerateStructuredBlockEntries(noMatchBody, descriptions))
                        yield return entry;
                }
                yield break;
            case ExecutionKeySetProbe keySetProbe:
                foreach (var entry in EnumerateStructuredBlockEntries(keySetProbe.Body, descriptions))
                    yield return entry;
                if (keySetProbe.NoMatchBody is { Nodes.Count: > 0 } keySetNoMatchBody)
                {
                    yield return Label("KeySetProbeNoMatch");
                    foreach (var entry in EnumerateStructuredBlockEntries(keySetNoMatchBody, descriptions))
                        yield return entry;
                }
                yield break;
            case ExecutionAsOfProbe asOfProbe:
                foreach (var entry in EnumerateStructuredBlockEntries(asOfProbe.Body, descriptions))
                    yield return entry;
                if (asOfProbe.NoMatchBody is { Nodes.Count: > 0 } asOfNoMatchBody)
                {
                    yield return Label("AsOfProbeNoMatch");
                    foreach (var entry in EnumerateStructuredBlockEntries(asOfNoMatchBody, descriptions))
                        yield return entry;
                }
                yield break;
            case ExecutionRangeProbe rangeProbe:
                foreach (var entry in EnumerateStructuredBlockEntries(rangeProbe.Body, descriptions))
                    yield return entry;
                if (rangeProbe.NoMatchBody is { Nodes.Count: > 0 } rangeNoMatchBody)
                {
                    yield return Label("RangeProbeNoMatch");
                    foreach (var entry in EnumerateStructuredBlockEntries(rangeNoMatchBody, descriptions))
                        yield return entry;
                }
                yield break;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                yield return Label("ParallelAccumulate");
                foreach (var entry in EnumerateStructuredBlockEntries(parallelAggregate.AggregateBody, descriptions))
                    yield return entry;
                yield break;
            case ExecutionParallelFilterProjectLoop parallelProject:
                yield return Label("ParallelProject");
                foreach (var entry in EnumerateStructuredBlockEntries(parallelProject.ProjectionBody, descriptions))
                    yield return entry;
                yield break;
            default:
                foreach (var childBlock in descriptor.GetChildBlocks(node))
                {
                    foreach (var entry in EnumerateStructuredBlockEntries(childBlock, descriptions))
                        yield return entry;
                }
                yield break;
        }
    }

    private static StructuredOperatorEntry Label(string label) =>
        new(null, label, label);

    private static string GetFallbackNodeKind(ExecutionNode node)
    {
        const string prefix = "Execution";
        var name = node.GetType().Name;
        return name.StartsWith(prefix, StringComparison.Ordinal)
            ? name[prefix.Length..]
            : name;
    }

    private sealed record StructuredOperatorEntry(
        ExecutionNode? Node,
        string DisplayName,
        string NodeKind,
        string? OperationId = null,
        bool IsRoot = false);

    private static string ExtractNodeKind(string content)
    {
        var end = content.IndexOfAny([' ', '[']);
        return end < 0 ? content : content[..end];
    }

    private static ExecutionPlanOperatorRowCountStrategy ResolveRowCountStrategy(string nodeKind)
    {
        if (nodeKind.Contains("Source", StringComparison.OrdinalIgnoreCase))
            return ExecutionPlanOperatorRowCountStrategy.SourceBoundary;

        if (nodeKind.Contains("Append", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Return", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Values", StringComparison.OrdinalIgnoreCase))
            return ExecutionPlanOperatorRowCountStrategy.RowProducer;

        if (nodeKind.Contains("Table", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Materialize", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Sort", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Top", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Skip", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Take", StringComparison.OrdinalIgnoreCase) ||
            nodeKind.Contains("Distinct", StringComparison.OrdinalIgnoreCase))
            return ExecutionPlanOperatorRowCountStrategy.TableTransform;

        return ExecutionPlanOperatorRowCountStrategy.Unknown;
    }

    private sealed class ExecutionNodeReferenceComparer : IEqualityComparer<ExecutionNode>
    {
        public static readonly ExecutionNodeReferenceComparer Instance = new();

        public bool Equals(ExecutionNode? x, ExecutionNode? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(ExecutionNode obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
