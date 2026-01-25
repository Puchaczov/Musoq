using System;
using System.Linq;

namespace Musoq.Parser.Nodes.From;

public class PropertyFromNode : FromNode
{
    public PropertyFromNode(string alias, string sourceAlias, string[] properties)
        : base(alias)
    {
        SourceAlias = sourceAlias;
        PropertiesChain = properties.Select(f => new PropertyNameAndTypePair(f, null)).ToArray();
    }

    protected PropertyFromNode(string alias, string sourceAlias, PropertyNameAndTypePair[] properties)
        : base(alias, properties.Last().PropertyType)
    {
        SourceAlias = sourceAlias;
        PropertiesChain = properties;
    }

    public string SourceAlias { get; }

    public PropertyNameAndTypePair[] PropertiesChain { get; }

    public PropertyNameAndTypePair FirstProperty => PropertiesChain.First();

    public override string Id => $"{nameof(PropertyFromNode)}{Alias}{SourceAlias}{ToIdString(PropertiesChain)}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return !string.IsNullOrEmpty(Alias)
            ? $"{SourceAlias}.{ToPropertiesString(PropertiesChain)} {Alias}"
            : $"{SourceAlias}.{ToPropertiesString(PropertiesChain)}";
    }

    private static string ToIdString(PropertyNameAndTypePair[] propertiesChain)
    {
        return string.Join("", propertiesChain.Select(f => $"{f.PropertyName},{f.PropertyType?.Name ?? string.Empty}"));
    }

    private static string ToPropertiesString(PropertyNameAndTypePair[] propertiesChain)
    {
        return string.Join(".", propertiesChain.Select(f => f.PropertyName));
    }

    public record PropertyNameAndTypePair(string PropertyName, Type PropertyType);
}
