using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OrderByTests
{
    private const string InlineOrdinalQuery =
        "select t.Name from values { (Name: 'a'), (Name: 'A') } t order by t.Name";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0031")]
    public void Current_P2_0031_StringOrderIsOrdinal()
    {
        AssertInlineOrdinalResult("Name");
    }

    [TestMethod]
    public void InlineStringOrder_ShouldBeOrdinalWithEstablishedProjectionName()
    {
        AssertInlineOrdinalResult("Name");
    }

    private void AssertInlineOrdinalResult(string outputName)
    {
        using var compiled = CreateAndRunVirtualMachine(InlineOrdinalQuery,
            new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, (outputName, typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { "A" }, new object?[] { "a" });
    }
}
