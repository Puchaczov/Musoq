using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private static Token CreateKeywordToken(TokenType keywordType, TextSpan span)
    {
        return keywordType switch
        {
            TokenType.Desc => new DescToken(span),
            TokenType.Asc => new AscToken(span),
            TokenType.And => new AndToken(span),
            TokenType.Or => new OrToken(span),
            TokenType.Not => new NotToken(span),
            TokenType.Where => new WhereToken(span),
            TokenType.Select => new SelectToken(span),
            TokenType.From => new FromToken(span),
            TokenType.Pivot => new PivotToken(span),
            TokenType.Unpivot => new UnpivotToken(span),
            TokenType.Like => new LikeToken(span),
            TokenType.RLike => new RLikeToken(span),
            TokenType.As => new AsToken(span),
            TokenType.Is => new IsToken(span),
            TokenType.Null => new NullToken(span),
            TokenType.Present => new PresentToken(span),
            TokenType.Missing => new MissingToken(span),
            TokenType.Union => new UnionToken(span),
            TokenType.Except => new ExceptToken(span),
            TokenType.Intersect => new IntersectToken(span),
            TokenType.Having => new HavingToken(span),
            TokenType.Contains => new ContainsToken(span),
            TokenType.Skip => new SkipToken(SkipToken.TokenText, span),
            TokenType.Take => new TakeToken(TakeToken.TokenText, span),
            TokenType.With => new WithToken(span),
            TokenType.On => new OnToken(span),
            TokenType.Functions => new FunctionsToken(span),
            TokenType.True => new TrueToken(span),
            TokenType.False => new FalseToken(span),
            TokenType.In => new InToken(span),
            TokenType.Exists => new ExistsToken(span),
            TokenType.Any => new AnyToken(span),
            TokenType.Some => new SomeToken(span),
            TokenType.All => new AllToken(span),
            TokenType.Table => new TableToken(span),
            TokenType.Couple => new CoupleToken(span),
            TokenType.Case => new CaseToken(span),
            TokenType.When => new WhenToken(span),
            TokenType.Then => new ThenToken(span),
            TokenType.Else => new ElseToken(span),
            TokenType.End => new EndToken(span),
            TokenType.Distinct => new DistinctToken(span),
            TokenType.Between => new BetweenToken(span),
            TokenType.Over => new OverToken(span),
            TokenType.Window => new WindowToken(span),
            _ => new WordToken(keywordType.ToString(), span)
        };
    }

    private Token? TryMatchMultiWordKeyword()
    {
        var start = Position;

        return char.ToLowerInvariant(Input[Position]) switch
        {
            'n' => TryMatchTwoWords("not", "in", PhraseBoundary.WhitespaceOrEnd, out var end)
                ? new NotInToken(new TextSpan(start, end - start))
                : TryMatchTwoWords("not", "like", PhraseBoundary.WhitespaceOrEnd, out end)
                    ? new NotLikeToken(new TextSpan(start, end - start))
                    : TryMatchTwoWords("not", "rlike", PhraseBoundary.WhitespaceOrEnd, out end)
                        ? new NotRLikeToken(new TextSpan(start, end - start))
                        : null,
            'u' => TryMatchTwoWords("union", "all", PhraseBoundary.WhitespaceOrEnd, out var unionEnd)
                ? new UnionAllToken(new TextSpan(start, unionEnd - start))
                : null,
            'g' => TryMatchTwoWords("group", "by", PhraseBoundary.WhitespaceOrEnd, out var groupEnd)
                ? new GroupByToken(new TextSpan(start, groupEnd - start))
                : null,
            'o' => TryMatchTwoWords("order", "by", PhraseBoundary.WhitespaceOrEnd, out var orderEnd)
                ? new OrderByToken(new TextSpan(start, orderEnd - start))
                : TryMatchTwoWords("outer", "apply", PhraseBoundary.WhitespaceOrEnd, out orderEnd)
                    ? new OuterApplyToken(new TextSpan(start, orderEnd - start))
                    : null,
            'p' => TryMatchTwoWords("partition", "by", PhraseBoundary.WhitespaceOrEnd, out var partitionEnd)
                ? new PartitionByToken(new TextSpan(start, partitionEnd - start))
                : null,
            'j' or 'i' => TryMatchTwoWords("inner", "join", PhraseBoundary.WordBoundary, out var innerEnd)
                ? new InnerJoinToken(new TextSpan(start, innerEnd - start))
                : TryMatchWord("join", PhraseBoundary.WordBoundary, out innerEnd)
                    ? new InnerJoinToken(new TextSpan(start, innerEnd - start))
                    : null,
            'l' => TryMatchFourWords("left", "anti", "semi", "join", PhraseBoundary.WordBoundary, out var leftEnd)
                ? new AntiJoinToken(new TextSpan(start, leftEnd - start))
                : TryMatchThreeWords("left", "semi", "join", PhraseBoundary.WordBoundary, out leftEnd)
                    ? new SemiJoinToken(new TextSpan(start, leftEnd - start))
                    : TryMatchThreeWords("left", "outer", "join", PhraseBoundary.WordBoundary, out leftEnd)
                        ? new OuterJoinToken(OuterJoinType.Left, new TextSpan(start, leftEnd - start))
                        : TryMatchTwoWords("left", "join", PhraseBoundary.WordBoundary, out leftEnd)
                            ? new OuterJoinToken(OuterJoinType.Left, new TextSpan(start, leftEnd - start))
                            : null,
            'r' => TryMatchThreeWords("right", "outer", "join", PhraseBoundary.WordBoundary, out var rightEnd)
                ? new OuterJoinToken(OuterJoinType.Right, new TextSpan(start, rightEnd - start))
                : TryMatchTwoWords("right", "join", PhraseBoundary.WordBoundary, out rightEnd)
                    ? new OuterJoinToken(OuterJoinType.Right, new TextSpan(start, rightEnd - start))
                    : null,
            'c' => TryMatchTwoWords("current", "row", PhraseBoundary.WhitespaceRightParenOrEnd, out var currentEnd)
                ? new CurrentRowToken(new TextSpan(start, currentEnd - start))
                : TryMatchTwoWords("cross", "apply", PhraseBoundary.WhitespaceOrEnd, out currentEnd)
                    ? new CrossApplyToken(new TextSpan(start, currentEnd - start))
                    : TryMatchTwoWords("cross", "join", PhraseBoundary.WordBoundary, out currentEnd)
                        ? new CrossJoinToken(new TextSpan(start, currentEnd - start))
                        : null,
            'f' => TryMatchThreeWords("full", "outer", "join", PhraseBoundary.WordBoundary, out var fullEnd)
                ? new OuterJoinToken(OuterJoinType.Full, new TextSpan(start, fullEnd - start))
                : TryMatchTwoWords("full", "join", PhraseBoundary.WordBoundary, out fullEnd)
                    ? new OuterJoinToken(OuterJoinType.Full, new TextSpan(start, fullEnd - start))
                    : null,
            's' => TryMatchTwoWords("semi", "join", PhraseBoundary.WordBoundary, out var semiEnd)
                    ? new SemiJoinToken(new TextSpan(start, semiEnd - start))
                    : null,
            'a' => TryMatchThreeWords("anti", "semi", "join", PhraseBoundary.WordBoundary, out var antiEnd)
                    ? new AntiJoinToken(new TextSpan(start, antiEnd - start))
                    : TryMatchTwoWords("anti", "join", PhraseBoundary.WordBoundary, out antiEnd)
                        ? new AntiJoinToken(new TextSpan(start, antiEnd - start))
                        : TryMatchAsOfJoin(start),
            _ => null
        };
    }

    private AsOfJoinToken? TryMatchAsOfJoin(int start)
    {
        if (TryMatchFourWords("asof", "left", "outer", "join", PhraseBoundary.WordBoundary, out var end) ||
            TryMatchThreeWords("asof", "left", "join", PhraseBoundary.WordBoundary, out end))
            return new AsOfJoinToken(true, new TextSpan(start, end - start));

        return TryMatchTwoWords("asof", "join", PhraseBoundary.WordBoundary, out end)
            ? new AsOfJoinToken(false, new TextSpan(start, end - start))
            : null;
    }

}
