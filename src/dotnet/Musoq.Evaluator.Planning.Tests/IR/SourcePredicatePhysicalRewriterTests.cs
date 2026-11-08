using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourcePredicatePhysicalRewriterTests
{
    private const string SourceContextId = "source-1";
    private const string Alias = "s";

    [TestMethod]
    [DataRow("cte")]
    [DataRow("join")]
    [DataRow("apply")]
    [DataRow("set")]
    public void AcceptedPredicate_ShouldBeRemovedThroughPhysicalContainer(string container)
    {
        var pushedPredicate = CreatePredicate(Alias);
        var filteredScan = new PhysicalFilterNode(
            pushedPredicate,
            CreateScan(Alias, SourceContextId));
        var physicalPlan = Wrap(container, filteredScan);

        var rewritten = SourcePredicatePhysicalRewriter.Rewrite(
            physicalPlan,
            CreateSourcePlanResults(),
            CreateSourcePredicatePlans(pushedPredicate));

        Assert.IsFalse(ContainsFilter(rewritten));
    }

    private static PhysicalNode Wrap(string container, PhysicalNode filteredScan)
    {
        return container switch
        {
            "cte" => new PhysicalCteNode(
                [new PhysicalCteDefinition("Filtered", filteredScan)],
                CreateScan("q", "query-source")),
            "join" => new PhysicalHashJoinNode(
                JoinKind.Inner,
                [new ColumnRef(Alias, "Category", typeof(string))],
                [new ColumnRef("r", "Category", typeof(string))],
                null,
                filteredScan,
                CreateScan("r", "right-source")),
            "apply" => new PhysicalNestedLoopApplyNode(
                ApplyKind.Cross,
                filteredScan,
                CreateScan("r", "right-source")),
            "set" => new PhysicalSetOperationNode(
                SetOpKind.Union,
                filteredScan,
                CreateScan("r", "right-source"),
                [0],
                [typeof(string)]),
            _ => throw new AssertFailedException($"Unsupported container '{container}'.")
        };
    }

    private static Dictionary<string, SourcePlanResult> CreateSourcePlanResults()
    {
        var identity = new SourceIdentity("#sp", "items", SourceContextId, Alias);
        var acceptedPredicate = CreateSourcePredicate();
        return new Dictionary<string, SourcePlanResult>
        {
            [SourceContextId] = new()
            {
                ExecutionPlan = SourceExecutionPlan.Empty(identity) with
                {
                    AcceptedPredicate = acceptedPredicate
                },
                AcceptedPredicate = acceptedPredicate
            }
        };
    }

    private static Dictionary<string, SourcePredicatePlan> CreateSourcePredicatePlans(
        IrExpression pushedPredicate)
    {
        return new Dictionary<string, SourcePredicatePlan>
        {
            [SourceContextId] = new(
                SourceContextId,
                Alias,
                new WhereNode(new BooleanNode(true)),
                [pushedPredicate],
                "test",
                PlanningConfidence.High)
        };
    }

    private static PhysicalSchemaScanNode CreateScan(string alias, string sourceContextId)
    {
        return new PhysicalSchemaScanNode(
            "#sp",
            "items",
            [],
            alias,
            [],
            [],
            new OutputSchema([new ColumnSchema("Category", typeof(string), 0)]),
            sourceContextId);
    }

    private static BinaryOp CreatePredicate(string alias)
    {
        return new BinaryOp(
            BinaryOpKind.Equal,
            new ColumnRef(alias, "Category", typeof(string)),
            new Literal("alpha", typeof(string)),
            typeof(bool));
    }

    private static SourcePredicateComparison CreateSourcePredicate()
    {
        return new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Category")),
            new SourcePredicateLiteral("alpha"));
    }

    private static bool ContainsFilter(PhysicalNode node)
    {
        if (node is PhysicalFilterNode)
            return true;

        foreach (var child in node.Children)
            if (ContainsFilter(child))
                return true;

        return false;
    }
}
