using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class ExecutionPlanOperatorCatalogTests
{
    [TestMethod]
    public void Create_ShouldAssignStableIdsInPrintedPlanOrder()
    {
        var plan = new ExecutionPlan(
            "compiled",
            [],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(
                    new ExecutionVariable("results", typeof(Table)),
                    new GeneratedRowShape("ResultRow", [])),
                new ExecutionReturnTable(new ExecutionVariable("results", typeof(Table)))
            ]));

        var catalog = ExecutionPlanOperatorCatalog.Create(plan);

        Assert.AreEqual("op1", catalog.Operators[0].Id);
        Assert.AreEqual("ExecutionPlan", catalog.Operators[0].NodeKind);
        Assert.IsTrue(catalog.Operators.Any(static descriptor => descriptor.NodeKind == "CreateTable"));
        Assert.IsTrue(catalog.Operators.Any(static descriptor => descriptor.NodeKind == "ReturnTable"));
        StringAssert.Contains(catalog.AnnotatedExecutionPlanText, "[op1] ExecutionPlan [compiled]");
    }

    [TestMethod]
    public void Create_FromPlan_ShouldMapNodesThroughStructuredChildBlocks()
    {
        var first = new ExecutionCreateTable(
            new ExecutionVariable("results", typeof(Table)),
            new GeneratedRowShape("ResultRow", []));
        var second = new ExecutionReturnTable(new ExecutionVariable("results", typeof(Table)));
        var plan = new ExecutionPlan("structured", [], new ExecutionBlock([first, second]));

        var catalog = ExecutionPlanOperatorCatalog.Create(plan);

        Assert.HasCount(2, catalog.NodeOperators);
        Assert.IsTrue(catalog.TryGetDescriptor(first, out var firstDescriptor));
        Assert.IsTrue(catalog.TryGetDescriptor(second, out var secondDescriptor));
        Assert.AreEqual("CreateTable", firstDescriptor.NodeKind);
        Assert.AreEqual("ReturnTable", secondDescriptor.NodeKind);
        Assert.AreEqual("op4", firstDescriptor.Id);
        Assert.AreEqual("op5", secondDescriptor.Id);
    }

    [TestMethod]
    public void Create_WhenPlanContainsBlankLines_DoesNotAssignIdsToBlankLines()
    {
        var catalog = ExecutionPlanOperatorCatalog.Create("""
            ExecutionPlan [compiled]

              Body
                ReturnTable [results]
            """);

        Assert.AreEqual(3, catalog.Operators.Count);
        CollectionAssert.AreEqual(
            new[] { "op1", "op2", "op3" },
            catalog.Operators.Select(static descriptor => descriptor.Id).ToArray());
    }
}
