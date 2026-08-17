using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Targets.CSharpClr.Optimization.Codegen;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tests.Schema.Basic;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RuleBasedSkippedOptimizationBaselineTests : BasicEntityTestBase
{
    [TestMethod]
    public void ProjectionPruning_WhenAggregateBoundaryIsBetweenProjects_ShouldRemainBlocked()
    {
        var scan = CreateValuesScan("e", ("City", typeof(string)), ("Name", typeof(string)));
        var aggregate = new PhysicalSingleKeyAggregateNode(
            new ColumnRef("e", "City", typeof(string)),
            "City",
            typeof(string),
            Array.Empty<AggregateBinding>(),
            scan);
        var project = new PhysicalProjectNode(
            [
                new ProjectedField(
                    "City",
                    new ColumnRef("aggregate", "City", typeof(string)),
                    0)
            ],
            aggregate);

        var result = new PhysicalOptimizer().Optimize(
            project,
            CreateEmptyProperties(),
            new CompilationOptions(),
            ConservativeTestPlanningShapeResolver.Instance);
        var projectionTrace = result.Trace.Entries.Single(static entry => entry.PassName == "ProjectionPruning");

        Assert.AreSame(project, result.OptimizedPlan);
        Assert.IsFalse(projectionTrace.IsChanged);
        Assert.Contains("No simple projection chains were safe to prune.", projectionTrace.Reason);
    }

    [TestMethod]
    public void RowWidthPruning_WhenDistinctAndSetBoundariesHaveCandidates_ShouldApplyBoth()
    {
        var result = RowWidthPruningPlanner.Plan(
        [
            new BoundaryRowShapePlan(
                "distinct:0",
                BoundaryRowShapeKind.Distinct,
                ["e.City", "e.Payload"],
                ["e.City"],
                [],
                ["e.Payload"],
                PlanningConfidence.Medium,
                "Distinct boundary has a future payload pruning opportunity."),
            new BoundaryRowShapePlan(
                "set:0",
                BoundaryRowShapeKind.SetOperation,
                ["left.City", "left.Payload", "right.City", "right.Payload"],
                ["left.City", "right.City"],
                [],
                ["left.Payload", "right.Payload"],
                PlanningConfidence.Medium,
                "Set operation boundary has a future symmetric pruning opportunity.")
        ]);

        var distinct = result.Plans.Single(static plan => plan.Kind == BoundaryRowShapeKind.Distinct);
        var set = result.Plans.Single(static plan => plan.Kind == BoundaryRowShapeKind.SetOperation);

        Assert.HasCount(2, result.Plans);
        Assert.AreEqual(RowWidthPruningStrategy.Applied, distinct.Strategy);
        CollectionAssert.AreEqual(new[] { "e.Payload" }, distinct.PrunedColumns);
        Assert.AreEqual(RowWidthPruningStrategy.Applied, set.Strategy);
        CollectionAssert.AreEqual(new[] { "left.Payload", "right.Payload" }, set.PrunedColumns);
        Assert.Contains("symmetric arm columns", set.Reason);
    }

    [TestMethod]
    public void PlanningText_WhenAggregateBoundaryHasDroppableColumns_ShouldExposeAppliedRowWidth()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            "select e.City, Count(e.Name) as NameCount from #A.Entities() e group by e.City order by NameCount desc");
        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("BoundaryRowShapes", planningText);
        Assert.Contains("Aggregate", planningText);
        Assert.Contains("RowWidthPruning", planningText);
        Assert.Contains("Aggregate -> Applied", planningText);
        Assert.Contains("aggregate input-only columns", planningText);
    }

    [TestMethod]
    public void PlanningText_WhenSubqueryRequiresOuterJoinFallback_ShouldExposeSkippedNormalizationFamily()
    {
        var buildItems = CreateBuildItems<BasicEntity>(
            """
            WITH matched AS (
                SELECT a.City, a.Country,
                       CASE
                           WHEN EXISTS (
                               SELECT b.City FROM #B.entities() b
                               WHERE b.Country = a.Country
                           )
                           THEN 'Y'
                           ELSE 'N'
                       END AS HasMatch
                FROM #A.entities() a
            )
            SELECT m.City, m.HasMatch
            FROM matched m
            ORDER BY m.City
            """);
        var planningText = buildItems.RequirePlanningText();

        Assert.Contains("SubqueryStrategy", planningText);
        Assert.Contains("PredicateHashMark", planningText);
    }

    [TestMethod]
    public void HelperExtractionReadability_WhenInlineBlockHasNoMetadata_ShouldRemainNoOp()
    {
        var initial = SyntaxFactory.ParseCompilationUnit(
            "public class Generated { public void Run() { var value = 1; if (value > 0) { value = value + 1; value = value + 2; } System.Console.WriteLine(value); } }");

        var result = new HelperExtractionReadabilityPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.CodegenReadability));

        Assert.IsFalse(result.IsChanged);
        Assert.AreEqual(initial.ToFullString(), result.Plan.ToFullString());
        Assert.Contains("No metadata-approved helper extraction candidates", result.Reason);
        Assert.IsFalse(result.Plan.DescendantNodes().Any(static node =>
            node.HasAnnotations(HelperExtractionReadabilityPass.HelperExtractionAnnotationKind)));
    }

    private static PhysicalValuesScanNode CreateValuesScan(
        string alias,
        params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var i = 0; i < columns.Length; i++)
            schemaColumns[i] = new ColumnSchema(columns[i].Name, columns[i].Type, i);

        return new PhysicalValuesScanNode(alias, [], new OutputSchema(schemaColumns));
    }

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }

    private static string FindRepositoryFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        Assert.Fail($"Could not locate repository file '{fileName}' from '{AppContext.BaseDirectory}'.");
        throw new InvalidOperationException();
    }
}
