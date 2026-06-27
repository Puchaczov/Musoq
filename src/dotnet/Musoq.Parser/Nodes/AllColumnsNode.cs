using System.Linq;

namespace Musoq.Parser.Nodes;

public class AllColumnsNode(
    string? alias,
    string? likePattern,
    bool isNotLike,
    string[]? excludeColumns,
    StarReplaceItemNode[]? replaceItems,
    StarRenameItemNode[]? renameItems)
    : Node
{
    public AllColumnsNode(string? alias = null) : this(alias, null, false, null, null, null)
    {
    }

    public string? Alias { get; } = alias;

    public string? LikePattern { get; } = likePattern;

    public bool IsNotLike { get; } = isNotLike;

    public string[]? ExcludeColumns { get; } = excludeColumns;

    public StarReplaceItemNode[]? ReplaceItems { get; } = replaceItems;

    public StarRenameItemNode[]? RenameItems { get; } = renameItems;

    public bool HasModifiers =>
        LikePattern != null || ExcludeColumns is { Length: > 0 } ||
        ReplaceItems is { Length: > 0 } || RenameItems is { Length: > 0 };

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
            var renamePart = RenameItems is { Length: > 0 }
                ? $"Rename({string.Join(",", RenameItems.Select(r => $"{r.SourceName}->{r.TargetName}"))})"
                : string.Empty;

            return $"{baseId}{likePart}{excludePart}{replacePart}{renamePart}";
        }
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
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
        if (RenameItems is { Length: > 0 })
            parts += $" rename ({string.Join(", ", RenameItems.Select(r => r.ToString()))})";

        return parts;
    }
}
