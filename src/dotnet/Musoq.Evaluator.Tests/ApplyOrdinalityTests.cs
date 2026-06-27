using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ApplyOrdinalityTests : GenericEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void CrossApplyWithOrdinality_PrimitiveArray_ShouldExposeZeroBasedOrdinal()
    {
        const string query = @"
select a.City, b.Value, b.Ordinal
from #schema.first() a
cross apply a.Values b with ordinality
order by a.City, b.Ordinal";
        var source = new[]
        {
            new PrimitiveArrayRow { City = "Alpha", Values = [10d, 20d] },
            new PrimitiveArrayRow { City = "Beta", Values = [30d] }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("b.Ordinal", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        AssertRow(table, 0, "Alpha", 10d, 0);
        AssertRow(table, 1, "Alpha", 20d, 1);
        AssertRow(table, 2, "Beta", 30d, 0);
    }

    [TestMethod]
    public void CrossApplyWithOrdinality_ObjectArray_ShouldExposeOrdinalBesideObjectColumns()
    {
        const string query = @"
select child.Name, child.Score, child.Ordinal
from #schema.first() parent
cross apply parent.Children child with ordinality
order by child.Ordinal";
        var source = new[]
        {
            new ObjectArrayRow
            {
                Children =
                [
                    new ChildRow { Name = "first", Score = 7 },
                    new ChildRow { Name = "second", Score = 11 }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("child.Ordinal", table.Columns.ElementAt(2).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(2).ColumnType);
        Assert.AreEqual("first", table[0][0]);
        Assert.AreEqual(7, table[0][1]);
        Assert.AreEqual(0, table[0][2]);
        Assert.AreEqual("second", table[1][0]);
        Assert.AreEqual(11, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
    }

    [TestMethod]
    public void OuterApplyWithOrdinality_EmptyRightSide_ShouldExposeNullOrdinal()
    {
        const string query = @"
select a.City, b.Ordinal
from #schema.first() a
outer apply a.Values b with ordinality
order by a.City, b.Ordinal";
        var source = new[]
        {
            new PrimitiveArrayRow { City = "Empty", Values = [] },
            new PrimitiveArrayRow { City = "Full", Values = [40d, 50d] }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("b.Ordinal", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(int?), table.Columns.ElementAt(1).ColumnType);
        Assert.AreEqual("Empty", table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.AreEqual("Full", table[1][0]);
        Assert.AreEqual(0, table[1][1]);
        Assert.AreEqual("Full", table[2][0]);
        Assert.AreEqual(1, table[2][1]);
    }

    [TestMethod]
    public void ChainedCrossApplyWithOrdinality_ShouldResetOrdinalForEachApplySource()
    {
        const string query = @"
select a.City, leftValue.Value, leftValue.Ordinal, rightValue.Value, rightValue.Ordinal
from #schema.first() a
cross apply a.LeftValues leftValue with ordinality
cross apply a.RightValues rightValue with ordinality
order by leftValue.Ordinal, rightValue.Ordinal";
        var source = new[]
        {
            new ChainedArrayRow
            {
                City = "Alpha",
                LeftValues = [1d, 2d],
                RightValues = [10d, 20d]
            }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        Assert.AreEqual(4, table.Count);
        AssertChainedRow(table, 0, 1d, 0, 10d, 0);
        AssertChainedRow(table, 1, 1d, 0, 20d, 1);
        AssertChainedRow(table, 2, 2d, 1, 10d, 0);
        AssertChainedRow(table, 3, 2d, 1, 20d, 1);
    }

    [TestMethod]
    public void CrossApplyWithOrdinality_EnumerableRightSide_ShouldCountOrdinals()
    {
        const string query = @"
select a.City, b.Value, b.Ordinal
from #schema.first() a
cross apply a.Values b with ordinality
order by b.Ordinal";
        var source = new[]
        {
            new EnumerableRow { City = "Alpha", Values = Enumerate(3d, 5d, 8d) }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        AssertRow(table, 0, "Alpha", 3d, 0);
        AssertRow(table, 1, "Alpha", 5d, 1);
        AssertRow(table, 2, "Alpha", 8d, 2);
    }

    [TestMethod]
    public void CrossApplyWithOrdinality_WhenRightSourceAlreadyHasOrdinalColumn_ShouldThrow()
    {
        const string query = @"
select item.Name
from #schema.first() source
cross apply source.Items item with ordinality";
        var source = new[]
        {
            new OrdinalCollisionSource
            {
                Items =
                [
                    new OrdinalCollisionItem { Name = "existing", Ordinal = 7 }
                ]
            }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, source));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticPhase.Parse,
            "already exposes an Ordinal column");
    }

    private static void AssertRow(
        Musoq.Evaluator.Tables.Table table,
        int index,
        string city,
        double value,
        int ordinal)
    {
        Assert.AreEqual(city, table[index][0]);
        Assert.AreEqual(value, table[index][1]);
        Assert.AreEqual(ordinal, table[index][2]);
    }

    private static void AssertChainedRow(
        Musoq.Evaluator.Tables.Table table,
        int index,
        double leftValue,
        int leftOrdinal,
        double rightValue,
        int rightOrdinal)
    {
        Assert.AreEqual("Alpha", table[index][0]);
        Assert.AreEqual(leftValue, table[index][1]);
        Assert.AreEqual(leftOrdinal, table[index][2]);
        Assert.AreEqual(rightValue, table[index][3]);
        Assert.AreEqual(rightOrdinal, table[index][4]);
    }

    private static IEnumerable<double> Enumerate(params double[] values)
    {
        foreach (var value in values)
            yield return value;
    }

    private sealed class PrimitiveArrayRow
    {
        public string City { get; init; } = string.Empty;

        public double[] Values { get; init; } = [];
    }

    private sealed class ObjectArrayRow
    {
        [BindablePropertyAsTable]
        public ChildRow[] Children { get; init; } = [];
    }

    public sealed class ChildRow
    {
        public string Name { get; init; } = string.Empty;

        public int Score { get; init; }
    }

    private sealed class ChainedArrayRow
    {
        public string City { get; init; } = string.Empty;

        public double[] LeftValues { get; init; } = [];

        public double[] RightValues { get; init; } = [];
    }

    private sealed class EnumerableRow
    {
        public string City { get; init; } = string.Empty;

        public IEnumerable<double> Values { get; init; } = [];
    }

    private sealed class OrdinalCollisionSource
    {
        [BindablePropertyAsTable]
        public OrdinalCollisionItem[] Items { get; init; } = [];
    }

    public sealed class OrdinalCollisionItem
    {
        public string Name { get; init; } = string.Empty;

        public int Ordinal { get; init; }
    }
}
