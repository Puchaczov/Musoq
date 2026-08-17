using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public abstract record RowShape
{
    private IReadOnlyList<FieldBinding> _fields = [];

    protected RowShape(string name, IReadOnlyList<FieldBinding> fields)
    {
        Name = name;
        _fields = ExecutionIrCollections.Freeze(fields);
    }

    public string Name { get; init; }

    public virtual IReadOnlyList<FieldBinding> Fields
    {
        get => _fields;
        init => _fields = ExecutionIrCollections.Freeze(value);
    }
}
