using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Helpers;

/// <summary>
/// Registry of all SQL keywords supported by the Musoq query language.
/// Used for providing helpful suggestions when keywords are mistyped.
/// </summary>
public static class KeywordRegistry
{
    /// <summary>
    /// All SQL keywords supported by Musoq, normalized to lowercase.
    /// </summary>
    public static readonly HashSet<string> AllKeywords = new HashSet<string>
    {
        // Core SQL keywords
        SelectToken.TokenText,
        FromToken.TokenText,
        WhereToken.TokenText,
        "group by",  // GroupByToken
        "order by",  // OrderByToken
        HavingToken.TokenText,
        
        // Join keywords
        "inner join",  // InnerJoinToken
        "outer join",  // OuterJoinToken  
        "left join",
        "right join",
        "cross apply",  // CrossApplyToken
        "outer apply",  // OuterApplyToken
        OnToken.TokenText,
        
        // Set operators
        SetOperatorToken.UnionOperatorText,
        "union all",  // UnionAllToken
        SetOperatorToken.ExceptOperatorText,
        SetOperatorToken.IntersectOperatorText,
        
        // Clauses and modifiers
        AsToken.TokenText,
        WithToken.TokenText,
        SkipToken.TokenText,
        TakeToken.TokenText,
        
        // Conditional keywords
        CaseToken.TokenText,
        WhenToken.TokenText,
        ThenToken.TokenText,
        ElseToken.TokenText,
        EndToken.TokenText,
        
        // Logical operators
        AndToken.TokenText,
        OrToken.TokenText,
        NotToken.TokenText,
        InToken.TokenText,
        "not in",  // NotInToken
        
        // Comparison operators
        LikeToken.TokenText,
        "not like",  // NotLikeToken
        RLikeToken.TokenText,
        "not rlike",  // NotRLikeToken
        ContainsToken.TokenText,
        IsToken.TokenText,
        
        // Literals and constants
        NullToken.TokenText,
        TrueToken.TokenText,
        FalseToken.TokenText,
        
        // Sort order
        AscToken.TokenText,
        DescToken.TokenText,
        
        // Special keywords
        TableToken.TokenText,
        CoupleToken.TokenText
    };

    /// <summary>
    /// Primary keywords that are most commonly used and most likely to be mistyped.
    /// Used for prioritizing suggestions.
    /// </summary>
    public static readonly HashSet<string> PrimaryKeywords = new HashSet<string>
    {
        SelectToken.TokenText,
        FromToken.TokenText,
        WhereToken.TokenText,
        "group by",
        "order by",
        HavingToken.TokenText,
        "inner join",
        "left join",
        "right join",
        AsToken.TokenText,
        WithToken.TokenText,
        AndToken.TokenText,
        OrToken.TokenText,
        NotToken.TokenText,
        InToken.TokenText,
        LikeToken.TokenText,
        IsToken.TokenText,
        NullToken.TokenText,
        TrueToken.TokenText,
        FalseToken.TokenText
    };
}
