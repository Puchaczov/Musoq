using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PreLogicalNormalizerTests
{
    [TestMethod]
    public void Normalize_WhenDistinctQueryIsUsed_ShouldRunThroughTraceableBoundary()
    {
        var root = Parse("select distinct Name from #A.Entities()");

        var result = new PreLogicalNormalizer().Normalize(root);

        Assert.AreSame(root, result.InitialRoot);
        Assert.HasCount(2, result.Trace.Entries);
        Assert.AreEqual(OptimizationStage.PreLogicalNormalization, result.Trace.Entries[0].Stage);
        Assert.AreEqual("DistinctToGroupByNormalization", result.Trace.Entries[0].PassName);
        Assert.IsTrue(result.Trace.Entries[0].IsChanged);
        Assert.AreEqual("SubqueryToCteNormalization", result.Trace.Entries[1].PassName);
        Assert.IsFalse(result.Trace.Entries[1].IsChanged);

        var statements = (StatementsArrayNode)result.NormalizedRoot.Expression;
        var query = statements.Statements.Single().Node switch
        {
            SingleSetNode singleSet => singleSet.Query,
            QueryNode queryNode => queryNode,
            var node => throw new AssertFailedException($"Expected normalized query node, got {node.GetType().Name}.")
        };
        Assert.IsNotNull(query.GroupBy);
    }

    [TestMethod]
    public void Normalize_WhenScalarSubqueryUsesSingleSetWrapper_ShouldRewriteAsSubqueryPass()
    {
        var innerQuery = CreateQuery(
            new FieldNode(new IntegerNode(7), 0, "Value"),
            "Inner",
            "i");
        var outerQuery = CreateQuery(
            new FieldNode(new ScalarSubqueryNode(new SingleSetNode(innerQuery)), 0, "Value"),
            "Outer",
            "o");
        var root = new RootNode(new StatementsArrayNode([new StatementNode(new SingleSetNode(outerQuery))]));

        var result = new PreLogicalNormalizer().Normalize(root);

        Assert.HasCount(2, result.Trace.Entries);
        Assert.AreEqual("SubqueryToCteNormalization", result.Trace.Entries[1].PassName);
        Assert.IsTrue(result.Trace.Entries[1].IsChanged);

        var statements = (StatementsArrayNode)result.NormalizedRoot.Expression;
        var normalizedStatement = statements.Statements.Single().Node;
        Assert.IsInstanceOfType<CteExpressionNode>(normalizedStatement);
    }

    [TestMethod]
    public void Normalize_WhenNegatedPredicateSubqueryIsInExpression_ShouldUseDirectNullCheck()
    {
        var root = Parse("""
            select not exists (
                select b.City from #B.entities() b
                where b.Country = 'MISSING'
            ) as Missing
            from #A.entities() a
            """);

        var result = new PreLogicalNormalizer().Normalize(root);
        var normalized = result.NormalizedRoot.ToString();

        Assert.IsTrue(result.Trace.Entries.Single(static entry => entry.PassName == "SubqueryToCteNormalization").IsChanged);
        Assert.Contains("is null", normalized);
        Assert.IsFalse(normalized.Contains("not (", System.StringComparison.OrdinalIgnoreCase), normalized);
    }

    [TestMethod]
    public void Normalize_WhenSubqueriesAreRewritten_ShouldExposeLogicalOwnershipFacts()
    {
        var root = Parse("""
            select a.City, (
                select b.City from #B.entities() b
                where b.Country = 'FRANCE'
            ) as MatchCity
            from #A.entities() a
            where exists (
                select c.City from #C.entities() c
                where c.Country = a.Country
            )
            """);

        var result = new PreLogicalNormalizer().Normalize(root);
        var facts = result.LogicalSubqueryFacts;

        Assert.IsTrue(facts.Any(static fact => fact is
        {
            CteName: "_sq_1",
            Kind: LogicalSubqueryFormKind.Predicate,
            IsCorrelated: true
        }));
        Assert.IsTrue(facts.Any(static fact => fact is
        {
            CteName: "_sq_2",
            Kind: LogicalSubqueryFormKind.Scalar,
            IsCorrelated: false
        }));
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        var parser = new Musoq.Parser.Parser(lexer);
        return parser.ComposeAll();
    }

    private static QueryNode CreateQuery(FieldNode field, string variableName, string alias)
    {
        return new QueryNode(
            new SelectNode([field]),
            new InMemoryTableFromNode(variableName, alias, typeof(object)),
            null,
            null,
            null,
            null,
            null);
    }
}
