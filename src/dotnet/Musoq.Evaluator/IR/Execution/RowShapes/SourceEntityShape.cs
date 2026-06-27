using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record SourceEntityShape(
    string Alias,
    Type EntityType,
    IReadOnlyList<FieldBinding> Fields) : RowShape(Alias, Fields);
