using System.Collections.Immutable;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Parser;

internal sealed class IntermediateStatementsNode(Node[] nodes, Type? returnType,
    ImmutableArray<IntermediateSourceProducer> producers) : MultiStatementNode(nodes, returnType)
{
    internal ImmutableArray<IntermediateSourceProducer> Producers { get; } = producers;
}
