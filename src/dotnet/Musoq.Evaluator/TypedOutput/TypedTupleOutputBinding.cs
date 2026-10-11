using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.TypedOutput;

internal static class TypedTupleOutputBinding
{
    public static TypedOutputBindingPlan? Create(Type outputType, IReadOnlyList<TypedOutputColumn> columns)
    {
        if (!typeof(ITuple).IsAssignableFrom(outputType) || outputType.Namespace != "System")
            return null;

        var constructor = outputType.GetConstructors().SingleOrDefault(candidate =>
            candidate.GetParameters().Length == columns.Count);
        if (constructor is null)
            throw new InvalidOperationException("Tuple result arity must match the selected column count.");

        var parameters = constructor.GetParameters();
        var bindings = new TypedOutputConstructorBinding[columns.Count];
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var target = parameters[index].ParameterType;
            if (!TypedOutputBinder.CanAssign(column.Type, target))
                throw new InvalidOperationException(
                    $"Tuple item {index + 1} expects '{target}', but column '{column.Name}' has type '{column.Type}'.");
            bindings[index] = new TypedOutputConstructorBinding(column, target);
        }

        return new TypedOutputBindingPlan(outputType, columns, constructor, bindings, []);
    }
}
