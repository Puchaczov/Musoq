using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class StructuralSyntaxParserTests
{
    [TestMethod]
    public void ArrayAndRecordLiterals_ShouldParseAndPrintCanonically()
    {
        const string query = "select x.Value from #inputs.numbers(values: array { (Id: 'todo'), (Pattern: 'FIXME', Id: 'fixme'), }) x";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var text = root.ToString();

        StringAssert.Contains(text, "array { (Id: 'todo'), (Pattern: 'FIXME', Id: 'fixme') }");
        var statement = (StatementsArrayNode)root.Expression;
        var singleSet = (SingleSetNode)statement.Statements[0].Node;
        var sourceWrapper = (ExpressionFromNode)singleSet.Query.From;
        var source = (SchemaFromNode)sourceWrapper.Expression;
        var array = (ArrayLiteralNode)source.Parameters.Args[0];
        Assert.HasCount(2, array.Elements);
        Assert.IsInstanceOfType<RecordLiteralNode>(array.Elements[0]);
        Assert.AreEqual("Id", ((RecordLiteralNode)array.Elements[0]).Fields[0].Name);
    }

    [TestMethod]
    public void ParenthesizedArithmetic_ShouldRemainGrouping()
    {
        const string query = "select (1 + 2) * 3 from #test.rows()";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var select = ((SingleSetNode)((StatementsArrayNode)root.Expression).Statements[0].Node).Query.Select;
        Assert.IsInstanceOfType<StarNode>(select.Fields[0].Expression);
        Assert.IsInstanceOfType<AddNode>(((StarNode)select.Fields[0].Expression).Left);
    }

    [TestMethod]
    public void InferredAndRecursiveDeclarations_ShouldPreserveTypeSyntax()
    {
        const string query = "let todo = (Id: 'todo', Pattern: 'TODO'); let patterns: (Id: string, Pattern: string, Mode: string = 'literal')[] = array { $todo }; select 1 from #test.rows()";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var inferred = (ScriptVariableDeclarationNode)statements.Statements[0].Node;
        var annotated = (ScriptVariableDeclarationNode)statements.Statements[1].Node;

        Assert.IsTrue(inferred.IsInferred);
        Assert.IsInstanceOfType<RecordLiteralNode>(inferred.Initializer);
        Assert.AreEqual("(Id: string, Pattern: string, Mode: string = 'literal')[]", annotated.DeclaredTypeName);
        Assert.IsNotNull(annotated.TypeSyntax);
        Assert.IsInstanceOfType<ArrayLiteralNode>(annotated.Initializer);
    }

    [TestMethod]
    public void RecursiveParameterSuffixes_ShouldPrintWithCorrectNullabilityPlacement()
    {
        const string query = "params(a: int?[], b: int[]?, c: int[][], d: (Id: string)?[]? = null) select 1 from #test.rows()";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var block = (ParameterBlockNode)((StatementsArrayNode)root.Expression).Statements[0].Node;

        CollectionAssert.AreEqual(
            new[] { "int?[]", "int[]?", "int[][]", "(Id: string)?[]?" },
            block.Parameters.Select(static parameter => parameter.DeclaredTypeName).ToArray());
    }
    [TestMethod]
    public void StructuralLookahead_ShouldSkipCommentsWithoutAdvancingTheLexer()
    {
        var lexer = new Lexer("array /* between keyword and brace */ { 1 }", true);
        var first = lexer.Next();
        var commentsBeforePeek = lexer.Comments.Count;

        var peeked = lexer.Peek();

        Assert.AreEqual(first, lexer.Current());
        Assert.AreEqual(commentsBeforePeek, lexer.Comments.Count);
        Assert.AreEqual(peeked, lexer.Next());
    }

    [TestMethod]
    public void RecordsAndValues_ShouldShareTokenFieldNamesAndTriviaRules()
    {
        const string query = "select x.rows from values /* source */ { /* row */ ( /* field */ rows: 1, ) } x";
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var queryNode = ((SingleSetNode)statements.Statements[0].Node).Query;
        var values = (ValuesFromNode)(queryNode.From is ExpressionFromNode expressionFrom
            ? expressionFrom.Expression
            : queryNode.From);

        Assert.AreEqual("rows", values.Rows[0].Fields[0].Name);

        var arrayQuery = "select x.Value from #inputs.numbers(values: array /* trivia */ { (rows: 1) }) x";
        var arrayRoot = new Parser(new Lexer(arrayQuery, true)).ComposeAll();
        var arrayQueryNode = ((SingleSetNode)((StatementsArrayNode)arrayRoot.Expression).Statements[0].Node).Query;
        var source = (SchemaFromNode)((ExpressionFromNode)arrayQueryNode.From).Expression;
        var array = (ArrayLiteralNode)source.Parameters.Args[0];
        var record = (RecordLiteralNode)array.Elements[0];

        Assert.AreEqual("rows", record.Fields[0].Name);
    }}
