using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

using static Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void CustomFrameKernel_WhenInputAndResultTypesDiffer_ShouldReadTypedFrameStorage()
    {
        const string query = """
            select Position, PositiveAverage(Value) over (order by Position
                rows between 2 preceding and current row) as Average
            from #schema.first() order by Position
            """;
        NumericRow<int>[] input = [new(1, 1), new(2, 2), new(3, -3), new(4, null)];
        using var compiled = CreateAndRunVirtualMachine<NumericRow<int>, FrameLibrary>(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Average", typeof(decimal?)));
        Assert.HasCount(4, rows);
        decimal?[] expected = [1m, 1.5m, 1.5m, 2m];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<decimal?>(1));
        }
    }
}
