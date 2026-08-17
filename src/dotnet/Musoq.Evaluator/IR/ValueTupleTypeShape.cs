using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR;

internal static class ValueTupleTypeShape
{
    private const int TupleElementLimit = 7;

    public static bool TryCreate(IReadOnlyList<Type> elementTypes, out Type tupleType)
    {
        if (elementTypes.Count < 2 || elementTypes.Any(static type => type == null))
        {
            tupleType = null!;
            return false;
        }

        tupleType = Create(elementTypes, 0);
        return true;
    }

    public static bool TryGetElementTypes(Type type, out Type[] elementTypes)
    {
        var flattened = new List<Type>();
        if (!TryFlatten(type, flattened) || flattened.Count < 2)
        {
            elementTypes = [];
            return false;
        }

        elementTypes = flattened.ToArray();
        return true;
    }

    public static bool IsValueTuple(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition().Namespace == typeof(ValueTuple).Namespace &&
               type.GetGenericTypeDefinition().Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
    }

    private static Type Create(IReadOnlyList<Type> elementTypes, int offset)
    {
        var remaining = elementTypes.Count - offset;
        if (remaining <= TupleElementLimit)
            return MakeTupleType(elementTypes.Skip(offset).ToArray());

        var head = elementTypes.Skip(offset).Take(TupleElementLimit).ToList();
        head.Add(Create(elementTypes, offset + TupleElementLimit));
        return typeof(ValueTuple<,,,,,,,>).MakeGenericType(head.ToArray());
    }

    private static bool TryFlatten(Type type, ICollection<Type> destination)
    {
        if (!IsValueTuple(type))
            return false;

        var arguments = type.GetGenericArguments();
        if (arguments.Length == 8)
        {
            for (var index = 0; index < TupleElementLimit; index++)
                destination.Add(arguments[index]);

            return TryFlatten(arguments[^1], destination);
        }

        foreach (var argument in arguments)
            destination.Add(argument);

        return true;
    }

    private static Type MakeTupleType(Type[] elementTypes)
    {
        var tupleDefinition = elementTypes.Length switch
        {
            1 => typeof(ValueTuple<>),
            2 => typeof(ValueTuple<,>),
            3 => typeof(ValueTuple<,,>),
            4 => typeof(ValueTuple<,,,>),
            5 => typeof(ValueTuple<,,,,>),
            6 => typeof(ValueTuple<,,,,,>),
            7 => typeof(ValueTuple<,,,,,,>),
            _ => throw new ArgumentOutOfRangeException(nameof(elementTypes))
        };

        return tupleDefinition.MakeGenericType(elementTypes);
    }
}
