using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserUnpivotTests
{
    [TestMethod]
    public void Unpivot_ShouldParseSourceEntriesKeepAndPostClauses()
    {
        var root = Parse("""
                         unpivot #sales.wide() s
                         on Quarter in (s.Q1 as Q1, s.Q2 as Q2)
                         using Sales
                         keep s.Region as Region
                         order by Region
                         skip 1
                         take 2
                         """);

        var query = GetSingleQuery(root);
        var unpivot = AssertUnpivot(query.From);

        Assert.AreEqual("Quarter", unpivot.NameColumn);
        Assert.AreEqual("Sales", unpivot.ValueColumn);
        Assert.HasCount(2, unpivot.Entries);
        Assert.AreEqual("Q1", unpivot.Entries[0].NameValue);
        Assert.AreEqual("Q2", unpivot.Entries[1].NameValue);
        Assert.HasCount(1, unpivot.KeepFields);
        Assert.AreEqual("Region", unpivot.KeepFields[0].FieldName);
        Assert.IsNotNull(query.OrderBy);
        Assert.IsNotNull(query.Skip);
        Assert.IsNotNull(query.Take);
        Assert.HasCount(1, query.Select.Fields);
        Assert.IsInstanceOfType<AllColumnsNode>(query.Select.Fields[0].Expression);

        var sourceExpression = Assert.IsInstanceOfType<ExpressionFromNode>(unpivot.Source);
        var source = Assert.IsInstanceOfType<SchemaFromNode>(sourceExpression.Expression);
        Assert.AreEqual("#sales", source.Schema);
        Assert.AreEqual("wide", source.Method);
        Assert.AreEqual("s", source.Alias);
    }

    [TestMethod]
    public void Unpivot_WithImplicitSimpleAliases_ShouldDeriveNameValues()
    {
        var root = Parse("""
                         unpivot #sales.wide() s
                         on Quarter in (s.Q1, Q2)
                         using Sales
                         keep s.Region
                         """);

        var unpivot = AssertUnpivot(GetSingleQuery(root).From);

        Assert.AreEqual("Q1", unpivot.Entries[0].NameValue);
        Assert.AreEqual("Q2", unpivot.Entries[1].NameValue);
        Assert.AreEqual("Region", unpivot.KeepFields[0].FieldName);
    }

    [TestMethod]
    public void Unpivot_ShouldParseInsideCteAndDerivedTable()
    {
        var cte = Parse("""
                        with u as (
                            unpivot #sales.wide() s
                            on Quarter in (s.Q1 as Q1)
                            using Sales
                            keep s.Region as Region
                        )
                        select Region, Quarter, Sales from u
                        """);

        Assert.IsNotNull(cte);

        var derived = Parse("""
                            select u.Region
                            from (
                                unpivot #sales.wide() s
                                on Quarter in (s.Q1 as Q1)
                                using Sales
                                keep s.Region as Region
                            ) u
                            """);

        Assert.IsNotNull(derived);
    }

    [TestMethod]
    public void Unpivot_LexerShouldProduceUnpivotToken()
    {
        var lexer = new Lexer("unpivot #sales.wide() on Quarter in (Q1) using Sales", true);

        var token = lexer.Next();

        Assert.AreEqual(Tokens.TokenType.Unpivot, token.TokenType);
        Assert.AreEqual("unpivot", token.Value);
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

    private static UnpivotFromNode AssertUnpivot(FromNode from)
    {
        var expression = Assert.IsInstanceOfType<ExpressionFromNode>(from);
        return Assert.IsInstanceOfType<UnpivotFromNode>(expression.Expression);
    }
}
