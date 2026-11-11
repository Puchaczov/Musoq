using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionAggregateQueryTestsFixtures
{
    internal static void AssertSeededGroups(Tables.Table result)
    {
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Partition", typeof(string)),
            ("Total", typeof(decimal)),
            ("Positive", typeof(long))
        );
        Assert.HasCount(2, rows);
        Assert.AreEqual("A", rows[0].GetValue<string>(0));
        Assert.AreEqual(104m, rows[0].GetValue<decimal>(1));
        Assert.AreEqual(2L, rows[0].GetValue<long>(2));
        Assert.AreEqual("B", rows[1].GetValue<string>(0));
        Assert.AreEqual(102m, rows[1].GetValue<decimal>(1));
        Assert.AreEqual(1L, rows[1].GetValue<long>(2));
    }

    public sealed record NumericValue<T>(T Value, T? NullableValue)
        where T : struct;

    public sealed class PositiveSumLibrary : Musoq.Plugins.LibraryBase
    {
        [Musoq.Plugins.Attributes.AggregateFunction(typeof(LibraryExecutionLibrary.PositiveTotalKernel))]
        public new decimal? Sum(decimal? value, [Musoq.Plugins.Attributes.AggregateParent] int parent = 0) =>
            Musoq.Plugins.AggregateFunction.NotInvoked<decimal?>();
    }
}
