using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserValuesFromTests
{
    [TestMethod]
    public void ValuesFromSource_ShouldParseRowsAndAlias()
    {
        var root = Parse("from values { { Name: 'Newtonsoft.Json', Approved: true }, { Name: 'Legacy.Package', Approved: false } } packages select packages.Name");

        var query = GetSingleQuery(root);
        var values = (ValuesFromNode)UnwrapFrom(query.From);

        Assert.AreEqual("packages", values.Alias);
        Assert.HasCount(2, values.Rows);
        Assert.AreEqual("Name", values.Rows[0].Fields[0].Name);
        Assert.AreEqual("Approved", values.Rows[0].Fields[1].Name);
        Assert.HasCount(2, values.Rows[1].Fields);
    }

    [TestMethod]
    public void ValuesFromSource_WithTrailingCommas_ShouldParse()
    {
        var root = Parse("select v.Name from values { { Name: 'A', Approved: true, }, { Name: 'B', Approved: false, }, } v");

        var query = GetSingleQuery(root);
        var values = (ValuesFromNode)UnwrapFrom(query.From);

        Assert.AreEqual("v", values.Alias);
        Assert.HasCount(2, values.Rows);
        Assert.HasCount(2, values.Rows[0].Fields);
        Assert.HasCount(2, values.Rows[1].Fields);
    }

    [TestMethod]
    public void ValuesFromSource_WithUnsignedIntSuffix_ShouldParse()
    {
        var root = Parse("from values { { Score: 10ui } } scores select scores.Score");

        var query = GetSingleQuery(root);
        var values = (ValuesFromNode)UnwrapFrom(query.From);
        var score = (IntegerNode)values.Rows[0].Fields[0].Expression;

        Assert.AreEqual(typeof(uint), score.ReturnType);
        Assert.AreEqual(10u, score.ObjValue);
    }

    [TestMethod]
    public void ValuesFromSource_WithSupportedNumericLiterals_ShouldParseTypes()
    {
        var cases = new (string Literal, Type ExpectedType, object ExpectedValue)[]
        {
            ("10", typeof(int), 10),
            ("10i", typeof(int), 10),
            ("10ui", typeof(uint), 10u),
            ("10l", typeof(long), 10L),
            ("10ul", typeof(ulong), 10UL),
            ("10s", typeof(short), (short)10),
            ("10us", typeof(ushort), (ushort)10),
            ("10b", typeof(sbyte), (sbyte)10),
            ("10ub", typeof(byte), (byte)10),
            ("10d", typeof(decimal), 10m),
            ("10.5", typeof(decimal), 10.5m),
            ("10.5d", typeof(decimal), 10.5m),
            ("0x10", typeof(long), 16L),
            ("0X10", typeof(long), 16L),
            ("0b1010", typeof(long), 10L),
            ("0B1010", typeof(long), 10L),
            ("0o17", typeof(long), 15L),
            ("0O17", typeof(long), 15L),
            ("-10", typeof(int), -10),
            ("-10.5", typeof(decimal), -10.5m)
        };

        foreach (var testCase in cases)
        {
            var root = Parse($"from values {{ {{ Value: {testCase.Literal} }} }} valuesSource select valuesSource.Value");
            var query = GetSingleQuery(root);
            var values = (ValuesFromNode)UnwrapFrom(query.From);
            var literal = values.Rows[0].Fields[0].Expression;

            Assert.AreEqual(testCase.ExpectedType, literal.ReturnType, testCase.Literal);
            Assert.AreEqual(testCase.ExpectedValue, ((ConstantValueNode)literal).ObjValue, testCase.Literal);
        }
    }

    [TestMethod]
    public void ValuesFromSource_WithBareUnsignedSuffix_ShouldFail()
    {
        Assert.Throws<SyntaxException>(() => Parse("from values { { Score: 10u } } scores select scores.Score"));
    }

    [TestMethod]
    public void ValuesFromSource_WithBasePrefixedNumericLiteralTypeSuffix_ShouldFail()
    {
        Assert.Throws<SyntaxException>(() => Parse("from values { { Score: 0x10ui } } scores select scores.Score"));
    }

    [TestMethod]
    public void ValuesFromSource_InJoin_ShouldParse()
    {
        var root = Parse("from #os.files('.') f join values { { Extension: '.dll', MaxSize: 5242880 }, { Extension: '.exe', MaxSize: 5242880 } } policy on f.Extension = policy.Extension select f.FullName");

        var query = GetSingleQuery(root);
        var join = ((JoinNode)UnwrapFrom(query.From)).Join;

        Assert.IsInstanceOfType<ValuesFromNode>(join.With);
        Assert.AreEqual("policy", join.With.Alias);
    }

    [TestMethod]
    public void ValuesFromSource_WithoutAlias_ShouldFail()
    {
        var exception = Assert.Throws<SyntaxException>(() => Parse("select * from values { { Name: 'A' } }"));

        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, exception.Code);
    }

    [TestMethod]
    public void SourceNamedValues_WithoutLiteralBrace_ShouldRemainInMemorySource()
    {
        var root = Parse("select * from values v");

        var query = GetSingleQuery(root);
        var source = (InMemoryTableFromNode)UnwrapFrom(query.From);

        Assert.AreEqual("values", source.VariableName);
        Assert.AreEqual("v", source.Alias);
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        return parser.ComposeAll();
    }

    private static QueryNode GetSingleQuery(Node root)
    {
        var statements = (StatementsArrayNode)((RootNode)root).Expression;
        var statementNode = statements.Statements[0].Node;
        return statementNode is SingleSetNode singleSet
            ? singleSet.Query
            : (QueryNode)statementNode;
    }

    private static FromNode UnwrapFrom(FromNode from)
    {
        return from is ExpressionFromNode expressionFrom
            ? expressionFrom.Expression
            : from;
    }
}
