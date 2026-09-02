using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes;

/// <summary>
///     Declares a query-local nominal enum type.
/// </summary>
public sealed class EnumDeclarationNode : Node
{
    public EnumDeclarationNode(
        string name,
        string underlyingTypeName,
        bool isFlags,
        IReadOnlyList<EnumMemberNode> members,
        TextSpan nameSpan,
        TextSpan underlyingTypeSpan,
        TextSpan span)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(underlyingTypeName);
        ArgumentNullException.ThrowIfNull(members);

        Name = name;
        UnderlyingTypeName = underlyingTypeName;
        IsFlags = isFlags;
        Members = members.ToArray();
        NameSpan = nameSpan;
        UnderlyingTypeSpan = underlyingTypeSpan;
        Span = span;
        FullSpan = span;
        Id = $"{nameof(EnumDeclarationNode)}{Name}{UnderlyingTypeName}{IsFlags}{string.Join(string.Empty, Members.Select(static member => member.Id))}";
    }

    public string Name { get; }

    public string UnderlyingTypeName { get; }

    public bool IsFlags { get; }

    public IReadOnlyList<EnumMemberNode> Members { get; }

    public TextSpan NameSpan { get; }

    public TextSpan UnderlyingTypeSpan { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        if (IsFlags)
            builder.Append("flags ");

        builder.Append("enum ");
        builder.Append(Name);
        builder.Append(" : ");
        builder.Append(UnderlyingTypeName);
        builder.Append(" { ");

        for (var index = 0; index < Members.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append(Members[index].ToString());
        }

        builder.Append(" };");
        return builder.ToString();
    }
}
