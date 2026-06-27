using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record HashPayloadShape(
    string TypeName,
    IReadOnlyList<FieldBinding> Fields,
    IReadOnlyList<FieldBinding> Contexts) : RowShape(TypeName, Fields)
{
    public HashPayloadShape(
        string typeName,
        IReadOnlyList<FieldBinding> fields)
        : this(typeName, fields, [])
    {
    }
}
