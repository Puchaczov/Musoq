using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class InterpretationMemberBinding
{
    private static readonly Regex Segments = new(@"(?<member>[^.\[\]]+)|\[(?<index>-?\d+)\]", RegexOptions.CultureInvariant);

    internal static ExecutionExpression? TryCreate(ColumnRef column, string alias, IReadOnlyDictionary<string, RowShape> shapes)
    {
        if (!shapes.TryGetValue(alias, out var source)) return null;
        var root = BoundNestedFieldPath.Resolve(column, source, alias);
        if (root?.Field.InterpretationShape is not { } shape) return null;
        var field = root.Field;
        ExecutionExpression value = new ExecutionFieldRead(alias, field.Name, field.Type, field.AccessStrategy, field.GeneratedTypeName)
        { InterpretationShape = shape, Stability = field.Stability, SourceReadType = field.SourceReadType };
        foreach (Match segment in Segments.Matches(root.PropertyPath))
        {
            if (segment.Groups["index"].Success)
            {
                if (shape.Element is not { } element) return null;
                value = new ExecutionArrayAccess(value, new ExecutionLiteral(int.Parse(segment.Groups["index"].Value, CultureInfo.InvariantCulture), typeof(int)), element.Type, element.Type)
                { InterpretationShape = element };
                shape = element;
                continue;
            }
            var name = segment.Groups["member"].Value;
            var member = shape.Member(name);
            if (member == null) return null;
            value = shape.Alternatives.Count == 0
                ? new ExecutionMemberRead(value, name, member.Type, false) { IsNullSafe = true, InterpretationShape = member }
                : new ExecutionUnionMemberRead(value, name, member.Type, Cases(shape, name)) { InterpretationShape = member };
            shape = member;
        }
        return value;
    }

    private static ExecutionUnionMemberCase[] Cases(ExecutionInterpretationShape shape, string name) =>
        shape.Alternatives.Where(arm => arm.TypeName != null && arm.Member(name) != null)
            .Select(arm => new ExecutionUnionMemberCase(arm.TypeName!, name)).Distinct().ToArray();
}
