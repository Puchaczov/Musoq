using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal static class SqlKeywordTokenFacts
{
    public static bool CanRepresentQualifiedIdentifier(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.And or TokenType.Or or TokenType.Not or
                TokenType.Where or TokenType.Select or TokenType.From or
                TokenType.Pivot or TokenType.Unpivot or
                TokenType.Like or TokenType.NotLike or TokenType.RLike or TokenType.NotRLike or
                TokenType.As or TokenType.Is or TokenType.Null or
                TokenType.Union or TokenType.UnionAll or TokenType.Except or TokenType.Intersect or
                TokenType.GroupBy or TokenType.Having or TokenType.Contains or
                TokenType.Skip or TokenType.Take or TokenType.With or
                TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossApply or TokenType.OuterApply or
                TokenType.On or TokenType.OrderBy or TokenType.Asc or TokenType.Desc or
                TokenType.Functions or TokenType.True or TokenType.False or
                TokenType.In or TokenType.Exists or TokenType.Any or TokenType.Some or TokenType.All or
                TokenType.NotIn or TokenType.Table or TokenType.Couple or
                TokenType.Case or TokenType.When or TokenType.Then or TokenType.Else or
                TokenType.Distinct or TokenType.ColumnKeyword or TokenType.Between => true,
            _ => false
        };
    }

    public static bool IsContextualExpressionIdentifier(TokenType tokenType)
    {
        return tokenType is TokenType.Exists or TokenType.Any or TokenType.Some or TokenType.All;
    }
}
