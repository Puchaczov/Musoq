using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRecordShape : RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];

    public GeneratedRecordShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields,
        bool emitAsValueType = false)
        : base(typeName, fields)
    {
        TypeName = typeName;
        Fields = ExecutionIrCollections.Freeze(fields);
        EmitAsValueType = emitAsValueType;
    }

    public string TypeName { get; init; }

    public override IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }

    public bool EmitAsValueType { get; init; }
}
