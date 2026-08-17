using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tests.Schema.Generic;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class ApplyJoinPipelineIntegrationTests : GenericEntityTestBase
{
    [TestMethod]
    public void WhenCrossApplyFollowedByRightOuterJoin_ShouldPreserveApplyAndJoinOperatorsInBothPlans()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            right outer join #schema.third() c on a.Country = c.Country";

        var secondSource = new[]
        {
            new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "Country1", Money = 1000, Month = "January" }
        };

        var thirdSource = new[]
        {
            new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "Country1", Money = 5000, Month = "March" },
            new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "NoMatch", Money = 6000, Month = "April" }
        };

        var buildItems = CreateApplyJoinBuildItems(query, secondSource, thirdSource);

        Assert.IsNotNull(buildItems.RequireLogicalPlan());
        Assert.IsNotNull(buildItems.RequirePhysicalPlan());

        var logicalPrinted = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        StringAssert.Contains(logicalPrinted, "Apply [Cross]");
        StringAssert.Contains(logicalPrinted, "Join [RightOuter]");

        var logicalApplyProject = PipelinePlanAssertions.FindLogicalApplyProject(buildItems.RequireLogicalPlan());
        var logicalApply = (ApplyNode)logicalApplyProject.Input;
        Assert.AreEqual(ApplyKind.Cross, logicalApply.Kind);
        Assert.IsInstanceOfType<SchemaScanNode>(logicalApply.Right);
        AssertCountryLateralArgument((SchemaScanNode)logicalApply.Right);
        PipelinePlanAssertions.AssertFinalLogicalStatementUsesCteRef(buildItems.RequireLogicalPlan());

        var physicalPrinted = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());
        StringAssert.Contains(physicalPrinted, "PhysicalNestedLoopApply [Cross]");
        StringAssert.Contains(physicalPrinted, "PhysicalHashJoin [RightOuter]");

        var physicalApplyProject = PipelinePlanAssertions.FindPhysicalApplyProject(buildItems.RequirePhysicalPlan());
        var physicalApply = (PhysicalNestedLoopApplyNode)physicalApplyProject.Input;
        Assert.AreEqual(ApplyKind.Cross, physicalApply.Kind);
        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(physicalApply.Right);

        AssertCountryLateralArgument((PhysicalSchemaScanNode)physicalApply.Right);
        PipelinePlanAssertions.AssertFinalPhysicalStatementUsesCteRef(buildItems.RequirePhysicalPlan());
    }

    [TestMethod]
    public void WhenOuterApplyFollowedByInnerJoin_ShouldPreserveApplyAndJoinOperatorsInBothPlans()
    {
        const string query = @"
            select a.City, t.Money, c.Month
            from #schema.first() a
            outer apply #schema.second(a.Country) t
            inner join #schema.third() c on a.Country = c.Country";

        var secondSource = new[]
        {
            new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "Country1", Money = 1000, Month = "January" }
        };

        var thirdSource = new[]
        {
            new CrossApplyUnusedAliasTests.CrossApplyClass2 { Country = "Country1", Money = 5000, Month = "March" }
        };

        var buildItems = CreateApplyJoinBuildItems(query, secondSource, thirdSource);

        Assert.IsNotNull(buildItems.RequireLogicalPlan());
        Assert.IsNotNull(buildItems.RequirePhysicalPlan());

        var logicalPrinted = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        StringAssert.Contains(logicalPrinted, "Apply [Outer]");
        StringAssert.Contains(logicalPrinted, "Join [Inner]");

        var logicalApplyProject = PipelinePlanAssertions.FindLogicalApplyProject(buildItems.RequireLogicalPlan());
        var logicalApply = (ApplyNode)logicalApplyProject.Input;
        Assert.AreEqual(ApplyKind.Outer, logicalApply.Kind);
        Assert.IsInstanceOfType<SchemaScanNode>(logicalApply.Right);
        AssertCountryLateralArgument((SchemaScanNode)logicalApply.Right);
        PipelinePlanAssertions.AssertFinalLogicalStatementUsesCteRef(buildItems.RequireLogicalPlan());

        var physicalPrinted = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());
        StringAssert.Contains(physicalPrinted, "PhysicalNestedLoopApply [Outer]");
        StringAssert.Contains(physicalPrinted, "PhysicalHashJoin [Inner]");

        var physicalApplyProject = PipelinePlanAssertions.FindPhysicalApplyProject(buildItems.RequirePhysicalPlan());
        var physicalApply = (PhysicalNestedLoopApplyNode)physicalApplyProject.Input;
        Assert.AreEqual(ApplyKind.Outer, physicalApply.Kind);
        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(physicalApply.Right);
        AssertCountryLateralArgument((PhysicalSchemaScanNode)physicalApply.Right);
        PipelinePlanAssertions.AssertFinalPhysicalStatementUsesCteRef(buildItems.RequirePhysicalPlan());
    }

    private BuildItems CreateApplyJoinBuildItems(
        string query,
        CrossApplyUnusedAliasTests.CrossApplyClass2[] secondSource,
        CrossApplyUnusedAliasTests.CrossApplyClass2[] thirdSource)
    {
        var firstSource = new[]
        {
            new CrossApplyUnusedAliasTests.CrossApplyClass1 { City = "City1", Country = "Country1", Population = 100 }
        };

        return CreateBuildItems(
            query,
            firstSource,
            secondSource,
            thirdSource,
            filterSecondRowsSource: static (parameters, source) =>
                source.Filter(row => (string)row.Country == RequireParameter<string>(parameters, 0)).ToArray());
    }

    private static void AssertCountryLateralArgument(SchemaScanNode scan)
    {
        Assert.AreEqual("second", scan.MethodName);
        Assert.HasCount(1, scan.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(scan.Arguments[0]);

        var argument = (ColumnRef)scan.Arguments[0];
        Assert.AreEqual("a", argument.Alias);
        Assert.AreEqual("Country", argument.ColumnName);
    }

    private static void AssertCountryLateralArgument(PhysicalSchemaScanNode scan)
    {
        Assert.AreEqual("second", scan.MethodName);
        Assert.HasCount(1, scan.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(scan.Arguments[0]);

        var argument = (ColumnRef)scan.Arguments[0];
        Assert.AreEqual("a", argument.Alias);
        Assert.AreEqual("Country", argument.ColumnName);
    }
}