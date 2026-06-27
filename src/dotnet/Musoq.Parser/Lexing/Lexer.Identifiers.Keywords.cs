using System.Text.RegularExpressions;
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
            'n' => TryMatchRegex(NotInRegex, start, span => new NotInToken(span)) ??
                   TryMatchRegex(NotLikeRegex, start, span => new NotLikeToken(span)) ??
                   TryMatchRegex(NotRLikeRegex, start, span => new NotRLikeToken(span)),
            'u' => TryMatchRegex(UnionAllRegex, start, span => new UnionAllToken(span)),
            'g' => TryMatchRegex(GroupByRegex, start, span => new GroupByToken(span)),
            'o' => TryMatchRegex(OrderByRegex, start, span => new OrderByToken(span)) ??
                   TryMatchRegex(OuterApplyRegex, start, span => new OuterApplyToken(span)),
            'p' => TryMatchRegex(PartitionByRegex, start, span => new PartitionByToken(span)),
            'j' or 'i' => TryMatchRegex(InnerJoinRegex, start, span => new InnerJoinToken(span)),
            'l' => TryMatchRegex(AntiJoinRegex, start, span => new AntiJoinToken(span)) ??
                   TryMatchRegex(SemiJoinRegex, start, span => new SemiJoinToken(span)) ??
                   TryMatchRegex(OuterJoinRegex, start, span => new OuterJoinToken(OuterJoinType.Left, span)),
            'r' => TryMatchRegex(OuterJoinRegex, start, span => new OuterJoinToken(OuterJoinType.Right, span)),
            'c' => TryMatchRegex(CurrentRowRegex, start, span => new CurrentRowToken(span)) ??
                   TryMatchRegex(CrossApplyRegex, start, span => new CrossApplyToken(span)) ??
                   TryMatchRegex(CrossJoinRegex, start, span => new CrossJoinToken(span)),
            'f' => TryMatchRegex(OuterJoinRegex, start, span => new OuterJoinToken(OuterJoinType.Full, span)),
            's' => TryMatchRegex(SemiJoinRegex, start, span => new SemiJoinToken(span)),
            'a' => TryMatchRegex(AntiJoinRegex, start, span => new AntiJoinToken(span)) ??
                   TryMatchAsOfJoin(start),
            _ => null
        };
    }

    private AsOfJoinToken? TryMatchAsOfJoin(int start)
    {
        var match = AsOfJoinRegex.Match(Input, Position);

        if (!match.Success || match.Index != Position)
            return null;

        Position += match.Length;
        var isLeft = match.Groups[1].Success;
        return new AsOfJoinToken(isLeft, new TextSpan(start, match.Length));
    }

    private Token? TryMatchRegex(Regex regex, int start, Func<TextSpan, Token> tokenFactory)
    {
        var match = regex.Match(Input, Position);

        if (!match.Success || match.Index != Position)
            return null;

        Position += match.Length;
        return tokenFactory(new TextSpan(start, match.Length));
    }

}
