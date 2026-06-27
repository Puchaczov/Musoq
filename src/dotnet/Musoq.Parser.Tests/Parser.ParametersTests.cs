using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserParametersTests
{
    [TestMethod]
    public void Lexer_ParameterReference_ShouldReturnParameterReferenceToken()
    {
        var lexer = new Lexer("$author", true);

        lexer.Next();

        Assert.AreEqual(TokenType.ParameterReference, lexer.Current().TokenType);
        Assert.AreEqual("author", lexer.Current().Value);
        Assert.AreEqual("$author", lexer.Current().ToString());
    }

    [TestMethod]
    public void Parser_ParamBlockWithDefaults_ShouldParseAsStatement()
    {
        const string query = "param (author: string, limit: int = 100, since: datetime? = null); select $author, $limit from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(2, statements.Statements);

        var parameterBlock = (ParameterBlockNode)statements.Statements[0].Node;
        Assert.HasCount(3, parameterBlock.Parameters);
        Assert.AreEqual("author", parameterBlock.Parameters[0].Name);
        Assert.AreEqual("string", parameterBlock.Parameters[0].TypeName);
        Assert.IsFalse(parameterBlock.Parameters[0].HasDefaultValue);
        Assert.AreEqual("limit", parameterBlock.Parameters[1].Name);
        Assert.AreEqual("int", parameterBlock.Parameters[1].TypeName);
        Assert.IsInstanceOfType<IntegerNode>(parameterBlock.Parameters[1].DefaultValue);
        Assert.AreEqual("since", parameterBlock.Parameters[2].Name);
        Assert.AreEqual("datetime?", parameterBlock.Parameters[2].DeclaredTypeName);
        Assert.IsInstanceOfType<NullNode>(parameterBlock.Parameters[2].DefaultValue);
    }

    [TestMethod]
    public void Parser_ParamBlockWithArrayType_ShouldParseAsStatement()
    {
        const string query = "param(ids: int[]) select 1 from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        var parameterBlock = (ParameterBlockNode)statements.Statements[0].Node;

        Assert.HasCount(1, parameterBlock.Parameters);
        Assert.AreEqual("ids", parameterBlock.Parameters[0].Name);
        Assert.AreEqual("int[]", parameterBlock.Parameters[0].TypeName);
        Assert.AreEqual("int[]", parameterBlock.Parameters[0].DeclaredTypeName);
        Assert.IsFalse(parameterBlock.Parameters[0].IsNullable);
        Assert.IsFalse(parameterBlock.Parameters[0].HasDefaultValue);
    }

    [TestMethod]
    public void Parser_ParameterReferences_ShouldParseAsExpressions()
    {
        const string query = "param(author: string, limit: int = 100) select $author, $limit from #test.rows()";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        var queryNode = ((SingleSetNode)statements.Statements[1].Node).Query;

        Assert.IsInstanceOfType<ParameterReferenceNode>(queryNode.Select.Fields[0].Expression);
        Assert.AreEqual("author", ((ParameterReferenceNode)queryNode.Select.Fields[0].Expression).Name);
        Assert.IsInstanceOfType<ParameterReferenceNode>(queryNode.Select.Fields[1].Expression);
        Assert.AreEqual("limit", ((ParameterReferenceNode)queryNode.Select.Fields[1].Expression).Name);
    }

    [TestMethod]
    public void Parser_InParameterReference_ShouldParseCollectionInNode()
    {
        const string query = "param(ids: int[]) select Name from #test.rows() where Id in $ids";

        var root = Parse(query);
        var statements = (StatementsArrayNode)root.Expression;
        var queryNode = ((SingleSetNode)statements.Statements[1].Node).Query;

        Assert.IsNotNull(queryNode.Where);
        Assert.IsInstanceOfType<CollectionInNode>(queryNode.Where.Expression);
        var collectionIn = (CollectionInNode)queryNode.Where.Expression;
        Assert.IsInstanceOfType<ParameterReferenceNode>(collectionIn.Collection);
        Assert.AreEqual("ids", ((ParameterReferenceNode)collectionIn.Collection).Name);
    }

    [TestMethod]
    public void Parser_QueryWithoutParamBlock_ShouldParseAsBefore()
    {
        var root = Parse("select 1 from #test.rows()");
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(1, statements.Statements);
        Assert.IsInstanceOfType<SingleSetNode>(statements.Statements[0].Node);
    }

    [TestMethod]
    [DataRow("param(string author) select 1 from #test.rows()", DisplayName = "C# style parameter")]
    [DataRow("param([string]$author) select 1 from #test.rows()", DisplayName = "PowerShell style parameter")]
    [DataRow("def query(author: str = \"x\") select 1 from #test.rows()", DisplayName = "Python style parameter")]
    [DataRow("declare @author string; select 1 from #test.rows()", DisplayName = "SQL variable declaration")]
    public void Parser_UnsupportedParameterSyntax_ShouldReject(string query)
    {
        try
        {
            Parse(query);
            Assert.Fail("Unsupported parameter syntax should not parse.");
        }
        catch (Exception ex) when (ex is SyntaxException or ParseException)
        {
        }
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        return parser.ComposeAll();
    }
}
