using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Schema;

/// <summary>Validates that a declared value shape retains concrete types, including nested members.</summary>
public static class ConcreteValueShape
{
    /// <summary>Returns the first unresolved member path, or null for a resolved shape.</summary>
    public static string? FindUnresolvedMember(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return Find(valueType, valueType.Name, new HashSet<Type>());
    }

    /// <summary>Validates transported value types without traversing unrelated members of opaque CLR references.</summary>
    public static string? FindUnresolvedValueType(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return FindValueType(valueType, valueType.Name, new HashSet<Type>());
    }

    private static string? FindValueType(Type type, string path, HashSet<Type> seen)
    {
        if (type == typeof(object) || type == typeof(ValueType) || type == typeof(Enum) || type.ContainsGenericParameters ||
            typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(type))
            return path;
        if (!seen.Add(type)) return null;
        if (type.HasElementType) return FindValueType(type.GetElementType()!, path + "[]", seen);
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            var contracts = type.GetInterfaces().Prepend(type).Where(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
            if (contracts.Length == 0) return path;
            foreach (var contract in contracts)
            {
                var unresolved = FindValueType(contract.GetGenericArguments()[0], path + "[]", seen);
                if (unresolved is not null) return unresolved;
            }
        }
        foreach (var argument in type.GetGenericArguments())
        {
            var unresolved = FindValueType(argument, path + "<" + argument.Name + ">", seen);
            if (unresolved is not null) return unresolved;
        }
        return null;
    }

    private static string? Find(Type type, string path, HashSet<Type> seen)
    {
        if (type == typeof(object) || type == typeof(ValueType) || type == typeof(Enum) || type.ContainsGenericParameters ||
            typeof(System.Dynamic.IDynamicMetaObjectProvider).IsAssignableFrom(type))
            return path;
        if (!seen.Add(type))
            return null;
        if (type.HasElementType)
            return Find(type.GetElementType()!, path + "[]", seen);
        foreach (var argument in type.GetGenericArguments())
        {
            var unresolved = Find(argument, path + "<" + argument.Name + ">", seen);
            if (unresolved is not null)
                return unresolved;
        }

        return FindDeclaredMembers(type, path, seen);
    }

    private static string? FindDeclaredMembers(Type type, string path, HashSet<Type> seen)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) ||
            type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(Guid) || type == typeof(Uri))
            return null;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var unresolved = Find(property.PropertyType, path + "." + property.Name, seen);
            if (unresolved is not null)
                return unresolved;
        }
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var unresolved = Find(field.FieldType, path + "." + field.Name, seen);
            if (unresolved is not null)
                return unresolved;
        }

        return null;
    }
}
