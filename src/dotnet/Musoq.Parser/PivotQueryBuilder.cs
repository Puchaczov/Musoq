using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal static class PivotQueryBuilder
{
    public static SelectNode BuildSelectNode(Node[] keys, PivotValue[] values, FieldNode[] measures, GroupByNode? groupBy, string queryPart)
    {
        var fields = new List<FieldNode>();
        var outputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (groupBy != null)
            foreach (var groupField in groupBy.Fields)
            {
                AddOutputName(outputNames, groupField.FieldName, groupField.Span, queryPart);
                fields.Add(new FieldNode(groupField.Expression, fields.Count, groupField.HasExplicitFieldName ? groupField.FieldName : null, groupField.HasExplicitFieldName, groupField.Span));
            }

        foreach (var measure in measures)
            if (measure.Expression is not AccessMethodNode)
                throw new SyntaxException("PIVOT USING accepts aggregate function calls only. Use a form like USING Sum(Amount) as Sales.",
                    queryPart, DiagnosticCode.MQ2003_InvalidExpression, measure.Span);

        foreach (var value in values)
        foreach (var measure in measures)
        {
            var method = (AccessMethodNode)measure.Expression;
            var outputName = CreateOutputName(value, measure, method, measures.Length == 1);
            AddOutputName(outputNames, outputName, measure.Span, queryPart);
            fields.Add(new FieldNode(CreateFilteredMeasure(keys, value.Expressions, method), fields.Count, outputName, true, measure.Span));
        }

        return new SelectNode(fields.ToArray());
    }

    public static bool IsSupportedValueExpression(Node expression) => expression is ConstantValueNode or NullNode;

    public static string CreateValueAlias(Node[] expressions)
    {
        return string.Join("_", expressions.Select(CreateValueAlias));
    }

    private static string CreateValueAlias(Node expression)
    {
        return expression switch
        {
            WordNode word => word.Value,
            NullNode => "null",
            ConstantValueNode constant => FormatConstantAlias(constant.ObjValue),
            _ => expression.ToString()
        };
    }

    public static bool IsMeasureTerminator(TokenType tokenType)
    {
        return tokenType is TokenType.GroupBy or TokenType.OrderBy or TokenType.Skip or TokenType.Take
            or TokenType.RightParenthesis or TokenType.Semicolon or TokenType.EndOfFile
            or TokenType.Union or TokenType.UnionAll or TokenType.Except or TokenType.Intersect;
    }

    private static AccessMethodNode CreateFilteredMeasure(Node[] keys, Node[] values, AccessMethodNode method)
    {
        var arguments = GetAggregateArguments(method);
        var wrappedArguments = new Node[arguments.Length];
        var predicate = CreatePivotPredicate(keys, values);

        for (var index = 0; index < arguments.Length; index += 1)
        {
            var thenExpression = arguments[index] is AllColumnsNode ? new IntegerNode(1) : arguments[index];
            wrappedArguments[index] = new CaseNode(
                [(new WhenNode(predicate), new ThenNode(thenExpression))],
                new ElseNode(new NullNode()));
        }

        return new AccessMethodNode(method.FunctionToken, new ArgsListNode(wrappedArguments), method.ExtraAggregateArguments,
            method.CanSkipInjectSource, null, method.Alias, method.Span, method.IsDistinct)
        {
            HasFilter = true,
            IsPivotGenerated = true
        };
    }

    private static Node CreatePivotPredicate(Node[] keys, Node[] values)
    {
        Node predicate = CreatePivotComparison(keys[0], values[0]);

        for (var index = 1; index < keys.Length; index += 1)
            predicate = new AndNode(predicate, CreatePivotComparison(keys[index], values[index]));

        return predicate;
    }

    private static Node CreatePivotComparison(Node key, Node value)
    {
        return value is NullNode
            ? new IsNullNode(key, false)
            : new EqualityNode(key, value);
    }

    private static Node[] GetAggregateArguments(AccessMethodNode method)
    {
        return method.Arguments.Args.Length == 0 && method.Name.Equals("count", StringComparison.OrdinalIgnoreCase)
            ? [new IntegerNode(1)]
            : method.Arguments.Args;
    }

    private static void AddOutputName(HashSet<string> outputNames, string outputName, TextSpan span, string queryPart)
    {
        if (outputNames.Add(outputName))
            return;

        throw new SyntaxException($"PIVOT generated duplicate output column name '{outputName}'. Use unique pivot value aliases or measure aliases.",
            queryPart, DiagnosticCode.MQ2008_DuplicateAlias, span);
    }

    private static string CreateOutputName(PivotValue value, FieldNode measure, AccessMethodNode method, bool hasSingleMeasure)
    {
        return hasSingleMeasure ? value.Alias : $"{value.Alias}_{(measure.HasExplicitFieldName ? measure.FieldName : method.Name)}";
    }

    private static string FormatConstantAlias(object value)
    {
        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }
}
