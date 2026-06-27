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

        var catalog = Create(ExecutionPlanPrinter.Print(plan));
        var operatorsByNode = CreateOperatorNodeMap(plan, catalog.Operators);

        return new ExecutionPlanOperatorCatalog(
            catalog.Operators,
            catalog.AnnotatedExecutionPlanText,
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

    private static IReadOnlyDictionary<ExecutionNode, ExecutionPlanOperatorDescriptor> CreateOperatorNodeMap(
        ExecutionPlan plan,
        IReadOnlyList<ExecutionPlanOperatorDescriptor> operators)
    {
        var result = new Dictionary<ExecutionNode, ExecutionPlanOperatorDescriptor>(
            ExecutionNodeReferenceComparer.Instance);
        var operatorIndex = FindBodyStartIndex(operators);

        foreach (var node in EnumeratePrinterOrder(plan.Body))
        {
            while (operatorIndex < operators.Count && IsPlanLabel(operators[operatorIndex]))
                operatorIndex++;

            if (operatorIndex >= operators.Count)
                break;

            result[node] = operators[operatorIndex];
            operatorIndex++;
        }

        return result;
    }

    private static int FindBodyStartIndex(IReadOnlyList<ExecutionPlanOperatorDescriptor> operators)
    {
        for (var index = 0; index < operators.Count; index++)
        {
            if (operators[index].NodeKind.Equals("Body", StringComparison.Ordinal))
                return index + 1;
        }

        return 0;
    }

    private static bool IsPlanLabel(ExecutionPlanOperatorDescriptor descriptor)
    {
        return descriptor.NodeKind is
            "ParallelTask" or
            "ParallelMerge" or
            "ParallelProject" or
            "SerialFallback" or
            "ParallelAccumulate" or
            "HashProbeNoMatch" or
            "KeySetProbeNoMatch" or
            "AsOfProbeNoMatch";
    }

    private static IEnumerable<ExecutionNode> EnumeratePrinterOrder(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            yield return node;

            foreach (var child in EnumeratePrinterOrder(node))
                yield return child;
        }
    }

    private static IEnumerable<ExecutionNode> EnumeratePrinterOrder(ExecutionNode node)
    {
        switch (node)
        {
            case ExecutionForEach forEach:
                return EnumeratePrinterOrder(forEach.Body);
            case ExecutionForEachWithOrdinality forEach:
                return EnumeratePrinterOrder(forEach.Body);
            case ExecutionForEachIndexed forEachIndexed:
                return EnumeratePrinterOrder(forEachIndexed.Body);
            case ExecutionParallelBlock parallel:
                return EnumerateParallelBlock(parallel);
            case ExecutionIf branch:
                return EnumeratePrinterOrder(branch.Body);
            case ExecutionHashProbe hashProbe:
                return EnumerateProbeBlocks(hashProbe.Body, hashProbe.NoMatchBody);
            case ExecutionKeySetProbe keySetProbe:
                return EnumerateProbeBlocks(keySetProbe.Body, keySetProbe.NoMatchBody);
            case ExecutionAsOfProbe asOfProbe:
                return EnumerateProbeBlocks(asOfProbe.Body, asOfProbe.NoMatchBody);
            case ExecutionRangeProbe rangeProbe:
                return EnumeratePrinterOrder(rangeProbe.Body);
            case ExecutionWindowKernelPlan plan:
                return plan.Kernels;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                return EnumerateParallelAggregateLoop(parallelAggregate);
            case ExecutionParallelFilterProjectLoop parallelProject:
                return EnumerateParallelFilterProjectLoop(parallelProject);
            case ExecutionFusedCteProducer fusedCte:
                return EnumeratePrinterOrder(fusedCte.Body);
            case ExecutionSingleUsePipelineFusionCandidate candidate:
                return EnumeratePrinterOrder(candidate.Body);
            case ExecutionCteReadOnceFusionCandidate candidate:
                return EnumeratePrinterOrder(candidate.Body);
            case ExecutionCteFusedProducerCandidate candidate:
                return EnumeratePrinterOrder(candidate.Body);
            default:
                return Array.Empty<ExecutionNode>();
        }
    }

    private static IEnumerable<ExecutionNode> EnumerateParallelBlock(ExecutionParallelBlock parallel)
    {
        foreach (var task in parallel.Tasks)
        {
            foreach (var node in EnumeratePrinterOrder(task.Body))
                yield return node;
        }

        foreach (var node in EnumeratePrinterOrder(parallel.Merge.Body))
            yield return node;
    }

    private static IEnumerable<ExecutionNode> EnumerateProbeBlocks(
        ExecutionBlock body,
        ExecutionBlock? noMatchBody)
    {
        foreach (var node in EnumeratePrinterOrder(body))
            yield return node;

        if (noMatchBody == null)
            yield break;

        foreach (var node in EnumeratePrinterOrder(noMatchBody))
            yield return node;
    }

    private static IEnumerable<ExecutionNode> EnumerateParallelAggregateLoop(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        foreach (var node in EnumeratePrinterOrder(parallelAggregate.AggregateBody))
            yield return node;

        foreach (var node in EnumeratePrinterOrder(parallelAggregate.SerialLoop.Body))
            yield return node;
    }

    private static IEnumerable<ExecutionNode> EnumerateParallelFilterProjectLoop(
        ExecutionParallelFilterProjectLoop parallelProject)
    {
        yield return parallelProject.AppendRow;

        foreach (var node in EnumeratePrinterOrder(parallelProject.SerialLoop.Body))
            yield return node;
    }

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
