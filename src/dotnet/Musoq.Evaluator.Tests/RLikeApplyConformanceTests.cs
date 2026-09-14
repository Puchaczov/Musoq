using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using ApplyChild = Musoq.Evaluator.Tests.LikeApplyConformanceTests.ApplyChild;
using ApplyGrandchild = Musoq.Evaluator.Tests.LikeApplyConformanceTests.ApplyGrandchild;
using ApplyParent = Musoq.Evaluator.Tests.LikeApplyConformanceTests.ApplyParent;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeApplyConformanceTests : GenericEntityTestBase
{
    [TestMethod]
    public void CorrelatedCrossApply_OuterValueRLikeInnerPattern_ShouldReturnExactMatchesAndCalls()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m1.Column rlike m2.Column order by m1.Key, m2.Id";
        var observedKeys = new List<object?>();

        var table = CreateAndRunVirtualMachine(
            query,
            Parents(),
            Children(),
            filterSecondRowsSource: CorrelateChildren(observedKeys)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("m1.Key", typeof(string)), ("m2.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 10], ["one", 11], ["two", 21]);
        CollectionAssert.AreEqual(new object?[] { "one", "two", "empty" }, observedKeys.ToArray());
    }

    [TestMethod]
    public void CorrelatedCrossApply_InnerValueRLikeOuterPattern_ShouldReadStablePatternOncePerOuterRow()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m2.Value rlike m1.Pattern order by m1.Key, m2.Id";
        var parents = Parents();

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            Children(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 10], ["one", 11], ["two", 20], ["two", 21]);
        CollectionAssert.AreEqual(new[] { 1, 1, 1 }, parents.Select(static parent => parent.PatternReads).ToArray());
    }

    [TestMethod]
    public void ChainedCrossApply_ShouldBindEveryRegexAfterItsSource()
    {
        const string query =
            "select m1.Key, m2.Id, m3.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "cross apply #schema.third(m2.Id) m3 " +
            "where m1.Column rlike m2.Column and m2.Value rlike m3.Pattern " +
            "order by m1.Key, m2.Id, m3.Id";
        var grandchildren = new[]
        {
            new ApplyGrandchild { ParentId = 10, Id = 100, Pattern = @"\AAlpha\z" },
            new ApplyGrandchild { ParentId = 11, Id = 110, Pattern = "pha" },
            new ApplyGrandchild { ParentId = 21, Id = 210, Pattern = @"\ABeta\z" }
        };
        var observedChildIds = new List<object?>();

        var table = CreateAndRunVirtualMachine(
            query,
            Parents(),
            Children(),
            grandchildren,
            filterSecondRowsSource: CorrelateChildren(),
            filterThirdRowsSource: (parameters, source) =>
            {
                observedChildIds.Add(parameters[0]);
                return source.Filter(row => row.ParentId == (int)parameters[0]!).ToArray();
            }).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["one", 10, 100],
            ["one", 11, 110],
            ["two", 21, 210]);
        CollectionAssert.AreEqual(new object?[] { 10, 11, 21 }, observedChildIds.ToArray());
    }

    [TestMethod]
    public void OuterApply_EmptyChildren_ShouldPreserveNullExtensionWithoutCompilingInvalidPattern()
    {
        const string query =
            "select m1.Key, m2.Id, m2.Value rlike m1.Pattern as Matched from #schema.first() m1 " +
            "outer apply #schema.second(m1.Key) m2 order by m1.Key, m2.Id";
        var parents = new[] { new ApplyParent { Key = "empty", Column = "Nothing", Pattern = "[" } };

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            Array.Empty<ApplyChild>(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m1.Key", typeof(string)),
            ("m2.Id", typeof(int?)),
            ("Matched", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["empty", null, false]);
        Assert.AreEqual(1, parents[0].PatternReads);
    }

    [TestMethod]
    public void DynamicInvalidPattern_WhenInputIsNull_ShouldReturnFalseWithoutConstruction()
    {
        const string query =
            "select m1.Key, m2.Id, m1.Column rlike m2.Column as Matched from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 order by m1.Key";
        var parents = new[] { new ApplyParent { Key = "null", Column = null, Pattern = "unused" } };
        var children = new[] { new ApplyChild { ParentKey = "null", Id = 1, Column = "[", Value = "unused" } };

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["null", 1, false]);
    }

    [TestMethod]
    public void DynamicInvalidPattern_WhenInputIsNonNull_ShouldRetainRuntimeContext()
    {
        const string query =
            "select m1.Key from #schema.first() m1 cross apply #schema.second(m1.Key) m2 " +
            "where m1.Column rlike m2.Column";
        var parents = new[] { new ApplyParent { Key = "invalid", Column = "Alpha", Pattern = "unused" } };
        var children = new[] { new ApplyChild { ParentKey = "invalid", Id = 1, Column = "[", Value = "unused" } };

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var table = CreateAndRunVirtualMachine(
                query,
                parents,
                children,
                filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);
            _ = table.Count;
        });

        StringAssert.Contains(exception.Message, "MQ9002");
        Assert.IsInstanceOfType<ArgumentException>(exception.InnerException);
    }

    [TestMethod]
    public void CorrelatedCrossApply_NegatedUnicodePattern_ShouldPreserveResults()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m1.Column not rlike m2.Column order by m1.Key, m2.Id";
        var parents = new[] { new ApplyParent { Key = "unicode", Column = "Łódź", Pattern = @"\AŁ" } };
        var children = new[]
        {
            new ApplyChild { ParentKey = "unicode", Id = 1, Column = @"\AŁ", Value = "Łódź" },
            new ApplyChild { ParentKey = "unicode", Id = 2, Column = @"\AW", Value = "Warsaw" }
        };

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["unicode", 2]);
    }

    private static ApplyParent[] Parents() =>
    [
        new ApplyParent { Key = "one", Column = "Alpha", Pattern = @"\AAl" },
        new ApplyParent { Key = "two", Column = "Beta", Pattern = @"\AB" },
        new ApplyParent { Key = "empty", Column = "Nothing", Pattern = @"\AN" }
    ];

    private static ApplyChild[] Children() =>
    [
        new ApplyChild { ParentKey = "one", Id = 10, Column = @"\AAlpha\z", Value = "Alpha" },
        new ApplyChild { ParentKey = "one", Id = 11, Column = "pha", Value = "Alphabet" },
        new ApplyChild { ParentKey = "two", Id = 20, Column = @"\AZ", Value = "Beta" },
        new ApplyChild { ParentKey = "two", Id = 21, Column = @"\ABeta\z", Value = "Beta" }
    ];

    private static Func<object?[], RowSourceFilterInput, object?> CorrelateChildren(
        ICollection<object?>? observedKeys = null) => (parameters, source) =>
    {
        var key = parameters[0];
        observedKeys?.Add(key);
        return source.Filter(row => Equals(row.ParentKey, key)).ToArray();
    };
}
