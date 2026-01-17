using System;

namespace Musoq.Parser.Nodes.From;

public class AccessMethodFromNode : FromNode
{
    public AccessMethodFromNode(string alias, string sourceAlias, AccessMethodNode accessMethod)
        : base(alias)
    {
        SourceAlias = sourceAlias;
        AccessMethod = accessMethod;
    }

    public AccessMethodFromNode(string alias, string sourceAlias, AccessMethodNode accessMethod, Type returnType)
        : base(alias, returnType)
    {
        SourceAlias = sourceAlias;
        AccessMethod = accessMethod;
    }

    public string SourceAlias { get; }

    public AccessMethodNode AccessMethod { get; }

    public override string Id => $"{nameof(AccessMethodFromNode)}{Alias}{AccessMethod.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Alias))
            return $"{AccessMethod.ToString()} {Alias}";

        return $"{AccessMethod}";
    }
}