using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserFilterClauseTests
{
    [TestMethod]
    public void FilterOnCount_ShouldPreserveArgumentAndStorePredicate()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(Name) filter (where Name = 'ABBA') from #some.a()");

        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(1, accessMethod.ArgsCount);
        var identifierNode = accessMethod.Arguments.Args[0] as IdentifierNode;
        Assert.IsNotNull(identifierNode);
        Assert.AreEqual("Name", identifierNode.Name);
        AssertFilterPredicate<EqualityNode>(accessMethod, "Name = 'ABBA'");
    }

    [TestMethod]
    public void RegularCount_ShouldNotHaveFilterPredicate()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(Name) from #some.a()");

        Assert.IsFalse(accessMethod.HasFilter);
        Assert.IsNull(accessMethod.FilterExpression);
        Assert.IsNull(accessMethod.FilterExpressionText);
        Assert.IsNotInstanceOfType<CaseNode>(accessMethod.Arguments.Args[0]);
    }

    [TestMethod]
    public void FilterOnCountWildcard_ShouldKeepWildcardAndStorePredicate()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(*) filter (where Population > 200) from #some.a()");

        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(1, accessMethod.ArgsCount);
        Assert.IsInstanceOfType<AllColumnsNode>(accessMethod.Arguments.Args[0]);
        AssertFilterPredicate<GreaterNode>(accessMethod, "Population > 200");
    }

    [TestMethod]
    public void FilterOnCountWithoutArguments_ShouldStayArgumentlessAndStorePredicate()
    {
        var accessMethod = ParseFirstAccessMethod("select Count() filter (where Population > 200) from #some.a()");

        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(0, accessMethod.ArgsCount);
        AssertFilterPredicate<GreaterNode>(accessMethod, "Population > 200");
    }

    [TestMethod]
    public void FilterOnCountDistinct_ShouldPreserveDistinctArgumentAndStorePredicate()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(distinct City) filter (where Population > 200) from #some.a()");

        Assert.IsTrue(accessMethod.IsDistinct);
        Assert.AreEqual(1, accessMethod.ArgsCount);
        var identifierNode = accessMethod.Arguments.Args[0] as IdentifierNode;
        Assert.IsNotNull(identifierNode);
        Assert.AreEqual("City", identifierNode.Name);
        AssertFilterPredicate<GreaterNode>(accessMethod, "Population > 200");
    }

    [TestMethod]
    public void RegularCountWithoutArguments_ShouldStayArgumentless()
    {
        var accessMethod = ParseFirstAccessMethod("select Count() from #some.a()");

        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(0, accessMethod.ArgsCount);
    }

    [TestMethod]
    public void RegularCountWildcard_ShouldKeepWildcardArgument()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(*) from #some.a()");

        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(1, accessMethod.ArgsCount);
        Assert.IsInstanceOfType<AllColumnsNode>(accessMethod.Arguments.Args[0]);
    }

    [TestMethod]
    public void FilterCaseInsensitive_ShouldParse()
    {
        var accessMethod = ParseFirstAccessMethod("select Count(Name) FILTER (WHERE Name = 'ABBA') from #some.a()");

        Assert.AreEqual(1, accessMethod.ArgsCount);
        AssertFilterPredicate<EqualityNode>(accessMethod, "Name = 'ABBA'");
    }

    [TestMethod]
    public void FilterWithGroupBy_ShouldParse()
    {
        var query = "select Country, Count(City) filter (where Population > 200) from #some.a() group by Country";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        Assert.IsNotNull(result);

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        Assert.HasCount(2, select.Fields);

        var accessMethod = select.Fields[1].Expression as AccessMethodNode;
        Assert.IsNotNull(accessMethod);
        Assert.AreEqual("Count", accessMethod.Name);

        AssertFilterPredicate<GreaterNode>(accessMethod, "Population > 200");
    }

    [TestMethod]
    public void MultipleFilters_ShouldParse()
    {
        var query = "select Count(City) filter (where Population > 200), Sum(Population) filter (where Country = 'Poland') from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        Assert.HasCount(2, select.Fields);

        var countMethod = select.Fields[0].Expression as AccessMethodNode;
        Assert.IsNotNull(countMethod);
        AssertFilterPredicate<GreaterNode>(countMethod, "Population > 200");

        var sumMethod = select.Fields[1].Expression as AccessMethodNode;
        Assert.IsNotNull(sumMethod);
        AssertFilterPredicate<EqualityNode>(sumMethod, "Country = 'Poland'");
    }

    [TestMethod]
    public void FilterWithComplexCondition_ShouldParse()
    {
        var query = "select Count(City) filter (where Population > 100 and Country = 'Poland') from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var accessMethod = select.Fields[0].Expression as AccessMethodNode;
        Assert.IsNotNull(accessMethod);

        Assert.AreEqual(1, accessMethod.ArgsCount);
        Assert.IsInstanceOfType<IdentifierNode>(accessMethod.Arguments.Args[0]);
        AssertFilterPredicate<AndNode>(accessMethod, "Population > 100 and Country = 'Poland'");
    }

    private static AccessMethodNode ParseFirstAccessMethod(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var field = singleSet.Query.Select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;
        Assert.IsNotNull(accessMethod);
        return accessMethod;
    }

    private static void AssertFilterPredicate<TPredicate>(AccessMethodNode accessMethod, string expectedText)
        where TPredicate : Node
    {
        Assert.IsTrue(accessMethod.HasFilter);
        Assert.IsInstanceOfType<TPredicate>(accessMethod.FilterExpression);
        Assert.AreEqual(expectedText, accessMethod.FilterExpressionText);
    }
}
