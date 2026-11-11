using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ResultAssertionContractTests
{
    [TestMethod]
    [DataRow("missing")]
    [DataRow("duplicate")]
    [DataRow("duplicate-and-missing")]
    [DataRow("negative")]
    [DataRow("outside")]
    public void ReadValues_WhenVisitationIsMalformed_ShouldRejectIt(string mode)
    {
        Assert.ThrowsExactly<AssertFailedException>(() => new VisitationRow(mode).ReadValues());
    }

    [TestMethod]
    [DataRow("missing")]
    [DataRow("duplicate")]
    public void ReadValue_WhenTheSelectedColumnIsNotVisitedExactlyOnce_ShouldRejectIt(string mode)
    {
        Assert.ThrowsExactly<AssertFailedException>(() => new VisitationRow(mode).ReadValue(0));
    }

    [TestMethod]
    public void ReadValues_WhenNullIsExplicitlyVisited_ShouldPreserveIt()
    {
        var row = new VisitationRow("valid");

        CollectionAssert.AreEqual(new object?[] { null }, row.ReadValues());
        Assert.IsNull(row.ReadValue(0));
    }

    [TestMethod]
    public void ReadValues_ShouldPreserveOrdinalsAndTypesRegardlessOfVisitationOrder()
    {
        var row = new OutOfOrderRow();

        CollectionAssert.AreEqual(new object?[] { 42, null, false }, row.ReadValues());
        Assert.AreEqual(42, row.ReadValue(0));
        Assert.IsNull(row.ReadValue(1));
        Assert.AreEqual(false, row.ReadValue(2));
        Assert.HasCount(0, new VisitationRow("empty").ReadValues());
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => new VisitationRow("empty").ReadValue(0));
    }

    [TestMethod]
    public void RowComparisons_WhenDuplicateMultiplicityChanges_ShouldRejectBothModes()
    {
        using var table = new Table("control", [new Column("Value", typeof(int), 0)])
        {
            new ValueRow(1),
            new ValueRow(2),
            new ValueRow(2),
        };

        Assert.ThrowsExactly<AssertFailedException>(() =>
            TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [1], [2])
        );
        Assert.ThrowsExactly<AssertFailedException>(() =>
            TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [1], [2])
        );
    }

    [TestMethod]
    public void RowComparisons_WhenOnlyOrderChanges_ShouldRespectTheSelectedContract()
    {
        using var table = new Table("control", [new Column("Value", typeof(int), 0)])
        {
            new ValueRow(2),
            new ValueRow(1),
        };

        TableMaterializationTestHelper.AssertRowsUnordered(table, [1], [2]);
        Assert.ThrowsExactly<AssertFailedException>(() =>
            TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2])
        );
    }

    private sealed class VisitationRow(string mode) : Row
    {
        public override int Count =>
            mode switch
            {
                "empty" => 0,
                "duplicate-and-missing" => 2,
                _ => 1,
            };

        public override void VisitColumns<TVisitor>(ref TVisitor visitor)
        {
            if (mode is "missing" or "empty")
                return;
            object? value = null;
            visitor.Visit(
                mode switch
                {
                    "negative" => -1,
                    "outside" => 1,
                    _ => 0,
                },
                in value
            );
            if (mode is "duplicate" or "duplicate-and-missing")
                visitor.Visit(0, in value);
        }
    }

    private sealed class ValueRow(int value) : Row
    {
        public override int Count => 1;

        public override void VisitColumns<TVisitor>(ref TVisitor visitor) => visitor.Visit(0, in value);
    }

    private sealed class OutOfOrderRow : Row
    {
        public override int Count => 3;

        public override void VisitColumns<TVisitor>(ref TVisitor visitor)
        {
            bool flag = false;
            int number = 42;
            string? text = null;
            visitor.Visit(2, in flag);
            visitor.Visit(0, in number);
            visitor.Visit(1, in text);
        }
    }
}
