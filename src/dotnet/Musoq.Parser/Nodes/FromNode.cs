#nullable enable
using System;

namespace Musoq.Parser.Nodes;

public abstract class FromNode : Node
{
    private readonly Type? _returnType;

    protected FromNode(string alias)
        : this(alias, null, default)
    {
    }

    protected FromNode(string alias, Type? returnType)
        : this(alias, returnType, default)
    {
    }

    protected FromNode(string alias, Type? returnType, TextSpan span)
    {
        Alias = alias;
        _returnType = returnType;
        Span = span;
        FullSpan = span;
    }

    public virtual string Alias { get; }

    // ReSharper disable once ConvertToAutoProperty
    public override Type? ReturnType => _returnType;
}
