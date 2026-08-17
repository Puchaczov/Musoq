using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParserSourceAliasProvenanceTests
{
    [TestMethod]
    public void ReferentialSourceName_ShouldAddressSubsequentPropertyApply()
    {
        var query = ParseSingleQuery("select item.Value from source cross apply source.Column item");
        var apply = GetApply(query);

        var source = Assert.IsInstanceOfType<InMemoryTableFromNode>(apply.Source);
        Assert.AreEqual("source", source.VariableName);

        var property = Assert.IsInstanceOfType<PropertyFromNode>(apply.With);
        Assert.AreEqual("source", property.SourceAlias);
        Assert.AreEqual("item", property.Alias);
    }

    [TestMethod]
    public void ExplicitSourceAlias_ShouldRemainTheRowMethodAddress()
    {
        var query = ParseSingleQuery("select item.Value from schema.first() first cross apply first.Column item");
        var apply = GetApply(query);

        var source = Assert.IsInstanceOfType<SchemaFromNode>(apply.Source);
        Assert.AreEqual("first", source.Alias);

        var property = Assert.IsInstanceOfType<PropertyFromNode>(apply.With);
        Assert.AreEqual("first", property.SourceAlias);
    }

    [TestMethod]
    public void CteReference_ShouldProvideANaturalApplyAddress()
    {
        var query = ParseSingleQuery(
            "with cteName as (select 1 from #schema.items()) select item.Value from cteName cross apply cteName.Column item");
        var apply = GetApply(query);

        var source = Assert.IsInstanceOfType<InMemoryTableFromNode>(apply.Source);
        Assert.AreEqual("cteName", source.VariableName);
        Assert.AreEqual("cteName", Assert.IsInstanceOfType<PropertyFromNode>(apply.With).SourceAlias);
    }

    [TestMethod]
    public void NewlyAliasedApplySource_ShouldAddressAChainedApply()
    {
        var query = ParseSingleQuery(
            "select item2.Value from source cross apply source.Column item cross apply item.Next item2");
        var outerApply = GetApply(query);

        var innerApply = Assert.IsInstanceOfType<ApplyFromNode>(outerApply.Source);
        var property = Assert.IsInstanceOfType<PropertyFromNode>(outerApply.With);
        Assert.AreEqual("item", property.SourceAlias);
        Assert.AreEqual("item2", property.Alias);

        Assert.IsInstanceOfType<InMemoryTableFromNode>(innerApply.Source);
    }

    [TestMethod]
    public void NestedQuerySourceScopes_ShouldRemainIndependent()
    {
        var query = ParseSingleQuery(
            "select outerItem.Value from outerSource cross apply (select innerSource.Value from innerSource) outerItem");

        var apply = GetApply(query);
        var outerSource = Assert.IsInstanceOfType<InMemoryTableFromNode>(apply.Source);
        Assert.AreEqual("outerSource", outerSource.VariableName);

        var derived = Assert.IsInstanceOfType<DerivedTableFromNode>(apply.With);
        Assert.AreEqual("outerItem", derived.Alias);
    }

    private static ApplyFromNode GetApply(QueryNode query)
    {
        var expression = Assert.IsInstanceOfType<ExpressionFromNode>(query.From);
        var apply = Assert.IsInstanceOfType<ApplyNode>(expression.Expression);
        return apply.Apply;
    }

    private static QueryNode ParseSingleQuery(string query)
    {
        var root = new Parser(new Lexer(query, true)).ComposeAll();
        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        var statement = statements.Statements[0].Node;
        if (statement is CteExpressionNode cte)
            statement = cte.OuterExpression;

        var set = Assert.IsInstanceOfType<SingleSetNode>(statement);
        return set.Query;
    }
}
