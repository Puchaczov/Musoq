using System.Collections.Frozen;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private static readonly FrozenDictionary<Type, FrozenSet<Type>> TypeCompatibilityTable = new Dictionary<Type, Type[]>
    {
        { typeof(bool), [typeof(bool)] },
        { typeof(short), [typeof(short)] },
        { typeof(int), [typeof(int), typeof(short)] },
        { typeof(long), [typeof(long), typeof(int), typeof(short)] },
        { typeof(DateTimeOffset), [typeof(DateTimeOffset)] },
        { typeof(DateTime), [typeof(DateTime)] },
        { typeof(string), [typeof(string)] },
        { typeof(decimal), [typeof(decimal)] },
        { typeof(TimeSpan), [typeof(TimeSpan)] }
    }.ToFrozenDictionary(static entry => entry.Key, static entry => entry.Value.ToFrozenSet());

    private static readonly FrozenDictionary<Type, FrozenSet<Type>> ValidImplicitConversions = new Dictionary<Type, Type[]>
    {
        [typeof(sbyte)] =
            [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(byte)] =
        [
            typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float),
            typeof(double), typeof(decimal)
        ],
        [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(ushort)] =
        [
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
        ],
        [typeof(int)] = [typeof(long), typeof(float), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(ulong)] = [typeof(float), typeof(double), typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(char)] =
        [
            typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal)
        ]
    }.ToFrozenDictionary(static entry => entry.Key, static entry => entry.Value.ToFrozenSet());

    private static readonly FrozenDictionary<(Type, Type), int> ConversionCosts = new Dictionary<(Type, Type), int>()
    {
        [(typeof(sbyte), typeof(short))] = 1,
        [(typeof(sbyte), typeof(int))] = 2,
        [(typeof(sbyte), typeof(long))] = 3,
        [(typeof(sbyte), typeof(float))] = 4,
        [(typeof(sbyte), typeof(double))] = 5,
        [(typeof(sbyte), typeof(decimal))] = 6,

        [(typeof(byte), typeof(short))] = 1,
        [(typeof(byte), typeof(ushort))] = 1,
        [(typeof(byte), typeof(int))] = 2,
        [(typeof(byte), typeof(uint))] = 2,
        [(typeof(byte), typeof(long))] = 3,
        [(typeof(byte), typeof(ulong))] = 3,
        [(typeof(byte), typeof(float))] = 4,
        [(typeof(byte), typeof(double))] = 5,
        [(typeof(byte), typeof(decimal))] = 6,

        [(typeof(short), typeof(int))] = 1,
        [(typeof(short), typeof(long))] = 2,
        [(typeof(short), typeof(float))] = 3,
        [(typeof(short), typeof(double))] = 4,
        [(typeof(short), typeof(decimal))] = 5,

        [(typeof(ushort), typeof(int))] = 1,
        [(typeof(ushort), typeof(uint))] = 1,
        [(typeof(ushort), typeof(long))] = 2,
        [(typeof(ushort), typeof(ulong))] = 2,
        [(typeof(ushort), typeof(float))] = 3,
        [(typeof(ushort), typeof(double))] = 4,
        [(typeof(ushort), typeof(decimal))] = 5,

        [(typeof(int), typeof(long))] = 1,
        [(typeof(int), typeof(float))] = 2,
        [(typeof(int), typeof(double))] = 2,
        [(typeof(int), typeof(decimal))] = 3,

        [(typeof(uint), typeof(long))] = 1,
        [(typeof(uint), typeof(ulong))] = 1,
        [(typeof(uint), typeof(float))] = 2,
        [(typeof(uint), typeof(double))] = 2,
        [(typeof(uint), typeof(decimal))] = 3,

        [(typeof(long), typeof(float))] = 1,
        [(typeof(long), typeof(double))] = 1,
        [(typeof(long), typeof(decimal))] = 2,

        [(typeof(ulong), typeof(float))] = 1,
        [(typeof(ulong), typeof(double))] = 1,
        [(typeof(ulong), typeof(decimal))] = 2,

        [(typeof(float), typeof(double))] = 1,

        [(typeof(char), typeof(ushort))] = 1,
        [(typeof(char), typeof(int))] = 2,
        [(typeof(char), typeof(uint))] = 2,
        [(typeof(char), typeof(long))] = 3,
        [(typeof(char), typeof(ulong))] = 3,
        [(typeof(char), typeof(float))] = 4,
        [(typeof(char), typeof(double))] = 5,
        [(typeof(char), typeof(decimal))] = 6
    }.ToFrozenDictionary();

    private static bool IsTypePossibleToConvert(Type to, Type from)
    {
        if (from == typeof(IDynamicMetaObjectProvider))
            return true;
        if (TypeCompatibilityTable.TryGetValue(to, out var compatibleTypes))
            return compatibleTypes.Contains(from);
        return to == from || to.IsAssignableFrom(from);
    }

    private static bool CanSafelyPassNull(Type to, Type from)
    {
        if (!IsNullType(from))
            return false;
        return (to.IsGenericType && to.GetGenericTypeDefinition() == typeof(Nullable<>))
               || to.IsGenericParameter
               || !to.IsValueType;
    }

    private static bool IsNullType(Type type)
    {
        return type.FullName == typeof(NullNode.NullType).FullName;
    }

    private static bool TypeConformsToConstraints(Type genericType, Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;

        var interfaces = genericType.GetGenericParameterConstraints()
            .Where(t => t.IsInterface);

        if (interfaces.Any(@interface => !effectiveType.GetInterfaces().Contains(@interface))) return false;

        var specialConstraints = genericType.GenericParameterAttributes;

        if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0
            && !effectiveType.IsValueType)
            return false;

        if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0
            && effectiveType.IsValueType)
            return false;

        var baseConstraint = genericType.GetGenericParameterConstraints()
            .FirstOrDefault(t => !t.IsInterface);
        if (baseConstraint != null)
            if (!baseConstraint.IsAssignableFrom(effectiveType))
                return false;

        if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) == 0) return true;

        if (effectiveType.IsValueType) return true;

        var constructor = effectiveType.GetConstructor(Type.EmptyTypes);

        return constructor != null;
    }

    private static Type FindCommonBaseType(IReadOnlyList<Type> types, int startIndex)
    {
        var count = types.Count - startIndex;
        if (count <= 0)
            return typeof(object);

        var nonNullTypes = new List<Type>();
        for (var i = startIndex; i < types.Count; i++)
        {
            if (!IsNullType(types[i]))
                nonNullTypes.Add(types[i]);
        }

        return FindCommonBaseTypeFromNonNullTypes(nonNullTypes);
    }

    private static Type FindCommonBaseTypeFromNonNullTypes(List<Type> nonNullTypes)
    {
        if (nonNullTypes.Count == 0)
            return typeof(object);

        if (nonNullTypes.Count == 1)
            return nonNullTypes.First();

        var commonBaseTypes = new HashSet<Type>(GetTypeHierarchy(nonNullTypes.First()));

        foreach (var type in nonNullTypes.Skip(1))
        {
            var currentHierarchy = GetTypeHierarchy(type);
            commonBaseTypes.IntersectWith(currentHierarchy);

            if (commonBaseTypes.Count == 1 && commonBaseTypes.Single() == typeof(object))
                return typeof(object);
        }

        return FindMostSpecificType(commonBaseTypes);
    }

    private static readonly ConcurrentDictionary<Type, HashSet<Type>> TypeHierarchyCache = new();

    private static HashSet<Type> GetTypeHierarchy(Type type)
    {
        if (type == null)
            return [];

        return TypeHierarchyCache.GetOrAdd(type, static t =>
        {
            var hierarchy = new HashSet<Type>();
            var current = t;
            while (current != null)
            {
                hierarchy.Add(current);
                current = current.BaseType;
            }

            return hierarchy;
        });
    }

    private static Type FindMostSpecificType(HashSet<Type> types)
    {
        if (types == null || types.Count == 0)
            return typeof(object);

        var mostSpecific = typeof(object);
        foreach (var type in types)
        {
            if (mostSpecific == typeof(object))
            {
                mostSpecific = type;
                continue;
            }

            if (type.IsSubclassOf(mostSpecific))
                mostSpecific = type;
        }

        return mostSpecific;
    }

    private static bool CanImplicitlyConvert(Type from, Type to)
    {
        if (from == null || to == null)
            return false;

        if (!from.IsPrimitive || !to.IsPrimitive) return false;

        return ValidImplicitConversions.TryGetValue(from, out var targetTypes) && targetTypes.Contains(to);
    }

    private static int GetNumericConversionCost(Type from, Type to)
    {
        return ConversionCosts.TryGetValue((from, to), out var cost) ? cost : int.MaxValue;
    }

}
