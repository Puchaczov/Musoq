using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static class BuildMetadataAndInferTypesVisitorUtilities
{
    /// <summary>
    ///     Finds the closest common parent type between two types in the inheritance hierarchy.
    /// </summary>
    public static Type FindClosestCommonParent(Type first, Type second)
    {
        var type1Ancestors = new HashSet<Type>();

        while (first != null)
        {
            type1Ancestors.Add(first);
            first = first.BaseType;
        }

        while (second != null)
        {
            if (type1Ancestors.Contains(second)) return second;

            second = second.BaseType;
        }

        return typeof(object);
    }

    /// <summary>
    ///     Makes a value type nullable, or returns the type as-is if it's already nullable or a reference type.
    /// </summary>
    public static Type MakeTypeNullable(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            || !type.IsValueType)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    /// <summary>
    ///     Strips the nullable wrapper from a nullable type, or returns the type as-is if it's not nullable.
    /// </summary>
    public static Type StripNullable(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return Nullable.GetUnderlyingType(type);

        return type;
    }

    /// <summary>
    ///     Checks if a type has an indexer property (supports array-like access).
    /// </summary>
    public static bool HasIndexer(Type type)
    {
        return type is not null && type.GetProperties().Any(f => f.GetIndexParameters().Length > 0);
    }

    /// <summary>
    ///     Checks if a type supports indexing (has an indexer property or is an array).
    /// </summary>
    public static bool IsIndexableType(Type type)
    {
        if (type == null) return false;

        try
        {
            if (type.IsArray)
                return true;

            if (type == typeof(string))
                return true;

            return type.GetProperties().Any(p => p.GetIndexParameters().Length > 0);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is TypeLoadException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Checks if a type is a primitive type that cannot have property access.
    /// </summary>
    public static bool IsPrimitiveType(Type type)
    {
        if (type == null) return false;

        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
               type == typeof(DateTimeOffset);
    }

    /// <summary>
    ///     Checks if a type is a valid query expression type.
    ///     Valid types are primitive types (numeric, bool, char), string, decimal, DateTime, DateTimeOffset, Guid, TimeSpan,
    ///     and null.
    ///     Nullable versions of these types are also valid.
    ///     Arrays and complex types (classes, structs) are not valid.
    /// </summary>
    public static bool IsValidQueryExpressionType(Type type)
    {
        if (type == null) return false;

        if (type.FullName == typeof(NullNode.NullType).FullName) return true;

        if (type.IsArray) return false;

        var typeToCheck = StripNullable(type);

        return IsPrimitiveType(typeToCheck) ||
               typeToCheck == typeof(Guid) ||
               typeToCheck == typeof(TimeSpan);
    }

    /// <summary>
    ///     Checks if a column should be included when expanding the star (*) operator.
    ///     Filters out arrays and non-primitive types.
    ///     <para>
    ///         In this context, a "primitive type" is defined by the <see cref="IsPrimitiveType" /> method,
    ///         which returns true for .NET primitive types, as well as <see cref="string" />, <see cref="decimal" />,
    ///         <see cref="DateTime" />, and <see cref="DateTimeOffset" />.
    ///     </para>
    /// </summary>
    public static bool ShouldIncludeColumnInStarExpansion(Type columnType)
    {
        if (columnType == null) return false;

        if (columnType.IsArray)
            return false;

        var typeToCheck = StripNullable(columnType);

        return IsPrimitiveType(typeToCheck);
    }

    /// <summary>
    ///     Checks if a type is a generic enumerable and returns the element type.
    /// </summary>
    public static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null;

        if (!type.IsGenericType) return false;

        var interfaces = type.GetInterfaces().Concat([type]);

        foreach (var interfaceType in interfaces)
        {
            if (!interfaceType.IsGenericType ||
                interfaceType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;

            elementType = interfaceType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a type is an array and returns the element type.
    /// </summary>
    public static bool IsArray(Type type, out Type elementType)
    {
        elementType = null;

        if (!type.IsArray) return false;

        elementType = type.GetElementType();
        return true;
    }

    /// <summary>
    ///     Creates position indexes for set operation fields.
    /// </summary>
    public static int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
    {
        var indexes = new int[keys.Length];

        var fieldIndex = 0;
        var index = 0;

        foreach (var field in node.Select.Fields)
        {
            if (keys.Contains(field.FieldName))
                indexes[index++] = fieldIndex;

            fieldIndex += 1;
        }

        return indexes;
    }
}