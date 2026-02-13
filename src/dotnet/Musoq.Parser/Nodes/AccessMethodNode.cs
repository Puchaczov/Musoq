using System;
using System.Collections.Generic;
using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class AccessMethodNode : Node
{
    public AccessMethodNode(FunctionToken functionToken, ArgsListNode args, ArgsListNode extraAggregateArguments,
        bool canSkipInjectSource,
        MethodInfo method = null, string alias = "", bool isDistinct = false)
        : this(functionToken, args, extraAggregateArguments, canSkipInjectSource, method, alias, default, isDistinct)
    {
    }

    public AccessMethodNode(FunctionToken functionToken, ArgsListNode args, ArgsListNode extraAggregateArguments,
        bool canSkipInjectSource,
        MethodInfo method, string alias, TextSpan span, bool isDistinct = false)
    {
        FunctionToken = functionToken;
        Arguments = args;
        ExtraAggregateArguments = extraAggregateArguments;
        CanSkipInjectSource = canSkipInjectSource;
        Method = method;
        Alias = alias;
        IsDistinct = isDistinct;
        Id = $"{nameof(AccessMethodNode)}{alias}{functionToken.Value}{args.Id}{(isDistinct ? "Distinct" : "")}";

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

    public MethodInfo Method { get; private set; }

    public ArgsListNode Arguments { get; }

    public string Name => FunctionToken.Value;

    public string Alias { get; }

    public ArgsListNode ExtraAggregateArguments { get; }

    /// <summary>
    /// Indicates whether this aggregate function should operate on distinct values only.
    /// Used with aggregate functions like COUNT(DISTINCT column).
    /// </summary>
    public bool IsDistinct { get; }

    public int ArgsCount => Arguments.Args.Length;

    public override Type ReturnType => Method != null ? ResolveGenericMethodReturnType() : typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    private Type ResolveGenericMethodReturnType()
    {
        if (!Method.ReturnType.IsGenericParameter)
            return Method.ReturnType;

        var paramIndex = 0;
        var types = new List<Type>();

        foreach (var param in Method.GetParameters())
        {
            if (param.ParameterType.IsGenericParameter && Method.ReturnType == param.ParameterType)
                types.Add(Arguments.Args[paramIndex].ReturnType);
            paramIndex += 1;
        }

        return GetTheMostCommonBaseTypes(types.ToArray());
    }

    public void ChangeMethod(MethodInfo method)
    {
        Method = method;
    }

    public override string ToString()
    {
        var alias = !string.IsNullOrWhiteSpace(Alias) ? $"{Alias}." : string.Empty;
        return ArgsCount > 0 ? $"{alias}{Name}({Arguments.ToString()})" : $"{alias}{Name}()";
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

        return returnType;
    }
}
