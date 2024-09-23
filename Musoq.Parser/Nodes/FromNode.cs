#nullable enable
using System;

namespace Musoq.Parser.Nodes;

public abstract class FromNode : Node
{
    private readonly Type? _returnType;
        
    protected FromNode(string alias)
    {
        Alias = alias;
        _returnType = null;
    }
        
    protected FromNode(string alias, Type returnType)
    {
        Alias = alias;
        _returnType = returnType;
    }

    public virtual string Alias { get; }

    // ReSharper disable once ConvertToAutoProperty
    public override Type? ReturnType => _returnType;
}