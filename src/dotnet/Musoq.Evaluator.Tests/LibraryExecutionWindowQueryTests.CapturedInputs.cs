using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BoundedWindow_WhenArgumentsAreVolatile_ShouldCaptureEachEligibleRowOnce(bool filtered)
    {
        var filter = filtered ? "filter (where AcceptRow())" : string.Empty;
        var query = $"select Position, Sum(NextValue()) {filter} over (order by Position rows between 2 preceding and current row) as Total from #schema.first() order by Position";
        VolatileWindowRow[] input = [new(1), new(2), new(3), new(4)];
        using var compiled = CreateAndRunVirtualMachine<VolatileWindowRow, VolatileWindowLibrary>(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(double?)));
        Assert.HasCount(4, rows);
        double[] expected = filtered ? [11d, 11d, 24d, 13d] : [11d, 23d, 36d, 39d];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(0));
            Assert.AreEqual(expected[index], rows[index].GetValue<double?>(1));
            Assert.AreEqual(filtered && input[index].Position % 2 == 0 ? 0 : 1, input[index].ValueCalls);
            Assert.AreEqual(filtered ? 1 : 0, input[index].FilterCalls);
        }
    }

    public sealed class VolatileWindowRow(int position)
    {
        public int Position { get; } = position;
        public int ValueCalls { get; set; }
        public int FilterCalls { get; set; }
    }

    public sealed class VolatileWindowLibrary : LibraryBase
    {
        [BindableMethod, NonDeterministic]
        public static double? NextValue([InjectSpecificSource(typeof(VolatileWindowRow))] VolatileWindowRow row)
            => row.Position + 10d * ++row.ValueCalls;

        [BindableMethod, NonDeterministic]
        public static bool AcceptRow([InjectSpecificSource(typeof(VolatileWindowRow))] VolatileWindowRow row)
        {
            row.FilterCalls++;
            return row.Position % 2 != 0;
        }
    }
}
