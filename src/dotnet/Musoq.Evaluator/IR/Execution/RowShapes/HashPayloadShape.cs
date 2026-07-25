using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record HashPayloadShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];
    private IReadOnlyList<FieldBinding> _contexts = [];

    public HashPayloadShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        IReadOnlyList<FieldBinding> contexts)
        : base(typeName, fields)
    {
        TypeName = typeName;
        Fields = ExecutionIrCollections.Freeze(fields);
        Contexts = ExecutionIrCollections.Freeze(contexts);
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

    public HashPayloadShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields)
        : this(typeName, fields, [])
    {
    }
}
