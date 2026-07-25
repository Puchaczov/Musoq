using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record SourceEntityShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];

    public SourceEntityShape(
        string alias,
        ExecutionTypeRef entityType,
        IReadOnlyList<FieldBinding> fields)
        : base(alias, fields)
    {
        Alias = alias;
        EntityType = entityType;
        Fields = ExecutionIrCollections.Freeze(fields);
    }

    public string Alias { get; init; }

    public ExecutionTypeRef EntityType { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    internal SourceEntityShape(string alias, Type entityType, IReadOnlyList<FieldBinding> fields)
        : this(alias, ExecutionClrBindingFactory.FromClr(entityType), fields)
    {
    }
}
