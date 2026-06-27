using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AggregateRefRewriter : ExpressionArrayRewriter
{
    private readonly Dictionary<string, AggregateBinding> _bindingsByIdentifier;

    private AggregateRefRewriter(Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        _bindingsByIdentifier = bindingsByIdentifier;
    }

    public static IrExpression Rewrite(
        IrExpression expression,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(bindingsByIdentifier);
        if (bindingsByIdentifier.Count == 0)
            return expression;

        var rewriter = new AggregateRefRewriter(bindingsByIdentifier);
        return rewriter.Visit(expression);
    }

    public static bool IsAggregateMethod(MethodInfo method)
    {
        return method.GetCustomAttribute<AggregateFunctionAttribute>() is not null;
    }

    public static string? ExtractIdentifier(MethodCall methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        foreach (var argument in methodCall.Arguments)
        {
            if (argument is Literal { Value: string identifier })
                return NormalizeIdentifier(identifier);
        }

        if (!IsAggregateMethod(methodCall.Method))
            return null;

        var parts = new List<string>(methodCall.Arguments.Count);

        foreach (var argument in methodCall.Arguments)
        {
            switch (argument)
            {
                case ColumnRef columnRef:
                    parts.Add(ExtractUnqualifiedColumnName(columnRef.ColumnName));
                    break;
                case WildcardLiteral:
                    parts.Add("*");
                    break;
                case Literal { Value: null }:
                    parts.Add("null");
                    break;
                case Literal literal:
                    parts.Add(literal.Value?.ToString() ?? "null");
                    break;
                default:
                    parts.Add(IrExpressionPrinter.Print(argument));
                    break;
            }
        }

        return NormalizeIdentifier($"{methodCall.Method.Name}({string.Join(", ", parts)})");
    }

    public static string? NormalizeIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        var withoutQualifier = Regex.Replace(identifier, @"\b[A-Za-z_][A-Za-z0-9_]*\.", string.Empty);
        return Regex.Replace(withoutQualifier, @"\s+", string.Empty);
    }

    private static string ExtractUnqualifiedColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return columnName;

        var lastDot = columnName.LastIndexOf('.');
        if (lastDot < 0 || lastDot == columnName.Length - 1)
            return columnName;

        return columnName[(lastDot + 1)..];
    }
}
