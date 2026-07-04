using System.Collections.Generic;
using System.Reflection;
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
                return identifier;
        }

        if (!IsAggregateMethod(methodCall.Method))
            return null;

        return AggregateCallIdentity.Create(methodCall);
    }

    public static string? ExtractDisplayName(MethodCall methodCall)
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        foreach (var argument in methodCall.Arguments)
        {
            if (argument is Literal { DisplayName.Length: > 0 } literal)
                return literal.DisplayName;
        }

        return null;
    }

    public static string? NormalizeIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return identifier;

        var builder = new System.Text.StringBuilder(identifier.Length);
        foreach (var character in identifier)
            if (!char.IsWhiteSpace(character))
                builder.Append(character);

        return builder.Length == identifier.Length ? identifier : builder.ToString();
    }
}
