using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed class IntermediateSourceRegistry
{
    private readonly Dictionary<QueryNode, IntermediateSourceBinding> _intermediateSources = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> _intermediateStorageNames = new(StringComparer.OrdinalIgnoreCase);
    private int _nextIntermediateProducer;
    private int _nextIntermediateName;

    internal void ReserveNames(RootNode root)
    {
        var pending = new Stack<Node>();
        pending.Push(root);
        while (pending.TryPop(out var node))
        {
            if (node is BinarySchemaNode or TextSchemaNode)
                continue;
            if (node is CteInnerExpressionNode cte)
                _intermediateStorageNames.Add(cte.Name);
            foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
                pending.Push(child);
        }
    }

    internal void Register(QueryNode query, string displayName)
    {
        var storageName = displayName;
        while (!_intermediateStorageNames.Add(storageName))
            storageName = "__musoqIntermediate" + (_nextIntermediateName++).ToString(CultureInfo.InvariantCulture);
        _intermediateSources.Add(query, new IntermediateSourceBinding(
            new IntermediateProducerId(_nextIntermediateProducer++), storageName));
    }

    internal IntermediateSourceBinding Get(QueryNode query) => _intermediateSources[query];

    internal MultiStatementNode CreateStatements(Node[] nodes, Type? returnType)
    {
        var producers = ImmutableArray.CreateBuilder<IntermediateSourceProducer>();
        var index = 0;
        foreach (var node in nodes)
        {
            if (node is not QueryNode query)
                continue;
            if (_intermediateSources.TryGetValue(query, out var source))
                producers.Add(new IntermediateSourceProducer(source, index));
            index++;
        }
        if (producers.Count == 0)
            return new MultiStatementNode(nodes, returnType);

        return new Parser.IntermediateStatementsNode(nodes, returnType, producers.ToImmutable());
    }
}
