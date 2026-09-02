using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticRework074LexicalContractTests
{
    [TestMethod]
    [DataRow("sElEcT", TokenType.Select)]
    [DataRow("fRoM", TokenType.From)]
    [DataRow("wHeRe", TokenType.Where)]
    [DataRow("aNd", TokenType.And)]
    [DataRow("oR", TokenType.Or)]
    [DataRow("nOt", TokenType.Not)]
    [DataRow("aS", TokenType.As)]
    [DataRow("iS", TokenType.Is)]
    [DataRow("nUlL", TokenType.Null)]
    [DataRow("iN", TokenType.In)]
    [DataRow("lIkE", TokenType.Like)]
    [DataRow("rLiKe", TokenType.RLike)]
    [DataRow("hAvInG", TokenType.Having)]
    [DataRow("cOnTaInS", TokenType.Contains)]
    [DataRow("uNiOn", TokenType.Union)]
    [DataRow("eXcEpT", TokenType.Except)]
    [DataRow("iNtErSeCt", TokenType.Intersect)]
    [DataRow("sKiP", TokenType.Skip)]
    [DataRow("tAkE", TokenType.Take)]
    [DataRow("wItH", TokenType.With)]
    [DataRow("oN", TokenType.On)]
    [DataRow("fUnCtIoNs", TokenType.Functions)]
    [DataRow("tRuE", TokenType.True)]
    [DataRow("fAlSe", TokenType.False)]
    [DataRow("eXiStS", TokenType.Exists)]
    [DataRow("aNy", TokenType.Any)]
    [DataRow("sOmE", TokenType.Some)]
    [DataRow("aLl", TokenType.All)]
    [DataRow("tAbLe", TokenType.Table)]
    [DataRow("cOuPlE", TokenType.Couple)]
    [DataRow("cAsE", TokenType.Case)]
    [DataRow("wHeN", TokenType.When)]
    [DataRow("tHeN", TokenType.Then)]
    [DataRow("eLsE", TokenType.Else)]
    [DataRow("eNd", TokenType.End)]
    [DataRow("dIsTiNcT", TokenType.Distinct)]
    [DataRow("bEtWeEn", TokenType.Between)]
    [DataRow("oVeR", TokenType.Over)]
    [DataRow("wInDoW", TokenType.Window)]
    [DataRow("pIvOt", TokenType.Pivot)]
    [DataRow("uNpIvOt", TokenType.Unpivot)]
    [DataRow("dEsC", TokenType.Desc)]
    [DataRow("aSc", TokenType.Asc)]
    public void SingleWordKeywords_ShouldBeCaseInsensitiveAndSpanOriginalText(
        string keyword,
        TokenType expectedType)
    {
        var lexer = new Lexer(keyword, true);

        var token = lexer.Next();

        Assert.AreEqual(expectedType, token.TokenType);
        Assert.AreEqual(new TextSpan(0, keyword.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    [DataRow("nOt \t iN", TokenType.NotIn)]
    [DataRow("uNiOn\n  aLl", TokenType.UnionAll)]
    [DataRow("gRoUp \r\n bY", TokenType.GroupBy)]
    [DataRow("oRdEr by", TokenType.OrderBy)]
    [DataRow("iNnEr JOIN", TokenType.InnerJoin)]
    [DataRow("LeFt OuTeR JoIn", TokenType.OuterJoin)]
    [DataRow("RiGhT jOiN", TokenType.OuterJoin)]
    [DataRow("FuLl OuTeR JoIn", TokenType.OuterJoin)]
    [DataRow("cRoSs JoIn", TokenType.CrossJoin)]
    [DataRow("cRoSs ApPlY", TokenType.CrossApply)]
    [DataRow("OuTeR ApPlY", TokenType.OuterApply)]
    [DataRow("aSoF JoIn", TokenType.AsOfJoin)]
    [DataRow("cUrReNt\tRoW", TokenType.CurrentRow)]
    public void MultiWordKeywords_ShouldBeCaseInsensitiveAndPreservePhraseSpan(
        string phrase,
        TokenType expectedType)
    {
        var lexer = new Lexer(phrase, true);

        var token = lexer.Next();

        Assert.AreEqual(expectedType, token.TokenType);
        Assert.AreEqual(new TextSpan(0, phrase.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    [DataRow("select2")]
    [DataRow("select_2")]
    [DataRow("selecté")]
    [DataRow("select\U00010400")]
    public void KeywordPrefixWithAnyIdentifierContinuation_ShouldRemainOneIdentifier(string identifier)
    {
        var lexer = new Lexer(identifier, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(identifier, token.Value);
        Assert.AreEqual(new TextSpan(0, identifier.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    public void MultiWordKeywordBoundary_ShouldTreatAnAstralLetterAsAnIdentifierContinuation()
    {
        const string identifier = "join\U00010400";
        const string input = "left " + identifier;
        var lexer = new Lexer(input, true);

        var first = lexer.Next();
        var second = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, first.TokenType);
        Assert.AreEqual("left", first.Value);
        Assert.AreEqual(TokenType.Identifier, second.TokenType);
        Assert.AreEqual(identifier, second.Value);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    [DataRow("\U00010400Column")]
    [DataRow("name\U00010400")]
    [DataRow("name\u0301")]
    [DataRow("name\u203Fvalue")]
    [DataRow("name\u200Cvalue")]
    public void UnicodeIdentifier_ShouldRemainOneCaseSensitiveUtf16Span(string identifier)
    {
        var lexer = new Lexer(identifier, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, token.TokenType);
        Assert.AreEqual(identifier, token.Value);
        Assert.AreEqual(new TextSpan(0, identifier.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    public void UnicodeParameterName_ShouldRemainOneParameterReferenceToken()
    {
        const string input = "$\U00010400";
        var lexer = new Lexer(input, true);

        var token = lexer.Next();

        Assert.AreEqual(TokenType.ParameterReference, token.TokenType);
        Assert.AreEqual("\U00010400", token.Value);
        Assert.AreEqual(new TextSpan(0, input.Length), token.Span);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
    }

    [TestMethod]
    public void UnicodeGenericTypeName_ShouldRemainOneGenericFunctionToken()
    {
        const string input = "Interpret<\U00010400>(value)";
        var lexer = new Lexer(input, true);

        var token = lexer.Next();

        Assert.IsInstanceOfType<GenericFunctionToken>(token);
        var genericFunction = (GenericFunctionToken)token;
        Assert.AreEqual("Interpret", genericFunction.Value);
        Assert.AreEqual("\U00010400", genericFunction.TypeParameter);
        Assert.AreEqual(new TextSpan(0, input.IndexOf('>') + 1), genericFunction.Span);
    }

    [TestMethod]
    public void BracketQuotedIdentifier_ShouldPreserveSpecialCharactersAndUnicode()
    {
        const string firstIdentifier = "[order by -- not a comment]";
        const string secondIdentifier = "[列\U00010400]";
        var input = $"{firstIdentifier} {secondIdentifier}";
        var lexer = new Lexer(input, true);

        var first = lexer.Next();
        var second = lexer.Next();

        Assert.AreEqual(TokenType.Identifier, first.TokenType);
        Assert.AreEqual("order by -- not a comment", first.Value);
        Assert.AreEqual(new TextSpan(0, firstIdentifier.Length), first.Span);
        Assert.AreEqual(TokenType.Identifier, second.TokenType);
        Assert.AreEqual("列\U00010400", second.Value);
        Assert.AreEqual(new TextSpan(firstIdentifier.Length + 1, secondIdentifier.Length), second.Span);
        Assert.IsEmpty(lexer.Comments);
    }

    [TestMethod]
    public void Comments_ShouldBeSkippedCapturedAndNeverCreateDiagnostics()
    {
        const string query = "-- leading\r\nselect/* ignored SELECT FROM */1-- trailing\r\nfrom system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        Assert.HasCount(3, lexer.Comments);
        Assert.AreEqual("-- leading", lexer.Comments[0].Value);
        Assert.AreEqual("/* ignored SELECT FROM */", lexer.Comments[1].Value);
        Assert.AreEqual("-- trailing", lexer.Comments[2].Value);
        Assert.AreEqual(new TextSpan(0, 10), lexer.Comments[0].Span);
        Assert.AreEqual(new TextSpan(query.IndexOf("/*", StringComparison.Ordinal), 25), lexer.Comments[1].Span);
        Assert.AreEqual(new TextSpan(query.IndexOf("-- trailing", StringComparison.Ordinal), 11), lexer.Comments[2].Span);
    }

    [TestMethod]
    public void CommentMarkersInsideStringsAndBracketIdentifiers_ShouldRemainLiteral()
    {
        const string query = "select '-- not a comment /* still text */', [-- also text] from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        Assert.IsEmpty(lexer.Comments);
    }

    [TestMethod]
    public void LineCommentAtEndOfInput_ShouldBeSkippedWithoutAClosingDelimiter()
    {
        const string query = "select 1 from system.dual() -- trailing";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
        Assert.HasCount(1, lexer.Comments);
        Assert.AreEqual("-- trailing", lexer.Comments[0].Value);
        Assert.AreEqual(new TextSpan(query.IndexOf("--", StringComparison.Ordinal), 11), lexer.Comments[0].Span);
    }

    [TestMethod]
    public void UnknownCharacter_ShouldExposeAnExactLocatedRecoveryDiagnostic()
    {
        const string query = "select @ from system.dual()";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code);
        Assert.AreEqual("Unknown token '@'. Remove the unsupported character or rewrite this part using valid Musoq syntax.",
            diagnostic.Message);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf('@'), 1), diagnostic.Span);
        Assert.AreEqual("Core Spec - Lexical Structure", diagnostic.DocsReference);
        Assert.AreEqual(
            "The lexer encountered a character that is not part of Musoq SQL syntax.",
            diagnostic.Explanation);
        Assert.HasCount(2, diagnostic.SuggestedFixes);
        Assert.IsTrue(diagnostic.SuggestedFixes[0].Kind == DiagnosticActionKind.Suggestion);
        Assert.IsNull(diagnostic.SuggestedFixes[0].TextEdit);
        StringAssert.Contains(diagnostic.RelatedInfo[0], "Remaining input: @ from");
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void UnterminatedBlockComment_ShouldExposeItsWholeSpanAndActionableGuidance()
    {
        const string query = "select 1 from system.dual() /* missing";
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics[0];
        var commentStart = query.IndexOf("/*", StringComparison.Ordinal);
        Assert.AreEqual(DiagnosticCode.MQ1005_UnterminatedBlockComment, diagnostic.Code);
        Assert.AreEqual("Unterminated block comment. Expected closing '*/' but reached end of input.",
            diagnostic.Message);
        Assert.AreEqual(new TextSpan(commentStart, query.Length - commentStart), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Core Spec - Comments", diagnostic.DocsReference);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.HasCount(2, diagnostic.SuggestedFixes);
        Assert.IsNull(diagnostic.SuggestedFixes[0].TextEdit);
        Assert.IsNotNull(diagnostic.ContextSnippet);
    }

    [TestMethod]
    public void UnterminatedBracketedIdentifier_ShouldRecoverAtTheNextBracketedToken()
    {
        const string input = "[broken [valid]";
        var lexer = new Lexer(input, true, recoverOnError: true);

        var recovered = lexer.Next();
        var diagnostic = lexer.Diagnostics.ToSortedList()[0];

        Assert.AreEqual(TokenType.Identifier, recovered.TokenType);
        Assert.AreEqual("valid", recovered.Value);
        Assert.AreEqual(new TextSpan(8, 7), recovered.Span);
        Assert.AreEqual(DiagnosticCode.MQ2011_MissingClosingBracket, diagnostic.Code);
        Assert.AreEqual(new TextSpan(0, 8), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        Assert.HasCount(1, lexer.Diagnostics.ToSortedList());
    }

    [TestMethod]
    public void InvalidSurrogate_ShouldRemainOneLocatedUnknownTokenDuringRecovery()
    {
        const string invalidSurrogate = "\uD800";
        var lexer = new Lexer(invalidSurrogate, true, recoverOnError: true);

        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType);
        var diagnostic = lexer.Diagnostics.ToSortedList()[0];

        Assert.AreEqual(DiagnosticCode.MQ1001_UnknownToken, diagnostic.Code);
        Assert.AreEqual(new TextSpan(0, 1), diagnostic.Span);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.HasCount(1, lexer.Diagnostics.ToSortedList());
    }

    [TestMethod]
    public void AStructuralNearMissWithLiteralCommentMarkers_ShouldRemainValid()
    {
        const string query = "select '@', [@] from system.dual()";
        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
