using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserPivotTests
{
    [TestMethod]
    public void Pivot_WithSingleMeasure_ShouldLowerToGroupedAggregateProjection()
    {
        var root = Parse("""
                         pivot #sales.orders()
                         on Quarter in ('Q1' as Q1, 'Q2' as Q2)
                         using Sum(Amount) as Sales
                         group by Region
                         order by Region
                         skip 1
                         take 10
                         """);

        var query = GetSingleQuery(root);

        var expressionFrom = Assert.IsInstanceOfType<ExpressionFromNode>(query.From);
        var from = Assert.IsInstanceOfType<SchemaFromNode>(expressionFrom.Expression);
        Assert.AreEqual("#sales", from.Schema);
        Assert.AreEqual("orders", from.Method);
        Assert.IsNotNull(query.GroupBy);
        Assert.IsNotNull(query.OrderBy);
        Assert.IsNotNull(query.Skip);
        Assert.IsNotNull(query.Take);
        Assert.HasCount(3, query.Select.Fields);
        Assert.AreEqual("Region", query.Select.Fields[0].FieldName);
        Assert.AreEqual("Q1", query.Select.Fields[1].FieldName);
        Assert.AreEqual("Q2", query.Select.Fields[2].FieldName);

        var q1Measure = AssertAccessMethod(query.Select.Fields[1].Expression);
        Assert.AreEqual("Sum", q1Measure.Name);
        Assert.IsTrue(q1Measure.HasFilter);
        Assert.HasCount(1, q1Measure.Arguments.Args);
        Assert.IsInstanceOfType<IdentifierNode>(q1Measure.Arguments.Args[0]);
        Assert.IsInstanceOfType<EqualityNode>(q1Measure.FilterExpression);
    }

    [TestMethod]
    public void Pivot_WithMultipleMeasures_ShouldIncludeMeasureAliasInGeneratedColumns()
    {
        var root = Parse("""
                         pivot #sales.orders()
                         on Quarter in ('Q1' as Q1, 'Q2' as Q2)
                         using Sum(Amount) as Sales, Count(*) as Orders
                         group by Region
                         """);

        var query = GetSingleQuery(root);

        Assert.HasCount(5, query.Select.Fields);
        Assert.AreEqual("Region", query.Select.Fields[0].FieldName);
        Assert.AreEqual("Q1_Sales", query.Select.Fields[1].FieldName);
        Assert.AreEqual("Q1_Orders", query.Select.Fields[2].FieldName);
        Assert.AreEqual("Q2_Sales", query.Select.Fields[3].FieldName);
        Assert.AreEqual("Q2_Orders", query.Select.Fields[4].FieldName);

        var countMeasure = AssertAccessMethod(query.Select.Fields[2].Expression);
        Assert.HasCount(1, countMeasure.Arguments.Args);
        Assert.IsInstanceOfType<AllColumnsNode>(countMeasure.Arguments.Args[0]);
        Assert.IsInstanceOfType<EqualityNode>(countMeasure.FilterExpression);
    }

    [TestMethod]
    public void Pivot_WithoutGroupBy_ShouldLowerToAggregateOnlyProjection()
    {
        var root = Parse("""
                         pivot #sales.orders()
                         on Quarter in ('Q1' as Q1, 'Q2' as Q2)
                         using Sum(Amount) as Sales
                         """);

        var query = GetSingleQuery(root);

        Assert.IsNull(query.GroupBy);
        Assert.HasCount(2, query.Select.Fields);
        Assert.AreEqual("Q1", query.Select.Fields[0].FieldName);
        Assert.AreEqual("Q2", query.Select.Fields[1].FieldName);
    }

    [TestMethod]
    public void Pivot_WithMultiColumnKey_ShouldLowerToConjunctiveFilteredAggregate()
    {
        var root = Parse("""
                         pivot #cities.entities()
                         on Year, Country in ((2000, 'NL') as y2000_nl)
                         using Sum(Population) as Total
                         group by Name
                         """);

        var query = GetSingleQuery(root);

        Assert.HasCount(2, query.Select.Fields);
        Assert.AreEqual("Name", query.Select.Fields[0].FieldName);
        Assert.AreEqual("y2000_nl", query.Select.Fields[1].FieldName);

        var measure = AssertAccessMethod(query.Select.Fields[1].Expression);
        Assert.IsInstanceOfType<IdentifierNode>(measure.Arguments.Args[0]);
        Assert.IsInstanceOfType<AndNode>(measure.FilterExpression);
    }

    [TestMethod]
    public void Pivot_WithNullValue_ShouldLowerToIsNullPredicate()
    {
        var root = Parse("""
                         pivot #sales.orders()
                         on Quarter in (null as Missing)
                         using Count(*) as Orders
                         """);

        var query = GetSingleQuery(root);
        var measure = AssertAccessMethod(query.Select.Fields[0].Expression);

        Assert.IsInstanceOfType<AllColumnsNode>(measure.Arguments.Args[0]);
        Assert.IsInstanceOfType<IsNullNode>(measure.FilterExpression);
    }

    [TestMethod]
    public void Pivot_WithExplicitMeasureFilter_ShouldCombineWithPivotPredicate()
    {
        var root = Parse("""
                         pivot #sales.orders()
                         on Quarter in ('Q1' as Q1)
                         using Sum(Amount) filter (where Amount > 0) as Sales
                         """);

        var query = GetSingleQuery(root);
        var measure = AssertAccessMethod(query.Select.Fields[0].Expression);

        Assert.HasCount(1, measure.Arguments.Args);
        Assert.IsInstanceOfType<IdentifierNode>(measure.Arguments.Args[0]);
        var filter = Assert.IsInstanceOfType<AndNode>(measure.FilterExpression);
        Assert.IsInstanceOfType<GreaterNode>(filter.Left);
        Assert.IsInstanceOfType<EqualityNode>(filter.Right);
    }

    [TestMethod]
    public void Pivot_LexerShouldProducePivotToken()
    {
        var lexer = new Lexer("pivot #sales.orders() on Quarter in ('Q1') using Sum(Amount)", true);

        var token = lexer.Next();

        Assert.AreEqual(Tokens.TokenType.Pivot, token.TokenType);
        Assert.AreEqual("pivot", token.Value);
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

    private static AccessMethodNode AssertAccessMethod(Node node)
    {
        Assert.IsInstanceOfType<AccessMethodNode>(node);
        return (AccessMethodNode)node;
    }

}
