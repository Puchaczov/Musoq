using System.Collections.Generic;
using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class AccessMethodNode : Node
{
    public AccessMethodNode(FunctionToken functionToken, ArgsListNode args, ArgsListNode? extraAggregateArguments,
        bool canSkipInjectSource,
        MethodInfo? method = null, string alias = "", bool isDistinct = false)
        : this(functionToken, args, extraAggregateArguments, canSkipInjectSource, method, alias, default, isDistinct)
    {
    }

    public AccessMethodNode(FunctionToken functionToken, ArgsListNode args, ArgsListNode? extraAggregateArguments,
        bool canSkipInjectSource,
        MethodInfo? method, string alias, TextSpan span, bool isDistinct = false)
    {
        ArgumentNullException.ThrowIfNull(args);
        FunctionToken = functionToken;
        Arguments = args;
        ExtraAggregateArguments = extraAggregateArguments;
        CanSkipInjectSource = canSkipInjectSource;
        Method = method;
        Alias = alias;
        IsDistinct = isDistinct;
        TypeParameter = (functionToken as GenericFunctionToken)?.TypeParameter;
        var typeParameterSuffix = CreateTypeParameterSuffix(TypeParameter);
        Id = $"{nameof(AccessMethodNode)}{alias}{functionToken.Value}{typeParameterSuffix}{args.Id}{(isDistinct ? "Distinct" : "")}";

        // If no explicit span provided, try to compute from function token and args
        if (span.IsEmpty && functionToken != null)
        {
            // The span should ideally cover from function name to closing paren
            // For now, use the function token span as the base
            Span = functionToken.Span;
            FullSpan = functionToken.Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public FunctionToken FunctionToken { get; }

    public bool CanSkipInjectSource { get; }

    public MethodInfo? Method { get; private set; }

    public ArgsListNode Arguments { get; }

    public string Name => FunctionToken.Value;

    public string? TypeParameter { get; }

    public string Alias { get; }

    public ArgsListNode? ExtraAggregateArguments { get; }

    /// <summary>
    ///     Indicates whether this aggregate function should operate on distinct values only.
    ///     Used with aggregate functions like COUNT(DISTINCT column).
    /// </summary>
    public bool IsDistinct { get; }

    public bool IsAggregate { get; private set; }

    /// <summary>
    ///     Indicates whether a FILTER clause was applied to this function call.
    ///     Used to validate that FILTER is only applied to aggregate functions.
    /// </summary>
    public bool HasFilter { get; set; }

    public bool IsPivotGenerated { get; set; }

    public int ArgsCount => Arguments.Args.Length;

    public override Type ReturnType => Method != null ? ResolveGenericMethodReturnType() : typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    private Type ResolveGenericMethodReturnType()
    {
        var method = Method ?? throw new InvalidOperationException("Cannot resolve return type before method binding.");

        if (!method.ReturnType.IsGenericParameter)
            return method.ReturnType;

        var paramIndex = 0;
        var types = new List<Type>();

        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType.IsGenericParameter && method.ReturnType == param.ParameterType)
            {
                var argumentReturnType = Arguments.Args[paramIndex].ReturnType;
                if (argumentReturnType != null)
                    types.Add(argumentReturnType);
            }
            paramIndex += 1;
        }

        return GetTheMostCommonBaseTypes(types.ToArray());
    }

    public void ChangeMethod(MethodInfo method)
    {
        Method = method;
    }

    public void MarkAsAggregate()
    {
        IsAggregate = true;
    }

    public override string ToString()
    {
        var alias = !string.IsNullOrWhiteSpace(Alias) ? $"{Alias}." : string.Empty;
        var typeParameterSuffix = CreateTypeParameterSuffix(TypeParameter);
        return ArgsCount > 0 ? $"{alias}{Name}{typeParameterSuffix}({Arguments.ToString()})" : $"{alias}{Name}{typeParameterSuffix}()";
    }

    private static string CreateTypeParameterSuffix(string? typeParameter)
    {
        return string.IsNullOrWhiteSpace(typeParameter) ? string.Empty : $"<{typeParameter}>";
    }

    private static Type GetTheMostCommonBaseTypes(Type[] types)
    {
        if (types.Length == 0)
            return typeof(object);

        var returnType = types[0];

        for (var i = 1; i < types.Length; ++i)
            if (types[i].IsAssignableFrom(returnType))
                returnType = types[i];
            else
                while (returnType is not null && !returnType.IsAssignableFrom(types[i]))
                    returnType = returnType.BaseType;

        return returnType ?? typeof(object);
    }
}
