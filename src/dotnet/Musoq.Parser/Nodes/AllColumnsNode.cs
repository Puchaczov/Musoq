using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class AllColumnsNode : Node
{
    public AllColumnsNode(string alias = null)
    {
        Alias = alias;
    }

    public AllColumnsNode(
        string alias,
        string likePattern,
        bool isNotLike,
        string[] excludeColumns,
        StarReplaceItemNode[] replaceItems)
    {
        Alias = alias;
        LikePattern = likePattern;
        IsNotLike = isNotLike;
        ExcludeColumns = excludeColumns;
        ReplaceItems = replaceItems;
    }

    public string Alias { get; }

    public string LikePattern { get; }

    public bool IsNotLike { get; }

    public string[] ExcludeColumns { get; }

    public StarReplaceItemNode[] ReplaceItems { get; }

    public bool HasModifiers =>
        LikePattern != null || ExcludeColumns is { Length: > 0 } || ReplaceItems is { Length: > 0 };

    public override Type ReturnType => typeof(object[]);

    public override string Id
    {
        get
        {
            var baseId = $"{nameof(AllColumnsNode)}{Alias ?? string.Empty}*";
            if (!HasModifiers)
                return baseId;

            var likeDirection = IsNotLike ? "NotLike" : "Like";
            var likePart = LikePattern != null ? $"{likeDirection}{LikePattern}" : string.Empty;
            var excludePart = ExcludeColumns is { Length: > 0 }
                ? $"Exclude({string.Join(",", ExcludeColumns)})"
                : string.Empty;
            var replacePart = ReplaceItems is { Length: > 0 }
                ? $"Replace({string.Join(",", ReplaceItems.Select(r => r.ColumnName))})"
                : string.Empty;

            return $"{baseId}{likePart}{excludePart}{replacePart}";
        }
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var star = !string.IsNullOrWhiteSpace(Alias) ? $"{Alias}.*" : "*";

        if (!HasModifiers)
            return star;

        var parts = star;
        if (LikePattern != null)
            parts += IsNotLike ? $" not like '{LikePattern}'" : $" like '{LikePattern}'";
        if (ExcludeColumns is { Length: > 0 })
            parts += $" exclude ({string.Join(", ", ExcludeColumns)})";
        if (ReplaceItems is { Length: > 0 })
            parts += $" replace ({string.Join(", ", ReplaceItems.Select(r => r.ToString()))})";

        return parts;
    }
}
