using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record TableRowShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];
    private IReadOnlyList<FieldBinding> _contexts = [];

    public TableRowShape(
        string alias,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<FieldBinding> contexts)
        : base(alias, fields)
    {
        Alias = alias;
        Fields = ExecutionIrCollections.Freeze(fields);
        Contexts = ExecutionIrCollections.Freeze(contexts);
    }

    public string Alias { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    public IReadOnlyList<FieldBinding> Contexts
    {
        get => _contexts;
        init => _contexts = ExecutionIrCollections.Freeze(value);
    }

    public TableRowShape(
        string alias,
        IReadOnlyList<FieldBinding> fields)
        : this(alias, fields, [])
    {
    }
}
