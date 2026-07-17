namespace Musoq.Parser.Nodes;

internal static class JoinTypeSql
{
    public static string GetKeyword(JoinType joinType)
    {
        return joinType switch
        {
            JoinType.Inner => "inner join",
            JoinType.OuterLeft => "left outer join",
            JoinType.OuterRight => "right outer join",
            JoinType.OuterFull => "full outer join",
            JoinType.AsOf => "asof join",
            JoinType.AsOfLeft => "asof left join",
            JoinType.Cross => "cross join",
            JoinType.LeftSemi => "left semi join",
            JoinType.LeftAntiSemi => "left anti semi join",
            JoinType.LeftMark => "left mark join",
            JoinType.LeftSingle => "left single join",
            _ => throw new ArgumentOutOfRangeException(nameof(joinType), joinType, "Unsupported join type.")
        };
    }

    public static bool PrintsCondition(JoinType joinType)
    {
        return joinType != JoinType.Cross;
    }
}
