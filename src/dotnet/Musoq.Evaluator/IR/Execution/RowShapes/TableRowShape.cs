using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record TableRowShape(
    string Alias,
    IReadOnlyList<FieldBinding> Fields,
    IReadOnlyList<FieldBinding> Contexts) : RowShape(Alias, Fields)
{
    public TableRowShape(
        string alias,
        IReadOnlyList<FieldBinding> fields)
        : this(alias, fields, [])
    {
    }
}
