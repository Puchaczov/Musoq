using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record SourceEntityShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];

    public SourceEntityShape(
        string alias,
        ExecutionTypeRef entityType,
        IReadOnlyList<FieldBinding> fields,
        string? generatedTypeName = null)
        : base(alias, fields)
    {
        Alias = alias;
        EntityType = entityType;
        GeneratedTypeName = generatedTypeName;
        Fields = ExecutionIrCollections.Freeze(fields);
    }

    public string Alias { get; init; }

    public ExecutionTypeRef EntityType { get; init; }

    public string? GeneratedTypeName { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    internal SourceEntityShape(string alias, Type entityType, IReadOnlyList<FieldBinding> fields, string? generatedTypeName = null)
        : this(alias, ExecutionClrBindingFactory.FromClr(entityType), fields, generatedTypeName)
    {
    }
}
