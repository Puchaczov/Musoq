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

    /// <summary>
    /// Indicates that this carrier is the private row type selected for a
    /// query-scoped source. Such a carrier is always an internal execution
    /// boundary, even when the planner selected the sealed-class policy.
    /// </summary>
    public bool IsQueryScopedRow { get; init; }

    /// <summary>
    /// Alias used while a query-scoped carrier is still the source row shape.
    /// Ordinary generated result shapes intentionally leave this unset.
    /// </summary>
    public string? SourceAlias { get; init; }

    public GeneratedRowShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields)
        : this(typeName, fields, [])
    {
    }
}
