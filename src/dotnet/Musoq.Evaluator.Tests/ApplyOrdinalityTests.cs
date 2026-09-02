using System.Collections.Generic;
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double)),
            ("b.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", 10d, 0],
            ["Alpha", 20d, 1],
            ["Beta", 30d, 0]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("child.Name", typeof(string)),
            ("child.Score", typeof(int)),
            ("child.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["first", 7, 0],
            ["second", 11, 1]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Ordinal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Empty", null],
            ["Full", 0],
            ["Full", 1]);
    }

    [TestMethod]
    public void OuterApplyWithOrdinality_WindowLagAndQualify_ShouldPreserveEmptyRightSide()
    {
        const string query = @"
select a.City, b.Value, b.Ordinal,
       RowNumber() over (partition by a.City order by b.Ordinal) as rn,
       Lag(b.Value, 1) over (partition by a.City order by b.Ordinal) as PreviousValue
from #schema.first() a
outer apply a.Values b with ordinality
qualify RowNumber() over (partition by a.City order by b.Ordinal) <= 2
order by a.City, b.Ordinal";
        var source = new[]
        {
            new PrimitiveArrayRow { City = "Alpha", Values = [10d, 20d, 30d] },
            new PrimitiveArrayRow { City = "Beta", Values = [] }
        };

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double?)),
            ("b.Ordinal", typeof(int?)),
            ("rn", typeof(long)),
            ("PreviousValue", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", 10d, 0, 1L, null],
            ["Alpha", 20d, 1, 2L, 10d],
            ["Beta", null, null, 1L, null]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("leftValue.Value", typeof(double)),
            ("leftValue.Ordinal", typeof(int)),
            ("rightValue.Value", typeof(double)),
            ("rightValue.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", 1d, 0, 10d, 0],
            ["Alpha", 1d, 0, 20d, 1],
            ["Alpha", 2d, 1, 10d, 0],
            ["Alpha", 2d, 1, 20d, 1]);
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("b.Value", typeof(double)),
            ("b.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alpha", 3d, 0],
            ["Alpha", 5d, 1],
            ["Alpha", 8d, 2]);
    }

    [TestMethod]
    public void CrossApplySchemaMethodWithOrdinality_ShouldRetainGenericInjectedSourceBinding()
    {
        const string noOrdinalityQuery = @"
select b.Value
from #schema.first() a
cross apply a.MethodArrayOfStringsWithoutParameters() b";
        const string query = @"
select b.Value, b.Ordinal
from #schema.first() a
cross apply a.MethodArrayOfStringsWithoutParameters() b with ordinality
order by b.Ordinal";
        var source = new[]
        {
            new ObjectArrayRow
            {
                Children = [new ChildRow { Name = "parent" }]
            }
        };

        var noOrdinalityTable = CreateAndRunVirtualMachine(noOrdinalityQuery, source).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(noOrdinalityTable, ["one"], ["two"]);

        var table = CreateAndRunVirtualMachine(query, source).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 0], ["two", 1]);
    }

    [TestMethod]
    public void OuterApplySchemaMethod_ShouldRetainGenericInjectedSourceWithAndWithoutOrdinality()
    {
        const string noOrdinalityQuery = @"
select b.Value
from #schema.first() a
outer apply a.MethodArrayOfStringsWithoutParameters() b";
        const string ordinalityQuery = @"
select b.Value, b.Ordinal
from #schema.first() a
outer apply a.MethodArrayOfStringsWithoutParameters() b with ordinality
order by b.Ordinal";
        var source = new[]
        {
            new ObjectArrayRow
            {
                Children = [new ChildRow { Name = "parent" }]
            }
        };

        var noOrdinalityTable = CreateAndRunVirtualMachine(noOrdinalityQuery, source).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(noOrdinalityTable, ["one"], ["two"]);

        var ordinalityTable = CreateAndRunVirtualMachine(ordinalityQuery, source).Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(ordinalityTable, ["one", 0], ["two", 1]);
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

    private static IEnumerable<double> Enumerate(params double[] values)
    {
        foreach (var value in values)
            yield return value;
    }

    public sealed class PrimitiveArrayRow
    {
        public string City { get; init; } = string.Empty;

        public double[] Values { get; init; } = [];
    }

    public sealed class ObjectArrayRow
    {
        [BindablePropertyAsTable]
        public ChildRow[] Children { get; init; } = [];
    }

    public sealed class ChildRow
    {
        public string Name { get; init; } = string.Empty;

        public int Score { get; init; }
    }

    public sealed class ChainedArrayRow
    {
        public string City { get; init; } = string.Empty;

        public double[] LeftValues { get; init; } = [];

        public double[] RightValues { get; init; } = [];
    }

    public sealed class EnumerableRow
    {
        public string City { get; init; } = string.Empty;

        public IEnumerable<double> Values { get; init; } = [];
    }

    public sealed class OrdinalCollisionSource
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
