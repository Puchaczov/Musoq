using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public enum PhysicalRecursiveCteInvariantStorageKind
{
    Snapshot,
    HashIndex,
    ExistingRows,
    ExistingHashIndex
}

public sealed record PhysicalRecursiveCteInvariantDefinition(
    string Name,
    PhysicalNode Plan,
    string Alias,
    string[] SourceAliases,
    ProjectedField[] Fields,
    PhysicalRecursiveCteInvariantStorageKind StorageKind,
    IrExpression[] HashKeys,
    IrExpression[] HashProbeKeys)
{
    public OutputSchema OutputSchema { get; } = OutputSchemaFactory.ForProjection(Fields);

    public string? ExistingCteName { get; init; }
}
