using System;
using System.Collections.Generic;
using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class AccessMethodNode : Node
{
    public AccessMethodNode(FunctionToken functionToken, ArgsListNode args, ArgsListNode extraAggregateArguments, bool canSkipInjectSource,
        MethodInfo method = null, string alias = "")
    {
        FunctionToken = functionToken;
        Arguments = args;
        ExtraAggregateArguments = extraAggregateArguments;
        CanSkipInjectSource = canSkipInjectSource;
        Method = method;
        Alias = alias;
        Id = $"{nameof(AccessMethodNode)}{alias}{functionToken.Value}{args.Id}";
    }
    
    public FunctionToken FunctionToken { get; }

    public bool CanSkipInjectSource { get; }

    public MethodInfo Method { get; private set; }

    public ArgsListNode Arguments { get; }

    public string Name => FunctionToken.Value;

    public string Alias { get; }

    public ArgsListNode ExtraAggregateArguments { get; }

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

        int paramIndex = 0;
        var types = new List<Type>();

        foreach (var param in Method.GetParameters())
        {
            if (param.ParameterType.IsGenericParameter && Method.ReturnType == param.ParameterType)
            {
                types.Add(Arguments.Args[paramIndex].ReturnType);
            }
            paramIndex += 1;
        }

        return GetTheMostCommonBaseTypes(types.ToArray());
    }

    private Type GetTheMostCommonBaseTypes(Type[] types)
    {
        if (types.Length == 0)
            return typeof(object);

        Type ret = types[0];

        for (int i = 1; i < types.Length; ++i)
        {
            if (types[i].IsAssignableFrom(ret))
                ret = types[i];
            else
            {
                // This will always terminate when ret == typeof(object)
                while (!ret.IsAssignableFrom(types[i]))
                    ret = ret.BaseType;
            }
        }

        return ret;
    }

    public void ChangeMethod(MethodInfo method)
    {
        Method = method;
    }

    public override string ToString()
    {
        return ArgsCount > 0 ? $"{Name}({Arguments.ToString()})" : $"{Name}()";
    }
}