using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionCoverageQueryTests
{
    [TestMethod]
    public void RecursiveTypedStorageAndFrames_ShouldMatchIndependentArithmeticReference()
    {
        using var compiled = CreateAndRunVirtualMachine(RecursiveFrameQuery, new[] { new CoverageRow(0, null, null) });
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Value", typeof(int)),
            ("Total", typeof(int?)),
            ("Average", typeof(decimal?)),
            ("Present", typeof(long))
        );
        Assert.HasCount(40, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            var first = System.Math.Max(0, index - 2);
            var total = Enumerable.Range(first, index - first + 1).Sum();
            var count = Enumerable.Range(0, 40).LongCount(value => value % 3 == index % 3);
            Assert.AreEqual(index, rows[index].GetValue<int>(0));
            Assert.AreEqual(total, rows[index].GetValue<int?>(1));
            Assert.AreEqual(19.5m, rows[index].GetValue<decimal?>(2));
            Assert.AreEqual(count, rows[index].GetValue<long>(3));
        }
    }
}
