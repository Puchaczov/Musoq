using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    internal static readonly FrozenDictionary<Type, FrozenSet<Type>> ValidImplicitConversions = new Dictionary<Type, Type[]>
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
        [typeof(int)] = [typeof(long), typeof(double), typeof(decimal)],
        [typeof(uint)] = [typeof(long), typeof(ulong), typeof(double), typeof(decimal)],
        [typeof(long)] = [typeof(decimal)],
        [typeof(ulong)] = [typeof(decimal)],
        [typeof(float)] = [typeof(double)],
        [typeof(char)] =
        [
            typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(decimal)
        ]
    }.ToFrozenDictionary(static entry => entry.Key, static entry => entry.Value.ToFrozenSet());

    internal static readonly FrozenDictionary<(Type, Type), int> ConversionCosts = new Dictionary<(Type, Type), int>()
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
        [(typeof(int), typeof(double))] = 2,
        [(typeof(int), typeof(decimal))] = 3,

        [(typeof(uint), typeof(long))] = 1,
        [(typeof(uint), typeof(ulong))] = 1,
        [(typeof(uint), typeof(double))] = 2,
        [(typeof(uint), typeof(decimal))] = 3,

        [(typeof(long), typeof(decimal))] = 2,

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
        effectiveType = Musoq.Schema.StructuralInputs.GeneratedRecordConstraintWitness.Resolve(genericType, effectiveType);

        var interfaces = genericType.GetGenericParameterConstraints()
            .Where(t => t.IsInterface);

        if (interfaces.Any(@interface => !ConformsToConstructedConstraint(@interface, genericType, effectiveType))) return false;

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
            if (!ConformsToConstructedConstraint(baseConstraint, genericType, effectiveType))
                return false;

        if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) == 0) return true;

        if (effectiveType.IsValueType) return true;

        var constructor = effectiveType.GetConstructor(Type.EmptyTypes);

        return constructor != null;
    }

    private static bool CanImplicitlyConvert(Type from, Type to)
    {
        if (from == null || to == null)
            return false;

        return ValidImplicitConversions.TryGetValue(from, out var targetTypes) && targetTypes.Contains(to);
    }

    private static int GetNumericConversionCost(Type from, Type to)
    {
        return ConversionCosts.TryGetValue((from, to), out var cost) ? cost : int.MaxValue;
    }

}
