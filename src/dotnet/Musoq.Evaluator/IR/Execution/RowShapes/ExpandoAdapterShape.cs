using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExpandoAdapterShape(
    string Alias,
    string TypeName,
    ExecutionTypeRef RuntimeType,
    IReadOnlyList<FieldBinding> Fields) : RowShape(TypeName, Fields)
{
    internal ExpandoAdapterShape(
        string alias,
        string typeName,
        Type runtimeType,
        IReadOnlyList<FieldBinding> fields)
        : this(alias, typeName, ExecutionTypeRef.FromClr(runtimeType), fields)
    {
    }
}
