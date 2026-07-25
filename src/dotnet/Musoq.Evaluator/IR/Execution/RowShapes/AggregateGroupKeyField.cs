using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

public sealed record AggregateGroupKeyField(
    string Name,
    string FieldName,
    ExecutionTypeRef Type)
{
    internal AggregateGroupKeyField(string name, string fieldName, Type type)
        : this(name, fieldName, ExecutionClrBindingFactory.FromClr(type))
    {
    }
}
