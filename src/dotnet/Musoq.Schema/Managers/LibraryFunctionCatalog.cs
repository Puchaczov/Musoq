using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

/// <summary>Discovers library-owned declarations once, preferring the most-derived signature.</summary>
public sealed class LibraryFunctionCatalog
{
    private static readonly ConcurrentDictionary<Type, LibraryFunctionCatalog> Cache = new();

    private LibraryFunctionCatalog(Dictionary<string, SqlFunctionDescriptor> registrations)
    {
        Registrations = registrations.ToImmutableArray();
        Functions = Registrations.Select(static registration => registration.Value).ToImmutableArray();
    }

    internal ImmutableArray<KeyValuePair<string, SqlFunctionDescriptor>> Registrations { get; }

    /// <summary>Gets immutable declarations including compiler-only operations.</summary>
    public ImmutableArray<SqlFunctionDescriptor> Functions { get; }

    /// <summary>Gets the catalog for a concrete library type without invoking its methods.</summary>
    public static LibraryFunctionCatalog For(Type libraryType)
    {
        ArgumentNullException.ThrowIfNull(libraryType);
        if (!typeof(LibraryBase).IsAssignableFrom(libraryType))
            throw new ArgumentException("Function catalogs require a LibraryBase type.", nameof(libraryType));

        return Cache.GetOrAdd(libraryType, Create);
    }

    private static LibraryFunctionCatalog Create(Type libraryType)
    {
        var selected = new Dictionary<string, SqlFunctionDescriptor>(StringComparer.Ordinal);
        for (var current = libraryType; current is not null && typeof(LibraryBase).IsAssignableFrom(current);
             current = current.BaseType)
        {
            foreach (var function in ReadDeclaredFunctions(current))
                SelectFunction(selected, function);
        }

        return new LibraryFunctionCatalog(selected);
    }

    private static IEnumerable<SqlFunctionDescriptor> ReadDeclaredFunctions(Type type)
        => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(static method => SqlFunctionDeclarations.Read(method));

    private static void SelectFunction(Dictionary<string, SqlFunctionDescriptor> selected, SqlFunctionDescriptor function)
    {
        var key = SignatureKey(function);
        if (!selected.TryGetValue(key, out var existing))
        {
            selected.Add(key, function);
            return;
        }

        if (existing.Declaration.DeclaringType == function.Declaration.DeclaringType &&
            existing.Declaration != function.Declaration)
            throw new InvalidOperationException($"Competing {function.Kind} declarations for SQL function '{function.Name}'.");
    }

    internal static string SignatureKey(SqlFunctionDescriptor function)
    {
        var values = string.Join(",", function.Parameters.Select(static parameter => TypeIdentity(parameter.Type)));
        var injections = string.Join(",", function.Declaration.GetParameters()
            .Where(static parameter => parameter.GetCustomAttribute<InjectTypeAttribute>() is not null)
            .Select(static parameter => TypeIdentity(parameter.ParameterType)));
        return $"{function.Kind}:{MethodNameNormalizer.Normalize(function.Name)}:{function.Declaration.GetGenericArguments().Length}:{values}:{injections}";
    }

    private static string TypeIdentity(Type type)
    {
        if (type.IsGenericParameter)
            return $"parameter:{type.GenericParameterPosition}";
        if (type.IsArray)
            return $"array:{type.GetArrayRank()}:{TypeIdentity(type.GetElementType()!)}";
        if (type.IsByRef)
            return $"reference:{TypeIdentity(type.GetElementType()!)}";
        if (type.IsGenericType)
            return $"{type.GetGenericTypeDefinition().AssemblyQualifiedName}<{string.Join(",", type.GetGenericArguments().Select(TypeIdentity))}>";

        return type.AssemblyQualifiedName ?? type.Name;
    }
}
