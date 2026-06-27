using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Physical.Nodes;

public sealed record PhysicalUnpivotNode(
    string Alias,
    string NameColumn,
    string ValueColumn,
    IReadOnlyList<UnpivotEntry> Entries,
    IReadOnlyList<ProjectedField> KeepFields,
    PhysicalNode Source,
    OutputSchema OutputSchema) : PhysicalNode(OutputSchema)
{
    public override IReadOnlyList<PhysicalNode> Children { get; } = [Source];
}
