using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record SourceEntityShape(
    string Alias,
    ExecutionTypeRef EntityType,
    IReadOnlyList<FieldBinding> Fields) : RowShape(Alias, Fields)
{
    internal SourceEntityShape(string alias, Type entityType, IReadOnlyList<FieldBinding> fields)
        : this(alias, ExecutionTypeRef.FromClr(entityType), fields)
    {
    }
}
