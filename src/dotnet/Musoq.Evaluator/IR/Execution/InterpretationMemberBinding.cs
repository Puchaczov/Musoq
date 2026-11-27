using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class InterpretationMemberBinding
{
    private static readonly Regex Segments = new(
        @"(?<member>[^.\[\]]+)|\[(?<index>-?\d+)\]",
        RegexOptions.CultureInvariant
    );

    internal static ExecutionExpression? TryCreate(
        ColumnRef column,
        string alias,
        IReadOnlyDictionary<string, RowShape> shapes
    )
    {
        if (!shapes.TryGetValue(alias, out var source))
            return null;
        if (
            source is SourceEntityShape { ElementInterpretationShape: { } element }
            && (
                string.IsNullOrEmpty(column.Alias)
                || string.Equals(column.Alias, alias, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            var receiver = new ExecutionFieldRead(alias, alias, element.Type, new DirectScalarValueAccess())
            {
                InterpretationShape = element,
            };
            return ReadPath(column.ColumnName, column, receiver, element);
        }
        var root = BoundNestedFieldPath.Resolve(column, source, alias);
        if (root?.Field.InterpretationShape is not { } shape)
            return null;
        var field = root.Field;
        ExecutionExpression value = new ExecutionFieldRead(
            alias,
            field.Name,
            field.Type,
            field.AccessStrategy,
            field.GeneratedTypeName
        )
        {
            InterpretationShape = shape,
            Stability = field.Stability,
            SourceReadType = field.SourceReadType,
        };
        return ReadPath(root.PropertyPath, column, value, shape);
    }

    private static ExecutionExpression? ReadPath(
        string path,
        ColumnRef column,
        ExecutionExpression value,
        ExecutionInterpretationShape shape
    )
    {
        foreach (Match segment in Segments.Matches(path))
        {
            if (segment.Groups["index"].Success)
            {
                if (shape.Element is not { } element)
                    return null;
                value = new ExecutionArrayAccess(
                    value,
                    new ExecutionLiteral(
                        int.Parse(segment.Groups["index"].Value, CultureInfo.InvariantCulture),
                        typeof(int)
                    ),
                    element.Type,
                    element.Type
                )
                {
                    InterpretationShape = element,
                };
                shape = element;
                continue;
            }
            var name = segment.Groups["member"].Value;
            var member = shape.Member(name);
            if (member == null)
            {
                var receiverType = shape.Type.ResolveClrType();
                if (
                    segment.Index + segment.Length == path.Length
                    && (receiverType == typeof(string) || receiverType.IsArray)
                )
                    return new ExecutionMemberRead(value, name, column.ReturnType, false)
                    {
                        IsNullSafe = column.IsNullSafePath,
                    };
                return null;
            }
            value = ReadMember(value, name, shape, member, true);
            shape = member;
        }
        return value;
    }

    internal static ExecutionExpression ReadMember(
        ExecutionExpression value,
        string name,
        ExecutionInterpretationShape receiver,
        ExecutionInterpretationShape member,
        bool isNullSafe
    ) =>
        receiver.Alternatives.Count == 0
            ? new ExecutionMemberRead(value, name, member.Type, false)
            {
                IsNullSafe = isNullSafe,
                InterpretationShape = member,
            }
            : new ExecutionUnionMemberRead(value, name, member.Type, Cases(receiver, name))
            {
                InterpretationShape = member,
            };

    private static ExecutionUnionMemberCase[] Cases(ExecutionInterpretationShape shape, string name) =>
        shape
            .Alternatives.Where(arm => arm.TypeName != null && arm.Member(name) != null)
            .Select(arm => new ExecutionUnionMemberCase(arm.TypeName!, name))
            .Distinct()
            .ToArray();
}
