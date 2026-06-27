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
