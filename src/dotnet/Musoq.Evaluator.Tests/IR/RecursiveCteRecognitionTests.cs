using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using System.Linq;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RecursiveCteRecognitionTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    private const string CounterQuery =
        "with recursive counter (Value) as (" +
        "select Value from values {{ Value: 1 }} seed " +
        "union all select c.Value + 1 from counter c where c.Value < 3) " +
        "select Value from counter";

    [TestMethod]
    public void Counter_ShouldProduceDedicatedLogicalAndPhysicalNodes()
    {
        var buildItems = PlanOnlyBuildItems.Create(CounterQuery);

        var logical = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        var physical = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("RecursiveCte [counter] [All]", logical);
        Assert.Contains("  Anchor", logical);
        Assert.Contains("  RecursiveMember", logical);
        Assert.Contains("PhysicalRecursiveCte [counter] [All]", physical);
    }

    [TestMethod]
    public void UnionModes_ShouldRemainExplicitInBothPlans()
    {
        AssertPlanUnionKind("union", "FullRow");
        AssertPlanUnionKind("union (Value)", "Keyed: Value");
    }

    [TestMethod]
    public void EarlierOrdinaryCte_ShouldBindBeforeRecursiveDefinition()
    {
        const string query =
            "with recursive seed (Value) as (select Value from values {{ Value: 1 }} source), " +
            "counter (Value) as (select Value from seed union all " +
            "select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter";

        var buildItems = PlanOnlyBuildItems.Create(query);
        var physical = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("Definition [seed]", physical);
        Assert.Contains("PhysicalRecursiveCte [counter] [All]", physical);
    }

    [TestMethod]
    public void WithRecursive_WhenNoDefinitionIsRecursive_ShouldUseOrdinaryCteExecution()
    {
        const string query =
            "with recursive items (Value) as (select Value from values {{ Value: 7 }} seed) " +
            "select Value from items";

        var vm = CreateAndRunVirtualMachine(query, CreateSingleSource());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.HasCount(1, table);
        Assert.AreEqual(7, table.Single().Values[0]);
    }

    [TestMethod]
    public void KeyedUnion_ShouldCanonicalizeKeyToExportedColumnName()
    {
        const string query =
            "with recursive counter (Id) as (" +
            "select Value from values {{ Value: 1 }} seed union (id) " +
            "select c.Id + 1 from counter c where c.Id < 3) select Id from counter";

        var buildItems = PlanOnlyBuildItems.Create(query);
        var logical = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        var physical = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains("RecursiveCte [counter] [Keyed: Id]", logical);
        Assert.Contains("PhysicalRecursiveCte [counter] [Keyed: Id]", physical);
    }

    [TestMethod]
    public void KeyedUnion_ShouldCarryResolvedIdentityOrdinalThroughLogicalAndPhysicalPlans()
    {
        const string query =
            "with recursive counter (Payload, Id) as (" +
            "select Label, Value from values {{ Label: 'seed', Value: 1 }} seed union (id) " +
            "select c.Payload, c.Id + 1 from counter c where c.Id < 3) " +
            "select Payload, Id from counter";

        var buildItems = PlanOnlyBuildItems.Create(query);
        var logicalCte = (Musoq.Evaluator.IR.Logical.Nodes.CteNode)buildItems.RequireLogicalPlan();
        var logicalRecursive = (Musoq.Evaluator.IR.Logical.Nodes.RecursiveCteNode)
            logicalCte.Definitions.Single().Plan;
        var physicalCte = (Musoq.Evaluator.IR.Physical.Nodes.PhysicalCteNode)buildItems.RequirePhysicalPlan();
        var physicalRecursive = (Musoq.Evaluator.IR.Physical.Nodes.PhysicalRecursiveCteNode)
            physicalCte.Definitions.Single().Plan;

        CollectionAssert.AreEqual(new[] { "Id" }, logicalRecursive.Keys);
        CollectionAssert.AreEqual(new[] { 1 }, logicalRecursive.IdentityFieldIndexes);
        CollectionAssert.AreEqual(new[] { "Id" }, physicalRecursive.Keys);
        CollectionAssert.AreEqual(new[] { 1 }, physicalRecursive.IdentityFieldIndexes);
    }

    [TestMethod]
    public void KeyedUnion_WhenKeyOnlyMatchesAnchorExpression_ShouldStopBeforePlanningWithMq3001()
    {
        const string query =
            "with recursive counter (Id) as (" +
            "select Value from values {{ Value: 1 }} seed union (Value) " +
            "select c.Id + 1 from counter c where c.Id < 3) select Id from counter";

        var buildItems = PlanOnlyBuildItems.Create(query);
        var errors = buildItems.DiagnosticContext.Errors.ToArray();

        Assert.IsNotEmpty(errors);
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, errors[0].Code);
        Assert.Contains("Unknown column 'Value'", errors[0].Message);
        Assert.IsNull(buildItems.LogicalPlan);
        Assert.IsNull(buildItems.PhysicalPlan);
    }

    private static void AssertPlanUnionKind(string separator, string expectedKind)
    {
        var query =
            "with recursive counter (Value) as (" +
            "select Value from values {{ Value: 1 }} seed " + separator +
            " select c.Value + 1 from counter c where c.Value < 3) " +
            "select Value from counter";
        var buildItems = PlanOnlyBuildItems.Create(query);

        var logical = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        var physical = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        Assert.Contains($"RecursiveCte [counter] [{expectedKind}]", logical);
        Assert.Contains($"PhysicalRecursiveCte [counter] [{expectedKind}]", physical);
    }
}
