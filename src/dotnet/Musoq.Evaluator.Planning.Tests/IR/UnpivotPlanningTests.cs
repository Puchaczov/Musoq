using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class UnpivotPlanningTests
{
    [TestMethod]
    public void Build_WhenUnpivotHasKeepFields_ShouldInferOutputSchemaInOrder()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Country as Country");

        var unpivot = FindLogicalUnpivot(buildItems.RequireLogicalPlan());

        Assert.AreEqual("__unpivot", unpivot.Alias);
        Assert.AreEqual("Metric", unpivot.NameColumn);
        Assert.AreEqual("Amount", unpivot.ValueColumn);
        Assert.AreEqual(2, unpivot.Entries.Count);
        Assert.AreEqual("Population", unpivot.Entries[0].NameValue);
        Assert.AreEqual("Money", unpivot.Entries[1].NameValue);

        AssertOutputColumn(unpivot, 0, "Country", typeof(string));
        AssertOutputColumn(unpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(unpivot, 2, "Amount", typeof(decimal));
    }

    [TestMethod]
    public void Build_WhenUnpivotMixesIntegerAndNull_ShouldInferNullableValueColumn()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.Id as Id, null as Missing) using Value keep s.Name as Name");

        var unpivot = FindLogicalUnpivot(buildItems.RequireLogicalPlan());

        AssertOutputColumn(unpivot, 0, "Name", typeof(string));
        AssertOutputColumn(unpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(unpivot, 2, "Value", typeof(int?));
    }

    [TestMethod]
    public void Build_WhenUnpivotUsesNullableExpressionOnly_ShouldInferNullableValueColumn()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.NullableValue as NullableValue) using Value keep s.Name as Name");

        var unpivot = FindLogicalUnpivot(buildItems.RequireLogicalPlan());

        AssertOutputColumn(unpivot, 0, "Name", typeof(string));
        AssertOutputColumn(unpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(unpivot, 2, "Value", typeof(int?));
    }

    [TestMethod]
    public void Build_WhenUnpivotMixesNullableAndNonNullableIntegers_ShouldInferNullableValueColumn()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.NullableValue as NullableValue, s.Id as Id) using Value keep s.Name as Name");

        var unpivot = FindLogicalUnpivot(buildItems.RequireLogicalPlan());

        AssertOutputColumn(unpivot, 0, "Name", typeof(string));
        AssertOutputColumn(unpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(unpivot, 2, "Value", typeof(int?));
    }

    [TestMethod]
    public void Build_WhenUnpivotMixesIntegerAndDecimal_ShouldInferDecimalValueColumn()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.Id as Id, s.Population as Population) using Value keep s.Name as Name");

        var unpivot = FindLogicalUnpivot(buildItems.RequireLogicalPlan());

        AssertOutputColumn(unpivot, 0, "Name", typeof(string));
        AssertOutputColumn(unpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(unpivot, 2, "Value", typeof(decimal));
    }

    [TestMethod]
    public void Build_WhenUnpivotValueTypesAreIncompatible_ShouldThrowDiagnosticException()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (1 as Id, 'Name' as Name) using Value");
        var errors = buildItems.DiagnosticContext.Errors.ToArray();

        Assert.AreEqual(1, errors.Length);
        StringAssert.Contains(errors[0].Message, "UNPIVOT value column 'Value' mixes incompatible types");
    }

    [TestMethod]
    public void Print_WhenUnpivotIsPlanned_ShouldShowLogicalAndPhysicalNodes()
    {
        var buildItems = PlanOnlyBuildItems.Create(
            "unpivot #A.Entities() s on Metric in (s.Population as Population, s.Money as Money) using Amount keep s.Country as Country order by Country, Metric");

        var logicalText = LogicalPlanPrinter.Print(buildItems.RequireLogicalPlan());
        var physicalText = PhysicalPlanPrinter.Print(buildItems.RequirePhysicalPlan());

        StringAssert.Contains(logicalText, "Unpivot [name: Metric; value: Amount; entries: s.Population as Population, s.Money as Money; keep: s.Country as Country] as __unpivot");
        StringAssert.Contains(physicalText, "PhysicalUnpivot [name: Metric; value: Amount; entries: s.Population as Population, s.Money as Money; keep: s.Country as Country] as __unpivot");

        var physicalUnpivot = FindPhysicalUnpivot(buildItems.RequirePhysicalPlan());
        AssertOutputColumn(physicalUnpivot, 0, "Country", typeof(string));
        AssertOutputColumn(physicalUnpivot, 1, "Metric", typeof(string));
        AssertOutputColumn(physicalUnpivot, 2, "Amount", typeof(decimal));
    }

    private static UnpivotNode FindLogicalUnpivot(LogicalNode node)
    {
        if (node is UnpivotNode unpivot)
            return unpivot;

        foreach (var child in node.Children)
        {
            var found = TryFindLogicalUnpivot(child);
            if (found != null)
                return found;
        }

        Assert.Fail("Expected logical plan to contain an UnpivotNode.");
        throw new InvalidOperationException();
    }

    private static UnpivotNode? TryFindLogicalUnpivot(LogicalNode node)
    {
        if (node is UnpivotNode unpivot)
            return unpivot;

        return node.Children
            .Select(TryFindLogicalUnpivot)
            .FirstOrDefault(found => found != null);
    }

    private static PhysicalUnpivotNode FindPhysicalUnpivot(PhysicalNode node)
    {
        if (node is PhysicalUnpivotNode unpivot)
            return unpivot;

        foreach (var child in node.Children)
        {
            var found = TryFindPhysicalUnpivot(child);
            if (found != null)
                return found;
        }

        Assert.Fail("Expected physical plan to contain a PhysicalUnpivotNode.");
        throw new InvalidOperationException();
    }

    private static PhysicalUnpivotNode? TryFindPhysicalUnpivot(PhysicalNode node)
    {
        if (node is PhysicalUnpivotNode unpivot)
            return unpivot;

        return node.Children
            .Select(TryFindPhysicalUnpivot)
            .FirstOrDefault(found => found != null);
    }

    private static void AssertOutputColumn(UnpivotNode node, int index, string name, Type type)
    {
        var column = node.OutputSchema.Columns[index];
        Assert.AreEqual(name, column.Name);
        Assert.AreEqual(type, column.Type);
        Assert.AreEqual(index, column.Index);
    }

    private static void AssertOutputColumn(PhysicalUnpivotNode node, int index, string name, Type type)
    {
        var column = node.OutputSchema.Columns[index];
        Assert.AreEqual(name, column.Name);
        Assert.AreEqual(type, column.Type);
        Assert.AreEqual(index, column.Index);
    }
}
