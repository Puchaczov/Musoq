using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class ParserFilterClauseTests
{
    [TestMethod]
    public void FilterOnCount_ShouldRewriteArgToCaseWhen()
    {
        var query = "select Count(Name) filter (where Name = 'ABBA') from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.AreEqual("Count", accessMethod.Name);
        Assert.AreEqual(1, accessMethod.ArgsCount);

        var caseNode = accessMethod.Arguments.Args[0] as CaseNode;
        Assert.IsNotNull(caseNode, "FILTER should rewrite the argument to a CaseNode");
        Assert.HasCount(1, caseNode.WhenThenPairs);
        Assert.IsInstanceOfType<WhenNode>(caseNode.WhenThenPairs[0].When);
        Assert.IsInstanceOfType<ThenNode>(caseNode.WhenThenPairs[0].Then);
        Assert.IsInstanceOfType<ElseNode>(caseNode.Else);
        var elseNode = (ElseNode)caseNode.Else;
        Assert.IsInstanceOfType<NullNode>(elseNode.Expression);
    }

    [TestMethod]
    public void RegularCount_ShouldNotHaveCaseWhenWrapper()
    {
        var query = "select Count(Name) from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.IsNotInstanceOfType<CaseNode>(accessMethod.Arguments.Args[0]);
    }

    [TestMethod]
    public void FilterCaseInsensitive_ShouldParse()
    {
        var query = "select Count(Name) FILTER (WHERE Name = 'ABBA') from #some.a()";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);
        var result = parser.ComposeAll();

        var statementsArray = result.Expression as StatementsArrayNode;
        Assert.IsNotNull(statementsArray);

        var singleSet = statementsArray.Statements[0].Node as SingleSetNode;
        Assert.IsNotNull(singleSet);

        var select = singleSet.Query.Select;
        var field = select.Fields[0];
        var accessMethod = field.Expression as AccessMethodNode;

        Assert.IsNotNull(accessMethod);
        Assert.AreEqual(1, accessMethod.ArgsCount);

        var caseNode = accessMethod.Arguments.Args[0] as CaseNode;
        Assert.IsNotNull(caseNode);
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

        var caseNode = accessMethod.Arguments.Args[0] as CaseNode;
        Assert.IsNotNull(caseNode);
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
        Assert.IsInstanceOfType<CaseNode>(countMethod.Arguments.Args[0]);

        var sumMethod = select.Fields[1].Expression as AccessMethodNode;
        Assert.IsNotNull(sumMethod);
        Assert.IsInstanceOfType<CaseNode>(sumMethod.Arguments.Args[0]);
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

        var caseNode = accessMethod.Arguments.Args[0] as CaseNode;
        Assert.IsNotNull(caseNode);
        Assert.HasCount(1, caseNode.WhenThenPairs);

        var whenNode = caseNode.WhenThenPairs[0].When as WhenNode;
        Assert.IsNotNull(whenNode);
        Assert.IsInstanceOfType<AndNode>(whenNode.Expression);
    }
}
