using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
/// Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static class BuildMetadataAndInferTypesVisitorUtilities
{
    /// <summary>
    /// Finds the closest common parent type between two types in the inheritance hierarchy.
    /// </summary>
    public static Type FindClosestCommonParent(Type type1, Type type2)
    {
        var type1Ancestors = new HashSet<Type>();

        while (type1 != null)
        {
            type1Ancestors.Add(type1);
            type1 = type1.BaseType;
        }

        while (type2 != null)
        {
            if (type1Ancestors.Contains(type2))
            {
                return type2;
            }

            type2 = type2.BaseType;
        }

        return typeof(object);
    }

    /// <summary>
    /// Makes a value type nullable, or returns the type as-is if it's already nullable or a reference type.
    /// </summary>
    public static Type MakeTypeNullable(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
            || !type.IsValueType)
        {
            return type;
        }

        return typeof(Nullable<>).MakeGenericType(type);
    }

    /// <summary>
    /// Strips the nullable wrapper from a nullable type, or returns the type as-is if it's not nullable.
    /// </summary>
    public static Type StripNullable(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(type);
        }

        return type;
    }

    /// <summary>
    /// Checks if a type has an indexer property (supports array-like access).
    /// </summary>
    public static bool HasIndexer(Type type)
    {
        return type is not null && type.GetProperties().Any(f => f.GetIndexParameters().Length > 0);
    }

    /// <summary>
    /// Checks if a type supports indexing (has an indexer property or is an array).
    /// </summary>
    public static bool IsIndexableType(Type type)
    {
        if (type == null) return false;
        
        try
        {
            // Arrays are indexable
            if (type.IsArray)
                return true;

            // Strings are indexable
            if (type == typeof(string))
                return true;

            // Check for indexer properties
            return type.GetProperties().Any(p => p.GetIndexParameters().Length > 0);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is TypeLoadException)
        {
            // If we can't access type properties due to type loading issues, assume not indexable
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a type is a primitive type that cannot have property access.
    /// </summary>
    public static bool IsPrimitiveType(Type type)
    {
        if (type == null) return false;
        
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
    }

    /// <summary>
    /// Checks if a type is a generic enumerable and returns the element type.
    /// </summary>
    public static bool IsGenericEnumerable(Type type, out Type elementType)
    {
        elementType = null;
    
        // Check if the type is a generic type
        if (!type.IsGenericType) return false;
            
        // Get all interfaces implemented by the type
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
    /// Checks if a type is an array and returns the element type.
    /// </summary>
    public static bool IsArray(Type type, out Type elementType)
    {
        elementType = null;
    
        if (!type.IsArray) return false;
            
        elementType = type.GetElementType();
        return true;
    }

    /// <summary>
    /// Creates position indexes for set operation fields.
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