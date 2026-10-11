using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Musoq.Schema;

namespace Musoq.Converter;

/// <summary>Separates semantic templates whose captured provider graphs bind different concrete source types.</summary>
internal static class SemanticProviderIdentity
{
    private static readonly ConditionalWeakTable<object, Identity> Identities = new();
    private static readonly ConditionalWeakTable<Type, CapturedFields> Fields = new();
    private static long _nextIdentity;

    internal static string Get(ISchemaProvider provider)
    {
        var result = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Append(provider, result, visited, 0);
        return result.ToString();
    }

    private static void Append(object? value, StringBuilder result, HashSet<object> visited, int depth)
    {
        if (value is null || depth > 8 || !visited.Add(value))
            return;
        if (value is ISchemaProvider or ISchema or ISchemaTable)
            result.Append(Identities.GetValue(value, static _ => new Identity(Interlocked.Increment(ref _nextIdentity))).Value).Append(';');
        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
                Append(entry.Value, result, visited, depth + 1);
            return;
        }
        if (value is ITuple tuple)
        {
            for (var index = 0; index < tuple.Length; index++)
                Append(tuple[index], result, visited, depth + 1);
            return;
        }
        if (value is not ISchemaProvider and not ISchema)
            return;
        foreach (var field in Fields.GetValue(value.GetType(), static type => new CapturedFields(FindCapturedFields(type))).Value)
            Append(field.GetValue(value), result, visited, depth + 1);
    }

    private static FieldInfo[] FindCapturedFields(Type type)
    {
        var result = new List<FieldInfo>();
        for (var current = type; current is not null; current = current.BaseType)
            result.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(static field => CanContainContract(field.FieldType)));
        return result.ToArray();
    }

    private static bool CanContainContract(Type type)
        => type == typeof(object) || typeof(ISchemaProvider).IsAssignableFrom(type) ||
           typeof(ISchema).IsAssignableFrom(type) || typeof(ISchemaTable).IsAssignableFrom(type) ||
           typeof(IDictionary).IsAssignableFrom(type) || typeof(ITuple).IsAssignableFrom(type) ||
           type.GetInterfaces().Append(type).Any(static candidate => candidate.IsGenericType &&
               candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    private sealed record CapturedFields(FieldInfo[] Value);
    private sealed record Identity(long Value);
}
