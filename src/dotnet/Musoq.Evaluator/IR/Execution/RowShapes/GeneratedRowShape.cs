using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRowShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];
    private IReadOnlyList<FieldBinding> _contexts = [];

    public GeneratedRowShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<FieldBinding> contexts,
        bool supportsGeneratedFieldAccess = true,
        bool requiresRowBase = true)
        : base(typeName, fields)
    {
        TypeName = typeName;
        Fields = ExecutionIrCollections.Freeze(fields);
        Contexts = ExecutionIrCollections.Freeze(contexts);
        SupportsGeneratedFieldAccess = supportsGeneratedFieldAccess;
        RequiresRowBase = requiresRowBase;
    }

    public string TypeName { get; init; }

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

    public bool SupportsGeneratedFieldAccess { get; init; }

    public bool RequiresRowBase { get; init; }

    public bool EmitAsValueType { get; init; }

    public GeneratedRowShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields)
        : this(typeName, fields, [])
    {
    }
}
