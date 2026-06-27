using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.TypedOutput;

internal static class TypedOutputBinder
{
    public static TypedOutputBindingPlan Create(Type outputType, IReadOnlyList<TypedOutputColumn> columns)
    {
        ArgumentNullException.ThrowIfNull(outputType);
        ArgumentNullException.ThrowIfNull(columns);

        var orderedColumns = columns.ToArray();
        var aliases = CreateAliasMap(orderedColumns);
        var constructorBinding = TryBindConstructor(outputType, orderedColumns, aliases);
        if (constructorBinding != null)
            return constructorBinding;

        return BindMembers(outputType, orderedColumns, aliases);
    }

    private static Dictionary<string, TypedOutputColumn> CreateAliasMap(
        IReadOnlyList<TypedOutputColumn> columns)
    {
        var aliases = new Dictionary<string, TypedOutputColumn>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in columns)
        {
            var alias = NormalizeAlias(column.Name);
            if (!aliases.TryAdd(alias, column))
                throw new InvalidOperationException($"Typed output binding has duplicate output alias '{alias}'.");
        }

        return aliases;
    }

    private static TypedOutputBindingPlan? TryBindConstructor(
        Type outputType,
        IReadOnlyList<TypedOutputColumn> columns,
        IReadOnlyDictionary<string, TypedOutputColumn> aliases)
    {
        var candidates = new List<TypedOutputBindingPlan>();
        foreach (var constructor in outputType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != aliases.Count)
                continue;

            var bindings = new List<TypedOutputConstructorBinding>();
            var matched = true;
            foreach (var parameter in parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name) ||
                    !aliases.TryGetValue(parameter.Name, out var column) ||
                    !CanAssign(column.Type, parameter.ParameterType))
                {
                    matched = false;
                    break;
                }

                bindings.Add(new TypedOutputConstructorBinding(column, parameter.ParameterType));
            }

            if (matched)
                candidates.Add(new TypedOutputBindingPlan(outputType, columns, constructor, bindings, []));
        }

        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => throw new InvalidOperationException(
                $"Typed output type '{outputType.FullName}' has multiple public constructors that match query output aliases.")
        };
    }

    private static TypedOutputBindingPlan BindMembers(
        Type outputType,
        IReadOnlyList<TypedOutputColumn> columns,
        IReadOnlyDictionary<string, TypedOutputColumn> aliases)
    {
        if (outputType.GetConstructor(Type.EmptyTypes) == null)
            throw new InvalidOperationException(
                $"Typed output type '{outputType.FullName}' must expose either a matching public constructor or a public parameterless constructor.");

        var members = GetBindableMembers(outputType);
        var bindings = new List<TypedOutputMemberBinding>(aliases.Count);
        foreach (var column in columns)
        {
            var alias = NormalizeAlias(column.Name);
            if (!members.TryGetValue(alias, out var member))
                throw new InvalidOperationException(
                    $"Typed output type '{outputType.FullName}' does not expose writable member '{alias}'.");

            if (!CanAssign(column.Type, member.Type))
                throw new InvalidOperationException(
                    $"Typed output member '{member.Name}' expects '{member.Type.FullName}', but query column '{column.Name}' has type '{column.Type.FullName}'.");

            bindings.Add(new TypedOutputMemberBinding(member.Name, member.Type, column, member.Member));
        }

        return new TypedOutputBindingPlan(outputType, columns, null, [], bindings);
    }

    private static Dictionary<string, BindableMember> GetBindableMembers(Type outputType)
    {
        var members = new Dictionary<string, BindableMember>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in outputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 0 || property.SetMethod == null)
                continue;

            AddMember(members, new BindableMember(property.Name, property.PropertyType, property));
        }

        foreach (var field in outputType.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.IsInitOnly)
                continue;

            AddMember(members, new BindableMember(field.Name, field.FieldType, field));
        }

        return members;
    }

    private static void AddMember(IDictionary<string, BindableMember> members, BindableMember member)
    {
        if (!members.TryAdd(member.Name, member))
            throw new InvalidOperationException($"Typed output member '{member.Name}' is ambiguous.");
    }

    private static string NormalizeAlias(string name)
    {
        var index = name.LastIndexOf('.');
        return index >= 0 && index + 1 < name.Length
            ? name[(index + 1)..]
            : name;
    }

    private static bool CanAssign(Type sourceType, Type targetType)
    {
        if (targetType == typeof(object))
            return true;

        if (targetType.IsAssignableFrom(sourceType))
            return true;

        var nullableTarget = Nullable.GetUnderlyingType(targetType);
        return nullableTarget != null && nullableTarget == sourceType;
    }

    private sealed record BindableMember(string Name, Type Type, MemberInfo Member);
}
