using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record GeneratedRowShape(
    string TypeName,
    IReadOnlyList<FieldBinding> Fields,
    IReadOnlyList<FieldBinding> Contexts,
    bool SupportsGeneratedFieldAccess = true,
    bool RequiresRowBase = true) : RowShape(TypeName, Fields)
{
    public GeneratedRowShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields)
        : this(typeName, fields, [])
    {
    }
}
