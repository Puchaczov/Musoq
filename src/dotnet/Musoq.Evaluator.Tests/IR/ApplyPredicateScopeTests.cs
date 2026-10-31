using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ApplyPredicateScopeTests
{
    [TestMethod]
    public void AcceptedPredicateInOneSetBranch_ShouldNotSuppressAnotherBranchGuard()
    {
        var first = new ApplyNode(ApplyKind.Cross, Scan("a", "first"), Scan("b", "first-right"));
        var second = new ApplyNode(ApplyKind.Cross, Scan("a", "second"), Scan("b", "second-right"));
        var predicate = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("a", "Category", typeof(string)),
            new Literal("alpha", typeof(string)), typeof(bool));
        var plan = new SetOperationNode(SetOpKind.UnionAll, new FilterNode(predicate, first),
            new FilterNode(predicate, second), []);
        var accepted = new SourcePredicateComparison(SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Category")), new SourcePredicateLiteral("alpha"));
        var identity = new SourceIdentity("#test", "items", "first", "a");
        var result = ApplyPredicateMovementPlanner.Plan(plan, new Dictionary<string, SourcePlanProperties>(),
            new Dictionary<string, SourcePlanResult>
            {
                ["first"] = new()
                {
                    AcceptedPredicate = accepted,
                    ExecutionPlan = SourceExecutionPlan.Empty(identity) with { AcceptedPredicate = accepted }
                }
            },
            new Dictionary<string, SourcePredicatePlan>
            {
                ["first"] = new("first", "a", new WhereNode(new BooleanNode(true)), [predicate],
                    "accepted by first source", PlanningConfidence.High)
            });
        Assert.HasCount(1, result.Plans);
        Assert.AreSame(second, result.Plans[0].Apply);
        Assert.AreEqual(1, result.Decisions.Count(decision => decision.Outcome == "AlreadySourcePushed"));
    }

    [TestMethod]
    public void AmbiguousAliasWithinOneBlock_ShouldRemainConservative()
    {
        var apply = new ApplyNode(ApplyKind.Cross, Scan("a", "first"), Scan("a", "second"));
        var predicate = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("a", "Category", typeof(string)),
            new Literal("alpha", typeof(string)), typeof(bool));
        var result = ApplyPredicateMovementPlanner.Plan(new FilterNode(predicate, apply),
            new Dictionary<string, SourcePlanProperties>());
        Assert.IsEmpty(result.Plans);
        Assert.Contains("multiple source scopes", result.Decisions.Single().Reason);
    }

    private static SchemaScanNode Scan(string alias, string sourceId) => new("#test", "items", [], alias,
        new OutputSchema([new ColumnSchema("Category", typeof(string), 0)]), sourceId);
}
