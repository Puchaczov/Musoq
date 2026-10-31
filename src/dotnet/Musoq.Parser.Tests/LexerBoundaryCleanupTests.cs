using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class LexerBoundaryCleanupTests
{
    [TestMethod]
    [DataRow("table", "table")]
    [DataRow("something4", "something4")]
    [DataRow("[table name]", "table name")]
    [DataRow("*", "*")]
    [DataRow("Δείγμα", "Δείγμα")]
    [DataRow("𐐀Name", "𐐀Name")]
    [DataRow(" /* keep */ table", "table")]
    [DataRow(" -- keep\n table", "table")]
    public void ForwardingLexer_ShouldParseSourceNamesUsingOrdinaryTokens(string spelling, string expected)
    {
        var lexer = new ForwardingLexer($"couple #some.{spelling} with table Test as Bound;");
        var root = new Parser(lexer).ComposeAll();
        var statement = ((StatementsArrayNode)root.Expression).Statements.Single();

        Assert.AreEqual(expected, ((CoupleNode)statement.Node).SchemaMethodNode.Method);
    }

    [TestMethod]
    [DataRow("123")]
    [DataRow("12name")]
    [DataRow("'name'")]
    [DataRow("rows()")]
    [DataRow("(Name)")]
    public void SourceExpressionsAndInvocation_ShouldRemainInvalid(string spelling)
    {
        var result = Parser.ParseSyntax($"couple #some.{spelling} with table Test as Bound;");

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Root);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
    }

    [TestMethod]
    [DataRow("a. /* keep */ table")]
    [DataRow("a. -- keep\n table")]
    [DataRow("a.\r\n table")]
    public void SkippedTrivia_ShouldPreserveMemberNameContext(string source)
    {
        var lexer = new Lexer(source, true);
        lexer.Next();
        lexer.Next();
        var token = lexer.Next();

        Assert.AreEqual(TokenType.Property, token.TokenType);
        Assert.AreEqual("table", token.Value);
        Assert.AreEqual(source.IndexOf("table", StringComparison.Ordinal), token.Span.Start);
    }

    [TestMethod]
    public void SuccessfulPeek_ShouldNotPublishObservationsOrChangeState()
    {
        var observations = new List<Token>();
        var lexer = new Lexer("select /* keep */ Name, Other from #s.rows()", true, false, observations.Add);
        var first = lexer.Next();
        var position = lexer.Position;
        var count = observations.Count;
        var peeked = lexer.Peek(3);

        Assert.AreEqual("Other", peeked.Value);
        Assert.AreEqual(count, observations.Count);
        Assert.AreSame(first, lexer.Current());
        Assert.AreEqual(position, lexer.Position);
        Assert.AreEqual(0, lexer.Comments.Count);
        Assert.AreEqual("Name", lexer.Next().Value);
    }

    [TestMethod]
    public void FailingPeek_ShouldRestoreObservationAndRecoveryState()
    {
        var observations = new List<Token>();
        var lexer = new Lexer("select Name @", true, true, observations.Add);
        var first = lexer.Next();
        var position = lexer.Position;
        var count = observations.Count;

        Assert.ThrowsExactly<UnknownTokenException>(() => lexer.Peek(2));
        Assert.AreSame(first, lexer.Current());
        Assert.AreEqual(position, lexer.Position);
        Assert.AreEqual(count, observations.Count);
        Assert.IsTrue(lexer.RecoverOnError);
    }

    [TestMethod]
    public void PeekAfterDot_ShouldRestoreSignificantTokenContext()
    {
        var lexer = new Lexer("a. /* keep */ table", true);
        lexer.Next();
        lexer.Next();
        lexer.Peek();
        var actual = lexer.Next();

        Assert.AreEqual(TokenType.Property, actual.TokenType);
        Assert.AreEqual("table", actual.Value);
    }

    [TestMethod]
    public void RegexTokenRead_ShouldNotifyTheConstructionTimeObserver()
    {
        var observations = new List<Token>();
        var lexer = new Lexer("Name", true, false, observations.Add);
        var token = lexer.NextOf(new Regex(@"\w+"), value => new ColumnToken(value, new TextSpan(0, value.Length)));

        Assert.AreSame(token, observations.Single());
        Assert.AreEqual(new TextSpan(0, 4), token.Span);
    }

    [TestMethod]
    public void ObservationCallbackMayPeek_ShouldNotPublishSpeculativeTokens()
    {
        var observations = new List<Token>();
        Lexer? lexer = null;
        lexer = new Lexer("select Name from #s.rows()", true, false, token => ObserveWithPeek(lexer!, observations, token));
        lexer.Next();

        Assert.AreEqual("select", observations.Single().Value);
        Assert.AreEqual("select", lexer.Current().Value);
        Assert.AreEqual("Name", lexer.Peek().Value);
        Assert.AreEqual(1, observations.Count);
    }

    [TestMethod]
    public void SchemaLookahead_ShouldRestorePendingTokensWithoutObservations()
    {
        var observations = new List<Token>();
        var lexer = new Lexer("byte[2]", true, false, observations.Add) { IsSchemaContext = true };
        lexer.Next();
        var count = observations.Count;
        var peeked = lexer.Peek(2);

        Assert.AreEqual("2", peeked.Value);
        Assert.AreEqual(count, observations.Count);
        Assert.AreEqual(TokenType.LeftSquareBracket, lexer.Next().TokenType);
        Assert.AreEqual("2", lexer.Next().Value);
        Assert.AreEqual(TokenType.RightSquareBracket, lexer.Next().TokenType);
    }

    [TestMethod]
    [DataRow("binary Box<T> { Value: T }; binary Envelope { Value: Box<Box<int>> };")]
    [DataRow("text Entry { Value: rest }; select e.Value from #s.rows() a cross apply Parse<Entry>(a.Line) e")]
    [DataRow("couple #s. /* keep */ [table name] with settings profile as Bound;")]
    public void SourceCapture_ShouldRetainSchemaAndAlternateTokenExtents(string source)
    {
        var result = Parser.ParseSyntax(source);

        Assert.IsTrue(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.AreEqual(source, string.Concat(result.Tokens.Select(t => t.Text)));
        Assert.IsFalse(result.Tokens.Any(t => t.Role == SyntaxTokenRole.Unknown));
        Assert.IsTrue(result.Tokens.All(t => source.Substring(t.Span.Start, t.Span.Length) == t.Text));
    }

    [TestMethod]
    public void LegacyEntryPoint_ShouldWrapUnexpectedLexerFailures()
    {
        var lexer = new ForwardingLexer("select Name from #s.rows()", new InvalidOperationException("sentinel"));
        var exception = Assert.ThrowsExactly<SyntaxException>(() => new Parser(lexer).ComposeAll());

        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        StringAssert.Contains(exception.Message, "sentinel");
    }

    [TestMethod]
    public void CompositionCore_ShouldPropagateUnexpectedFailuresIndependentlyOfCapture()
    {
        var failure = new InvalidOperationException("sentinel");
        var lexer = new ForwardingLexer("select Name from #s.rows()", failure);
        var capture = new SourceCapture(lexer.SourceText);
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => new Parser(lexer, capture).ComposeAllCore());

        Assert.AreSame(failure, exception);
    }

    private sealed class ForwardingLexer(string source, Exception? failure = null) : ILexer
    {
        private readonly Lexer _inner = new(source, true);
        public int Position => _inner.Position;
        public string AlreadyResolvedQueryPart => _inner.AlreadyResolvedQueryPart;
        public bool IsSchemaContext { get => _inner.IsSchemaContext; set => _inner.IsSchemaContext = value; }
        public string Input => _inner.Input;
        public SourceText SourceText => _inner.SourceText;
        public IReadOnlyList<CommentToken> Comments => _inner.Comments;
        public DiagnosticBag Diagnostics => _inner.Diagnostics;
        public bool RecoverOnError { get => _inner.RecoverOnError; set => _inner.RecoverOnError = value; }
        public Token Last() => _inner.Last();
        public Token Current() => _inner.Current();
        public Token Peek(int lookahead = 1) => _inner.Peek(lookahead);
        public Token Next()
        {
            if (failure != null)
                throw failure;
            return _inner.Next();
        }
        public Token NextOf(Regex regex, Func<string, Token> getToken) =>
            throw new InvalidOperationException("The parser must consume the ordinary token stream.");
    }

    private static void ObserveWithPeek(Lexer lexer, List<Token> observations, Token token)
    {
        observations.Add(token);
        if (token.TokenType == TokenType.Select)
            lexer.Peek(2);
    }
}
