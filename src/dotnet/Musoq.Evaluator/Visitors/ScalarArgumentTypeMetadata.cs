using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Visitors;

/// <summary>Retains record kind during callable binding before concrete carriers are generated.</summary>
internal static class ScalarArgumentTypeMetadata
{
    internal static Type? Resolve(Node argument)
        => argument is RecordLiteralNode ? typeof(StructuralValue) : argument.ReturnType;
}
