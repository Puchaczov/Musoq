using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static readonly ConcurrentDictionary<Type, string> CastableTypeCache = new();

    public static string GetCastableType(Type type)
    {
        if (type is NullNode.NullType) return "object";


        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(char)) return "char";
        if (type == typeof(object)) return "object";
        if (type == typeof(void)) return "void";


        return CastableTypeCache.GetOrAdd(type, ComputeCastableType);
    }

    private static string ComputeCastableType(Type type)
    {
        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return $"{GetCastableType(elementType)}[]";
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            return $"{GetCastableType(underlyingType)}?";
        }

        if (type.IsNested) return $"{GetCastableType(GetClosedDeclaringType(type))}.{GetNestedTypeName(type)}";
        if (type.IsGenericType) return GetFriendlyTypeName(type);

        if (IsWellKnownNamespace(type.Namespace))
            return type.Name;

        return ReplacePlusWithDotForNestedClasses(type.FullName ?? type.Name);
    }

    private static Type GetClosedDeclaringType(Type type)
    {
        var declaringType = type.DeclaringType;
        if (declaringType == null ||
            !declaringType.ContainsGenericParameters ||
            !type.IsGenericType)
            return declaringType ?? throw new InvalidOperationException($"Nested type '{type.FullName ?? type.Name}' does not have a declaring type.");

        var declaringArguments = declaringType.GetGenericArguments();
        var nestedArguments = type.GetGenericArguments();
        return nestedArguments.Length >= declaringArguments.Length
            ? declaringType.MakeGenericType(nestedArguments.Take(declaringArguments.Length).ToArray())
            : declaringType;
    }

    private static string GetNestedTypeName(Type type)
    {
        var name = StripGenericArity(type.Name);
        if (!type.IsGenericType)
            return name;

        var declaringArgumentCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
        var nestedArguments = type.GetGenericArguments().Skip(declaringArgumentCount).ToArray();
        return nestedArguments.Length == 0
            ? name
            : $"{name}<{string.Join(", ", nestedArguments.Select(GetCastableType))}>";
    }

    private static string StripGenericArity(string typeName)
    {
        var index = typeName.IndexOf('`', StringComparison.Ordinal);
        return index > 0 ? typeName[..index] : typeName;
    }

    public static Type[] GetNestedTypes(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type), @"Type cannot be null");

        if (!type.IsGenericType)
            return [type];

        var types = new Stack<Type>();

        types.Push(type);
        var finalTypes = new List<Type>();

        while (types.Count > 0)
        {
            var cType = types.Pop();
            finalTypes.Add(cType);

            if (cType.IsGenericType)
                foreach (var argType in cType.GetGenericArguments())
                    types.Push(argType);
        }

        return finalTypes.ToArray();
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericParameter) return type.Name;

        if (!type.IsGenericType) return GetCastableType(type);

        var builder = new StringBuilder();
        var name = type.Name;
        var shortName = StripGenericArity(name);
        builder.Append(IsWellKnownNamespace(type.Namespace) ? shortName : $"{type.Namespace}.{shortName}");
        builder.Append('<');
        var first = true;
        foreach (var arg in type.GetGenericArguments())
        {
            if (!first) builder.Append(", ");
            builder.Append(GetCastableType(arg));
            first = false;
        }

        builder.Append('>');
        return builder.ToString();
    }

    private static string ReplacePlusWithDotForNestedClasses(string fullName)
    {
        return fullName.Replace("+", ".", StringComparison.Ordinal);
    }

    private static bool IsWellKnownNamespace(string? ns)
    {
        return ns is "System" or "System.Collections.Generic" or "System.Linq" or "System.Threading" or "System.Threading.Tasks";
    }

    public static string RemapPrimitiveTypes(string typeName)
    {
        return PrimitiveTypeResolver.RemapPrimitiveTypeName(typeName);
    }

    public static Type? RemapPrimitiveTypeAsNullable(string typeName)
    {
        return PrimitiveTypeResolver.RemapPrimitiveTypeAsNullable(typeName);
    }

}
