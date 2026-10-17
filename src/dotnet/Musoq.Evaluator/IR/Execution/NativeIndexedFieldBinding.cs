using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal static class NativeIndexedFieldBinding
{
    private static readonly Regex Segments = new(@"(?<member>[^.\[\]]+)|\[(?<index>-?\d+|'[^']*')\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static ExecutionExpression? TryCreate(string alias, FieldBinding field, SourceEntityShape source,
        string path)
    {
        ExecutionExpression? receiver = null;
        var type = source.EntityType.ResolveClrType();
        var position = 0;
        foreach (Match segment in Segments.Matches(path))
        {
            var separator = path[position..segment.Index];
            if (separator.Length != 0 && separator != ".")
                return null;
            position = segment.Index + segment.Length;
            if (segment.Groups["member"].Success)
            {
                var name = segment.Groups["member"].Value;
                if (receiver is null && TryCreateDeclaredArrayRoot(alias, source, name) is { } declaredArray)
                {
                    receiver = declaredArray;
                    type = declaredArray.ReturnType.ResolveClrType();
                    continue;
                }
                var memberType = type.GetProperty(name)?.PropertyType ?? type.GetField(name)?.FieldType;
                if (memberType is null)
                {
                    var dictionary = type.GetInterfaces().Prepend(type).FirstOrDefault(candidate =>
                        candidate.IsGenericType && candidate.GetGenericArguments()[0] == typeof(string) &&
                        (candidate.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
                         candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
                    if (dictionary is null) return null;
                    receiver ??= new ExecutionVariableRead(new ExecutionVariable(alias, source.EntityType));
                    var valueType = dictionary.GetGenericArguments()[1];
                    receiver = ExecutionIndexedAccessBinding.Bind(new ExecutionArrayAccess(receiver,
                        new ExecutionLiteral(name, typeof(string)), valueType, valueType));
                    type = valueType;
                    continue;
                }
                receiver = receiver is null
                    ? new ExecutionFieldRead(alias, name, memberType, new ClrPropertyAccess(name))
                        { Stability = field.Stability }
                    : new ExecutionMemberRead(receiver, name, ResolveMemberResultType(field, memberType, path.Length - position), false)
                        { IsNullSafe = !type.IsValueType || Nullable.GetUnderlyingType(type) != null };
                type = memberType;
                continue;
            }

            if (receiver is null)
                return null;
            var token = segment.Groups["index"].Value;
            var index = token.StartsWith("'", StringComparison.Ordinal)
                ? new ExecutionLiteral(token[1..^1], typeof(string))
                : new ExecutionLiteral(int.Parse(token, CultureInfo.InvariantCulture), typeof(int));
            var contract = type.GetInterfaces().Prepend(type).FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() is var definition &&
                (definition == typeof(IReadOnlyList<>) || definition == typeof(IList<>) ||
                 definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>)));
            var element = type.IsArray ? type.GetElementType() : contract?.GetGenericArguments()[^1];
            if (element is null)
                return null;
            receiver = ExecutionIndexedAccessBinding.Bind(new ExecutionArrayAccess(receiver, index, element, element));
            type = element;
        }
        return position == path.Length ? receiver : null;
    }

    internal static ExecutionExpression? TryCreateFromReceiver(
        ExecutionExpression receiver,
        Type receiverType,
        FieldBinding field,
        string path)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(receiverType);
        ArgumentNullException.ThrowIfNull(field);

        var type = receiverType;
        var position = 0;
        foreach (Match segment in Segments.Matches(path))
        {
            var separator = path[position..segment.Index];
            if (separator.Length != 0 && separator != ".")
                return null;
            position = segment.Index + segment.Length;
            if (segment.Groups["member"].Success)
            {
                var name = segment.Groups["member"].Value;
                var memberType = type.GetProperty(name)?.PropertyType ?? type.GetField(name)?.FieldType;
                if (memberType is null)
                    return null;

                receiver = new ExecutionMemberRead(
                    receiver,
                    name,
                    ResolveMemberResultType(field, memberType, path.Length - position),
                    false)
                {
                    IsNullSafe = !type.IsValueType || Nullable.GetUnderlyingType(type) != null
                };
                type = memberType;
                continue;
            }

            var token = segment.Groups["index"].Value;
            var index = token.StartsWith("'", StringComparison.Ordinal)
                ? new ExecutionLiteral(token[1..^1], typeof(string))
                : new ExecutionLiteral(int.Parse(token, CultureInfo.InvariantCulture), typeof(int));
            var contract = type.GetInterfaces().Prepend(type).FirstOrDefault(candidate =>
                candidate.IsGenericType && candidate.GetGenericTypeDefinition() is var definition &&
                (definition == typeof(IReadOnlyList<>) || definition == typeof(IList<>) ||
                 definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>)));
            var element = type.IsArray ? type.GetElementType() : contract?.GetGenericArguments()[^1];
            if (element is null)
                return null;

            receiver = ExecutionIndexedAccessBinding.Bind(new ExecutionArrayAccess(receiver, index, element, element));
            type = element;
        }

        return position == path.Length ? receiver : null;
    }

    private static ExecutionFieldRead? TryCreateDeclaredArrayRoot(string alias, SourceEntityShape source, string name)
    {
        if (source.GeneratedTypeName is null)
            return null;
        var field = source.Fields.FirstOrDefault(candidate => candidate.Name.Equals(name, StringComparison.Ordinal));
        if (field is null || !field.Type.ResolveClrType().IsArray)
            return null;
        return new ExecutionFieldRead(alias, field.Name, field.Type, field.AccessStrategy, field.GeneratedTypeName)
        { Stability = field.Stability, SourceReadType = field.SourceReadType, EnumType = field.EnumType };
    }

    private static Type ResolveMemberResultType(FieldBinding field, Type memberType, int remainingPathLength)
    {
        var expectedType = field.Type.ResolveClrType();
        if (remainingPathLength == 0 && Nullable.GetUnderlyingType(expectedType) == memberType)
            return expectedType;
        return memberType;
    }
}
