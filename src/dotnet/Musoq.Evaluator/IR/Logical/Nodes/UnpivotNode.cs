using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical.Nodes;

public sealed record UnpivotNode(
    string Alias,
    string NameColumn,
    string ValueColumn,
    IReadOnlyList<UnpivotEntry> Entries,
    IReadOnlyList<ProjectedField> KeepFields,
    LogicalNode Source,
    OutputSchema OutputSchema) : LogicalNode(OutputSchema)
{
    public override IReadOnlyList<LogicalNode> Children { get; } = [Source];
}
