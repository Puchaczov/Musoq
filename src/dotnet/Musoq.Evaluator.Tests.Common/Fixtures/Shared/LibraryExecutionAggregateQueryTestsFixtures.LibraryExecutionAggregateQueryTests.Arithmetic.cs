using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionAggregateQueryTestsFixtures
{

    internal static void AssertReferenceGroup(Tables.Row row, IGrouping<int, GroupedNumericValue> expected)
    {
        var values = expected.Where(static item => item.Value.HasValue).Select(static item => item.Value!.Value).ToArray();
        var distinct = values.Distinct().ToArray();
        Assert.AreEqual(expected.Key, row.GetValue<int>(0));
        Assert.AreEqual(values.Sum(), row.GetValue<int?>(1));
        Assert.AreEqual(values.Select(static value => (decimal)value).Average(), row.GetValue<decimal?>(2));
        Assert.AreEqual(values.Min(), row.GetValue<int?>(3));
        Assert.AreEqual(values.Max(), row.GetValue<int?>(4));
        Assert.AreEqual((long)values.Length, row.GetValue<long>(5));
        Assert.AreEqual((long)distinct.Length, row.GetValue<long>(6));
        Assert.AreEqual(distinct.Select(static value => (decimal)value).Average(), row.GetValue<decimal?>(7));
    }

    public sealed record GroupedNumericValue(int Key, int? Value);

}
