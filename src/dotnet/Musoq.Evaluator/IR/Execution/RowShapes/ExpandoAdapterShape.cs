using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExpandoAdapterShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];

    public ExpandoAdapterShape(
        string alias,
        string typeName,
        ExecutionTypeRef runtimeType,
        IReadOnlyList<FieldBinding> fields)
        : base(typeName, fields)
    {
        Alias = alias;
        TypeName = typeName;
        RuntimeType = runtimeType;
        Fields = ExecutionIrCollections.Freeze(fields);
    }

    public string Alias { get; init; }

    public string TypeName { get; init; }

    public ExecutionTypeRef RuntimeType { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    internal ExpandoAdapterShape(
        string alias,
        string typeName,
        Type runtimeType,
        IReadOnlyList<FieldBinding> fields)
        : this(alias, typeName, ExecutionClrBindingFactory.FromClr(runtimeType), fields)
    {
    }
}
