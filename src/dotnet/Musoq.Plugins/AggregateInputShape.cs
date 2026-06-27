using System.Collections.Generic;
using System.Linq;

namespace Musoq.Plugins;

/// <summary>
///     Describes the exact typed value shape passed to an aggregate kernel.
/// </summary>
/// <param name="InputType">The metadata input type used for planning.</param>
/// <param name="ArgumentTypes">The SQL/runtime argument types that formed the input value.</param>
public sealed record AggregateInputShape(
    Type InputType,
    IReadOnlyList<Type> ArgumentTypes)
{
    /// <summary>Creates a shape for aggregates with one input argument.</summary>
    public static AggregateInputShape Single(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);
        return new AggregateInputShape(inputType, [inputType]);
    }

    /// <summary>Creates a shape for aggregates with no value argument, such as COUNT(*).</summary>
    public static AggregateInputShape Unit()
    {
        return new AggregateInputShape(typeof(AggregateUnit), []);
    }

    /// <summary>Creates a shape for aggregates whose metadata input is a ValueTuple of multiple arguments.</summary>
    public static AggregateInputShape Tuple(IReadOnlyList<Type> argumentTypes)
    {
        ArgumentNullException.ThrowIfNull(argumentTypes);
        var arguments = argumentTypes.ToArray();

        return arguments.Length switch
        {
            0 => Unit(),
            1 => Single(arguments[0]),
            2 => CreateTuple(typeof(ValueTuple<,>), arguments),
            3 => CreateTuple(typeof(ValueTuple<,,>), arguments),
            4 => CreateTuple(typeof(ValueTuple<,,,>), arguments),
            5 => CreateTuple(typeof(ValueTuple<,,,,>), arguments),
            6 => CreateTuple(typeof(ValueTuple<,,,,,>), arguments),
            7 => CreateTuple(typeof(ValueTuple<,,,,,,>), arguments),
            _ => throw new NotSupportedException("Typed aggregate input shapes support up to seven input arguments.")
        };
    }

    private static AggregateInputShape CreateTuple(Type tupleTypeDefinition, Type[] argumentTypes)
    {
        return new AggregateInputShape(
            tupleTypeDefinition.MakeGenericType(argumentTypes),
            argumentTypes);
    }
}
