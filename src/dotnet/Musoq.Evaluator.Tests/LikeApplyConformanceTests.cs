using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LikeApplyConformanceTests : GenericEntityTestBase
{
    [TestMethod]
    public void CorrelatedCrossApply_OuterValueLikeInnerPattern_ShouldReturnExactMatchesAndCalls()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m1.Column like m2.Column order by m1.Key, m2.Id";
        var parents = Parents();
        var children = Children();
        var observedKeys = new List<object?>();

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: CorrelateChildren(observedKeys)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("m1.Key", typeof(string)), ("m2.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 10], ["one", 11], ["two", 21]);
        CollectionAssert.AreEqual(new object?[] { "one", "two", "empty" }, observedKeys.ToArray());
    }

    [TestMethod]
    public void CorrelatedCrossApply_InnerValueLikeOuterPattern_ShouldReturnExactMatches()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m2.Value like m1.Pattern order by m1.Key, m2.Id";

        var table = CreateAndRunVirtualMachine(
            query,
            Parents(),
            Children(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("m1.Key", typeof(string)), ("m2.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 10], ["one", 11], ["two", 20], ["two", 21]);
    }

    [TestMethod]
    public void ChainedCrossApply_ShouldBindPatternAfterEachSource()
    {
        const string query =
            "select m1.Key, m2.Id, m3.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "cross apply #schema.third(m2.Id) m3 " +
            "where m1.Column like m2.Column and m2.Value like m3.Pattern order by m1.Key, m2.Id, m3.Id";
        var grandchildren = new[]
        {
            new ApplyGrandchild { ParentId = 10, Id = 100, Pattern = "Al%" },
            new ApplyGrandchild { ParentId = 11, Id = 110, Pattern = "%pha%" },
            new ApplyGrandchild { ParentId = 21, Id = 210, Pattern = "Beta" }
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

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m1.Key", typeof(string)),
            ("m2.Id", typeof(int)),
            ("m3.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one", 10, 100], ["one", 11, 110], ["two", 21, 210]);
        CollectionAssert.AreEqual(new object?[] { 10, 11, 21 }, observedChildIds.ToArray());
    }

    [TestMethod]
    public void OuterApply_EmptyChildren_ShouldPreserveNullExtensionAndFalseLike()
    {
        const string query =
            "select m1.Key, m2.Id, m1.Column like m2.Column as Matched from #schema.first() m1 " +
            "outer apply #schema.second(m1.Key) m2 order by m1.Key, m2.Id";

        var table = CreateAndRunVirtualMachine(
            query,
            Parents(),
            Children(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m1.Key", typeof(string)),
            ("m2.Id", typeof(int?)),
            ("Matched", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["empty", null, false],
            ["one", 10, true],
            ["one", 11, true],
            ["two", 20, false],
            ["two", 21, true]);
    }

    [TestMethod]
    public void OuterApply_OuterPattern_ShouldPreserveNullExtensionAndPrepareOncePerOuterRow()
    {
        const string query =
            "select m1.Key, m2.Id, m2.Value like m1.Pattern as Matched from #schema.first() m1 " +
            "outer apply #schema.second(m1.Key) m2 order by m1.Key, m2.Id";
        var parents = Parents();

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            Children(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("m1.Key", typeof(string)),
            ("m2.Id", typeof(int?)),
            ("Matched", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["empty", null, false],
            ["one", 10, true],
            ["one", 11, true],
            ["two", 20, true],
            ["two", 21, true]);
        CollectionAssert.AreEqual(new[] { 1, 1, 1 }, parents.Select(static parent => parent.PatternReads).ToArray());
    }

    [TestMethod]
    public void CorrelatedCrossApply_OuterPattern_WhenCancelled_ShouldNotReadPattern()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 where m2.Value like m1.Pattern";
        var parents = Parents();
        using var cancellation = new System.Threading.CancellationTokenSource();
        cancellation.Cancel();
        var runnable = CreateAndRunVirtualMachine(
            query,
            parents,
            Children(),
            filterSecondRowsSource: CorrelateChildren());

        Assert.Throws<OperationCanceledException>(() => runnable.Run(cancellation.Token));
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, parents.Select(static parent => parent.PatternReads).ToArray());
    }

    [TestMethod]
    public void CorrelatedCrossApply_OuterPattern_WhenPatternReadFails_ShouldPreserveContextAndCause()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 where m2.Value like m1.Pattern";
        var parents = new[]
        {
            new ApplyParent
            {
                Key = "one",
                Column = "Alpha",
                Pattern = "Al%",
                PatternReadException = new InvalidOperationException("pattern read failed")
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var table = CreateAndRunVirtualMachine(
                query,
                parents,
                Children(),
                filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);
            _ = table.Count;
        });

        StringAssert.Contains(exception.Message, "MQ9002");
        Assert.IsNotNull(exception.InnerException);
        Assert.AreEqual("pattern read failed", exception.InnerException.Message);
        Assert.AreEqual(1, parents[0].PatternReads);
    }

    [TestMethod]
    public void DynamicLikeAfterStoredApplyMaterialization_ShouldKeepCacheInQueryScope()
    {
        const string query =
            "select m1.Key from #schema.first() m1 " +
            "outer apply #schema.second(m1.Key) m2 " +
            "where m1.Column like m2.Column order by m1.Key";

        var table = CreateAndRunVirtualMachine(
            query,
            Parents(),
            Children(),
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("m1.Key", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["one"], ["one"], ["two"]);
    }

    [TestMethod]
    public void CorrelatedCrossApply_NegatedUnicodePattern_ShouldPreserveLegacyResults()
    {
        const string query =
            "select m1.Key, m2.Id from #schema.first() m1 " +
            "cross apply #schema.second(m1.Key) m2 " +
            "where m1.Column not like m2.Column order by m1.Key, m2.Id";
        var parents = new[] { new ApplyParent { Key = "unicode", Column = "Łódź", Pattern = "Ł%" } };
        var children = new[]
        {
            new ApplyChild { ParentKey = "unicode", Id = 1, Column = "Ł%", Value = "Łódź" },
            new ApplyChild { ParentKey = "unicode", Id = 2, Column = "W%", Value = "Warsaw" }
        };

        var table = CreateAndRunVirtualMachine(
            query,
            parents,
            children,
            filterSecondRowsSource: CorrelateChildren()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("m1.Key", typeof(string)), ("m2.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["unicode", 2]);
    }

    private static ApplyParent[] Parents() =>
    [
        new ApplyParent { Key = "one", Column = "Alpha", Pattern = "Al%" },
        new ApplyParent { Key = "two", Column = "Beta", Pattern = "B%" },
        new ApplyParent { Key = "empty", Column = "Nothing", Pattern = "N%" }
    ];

    private static ApplyChild[] Children() =>
    [
        new ApplyChild { ParentKey = "one", Id = 10, Column = "Al%", Value = "Alpha" },
        new ApplyChild { ParentKey = "one", Id = 11, Column = "%pha", Value = "Alphabet" },
        new ApplyChild { ParentKey = "two", Id = 20, Column = "Z%", Value = "Beta" },
        new ApplyChild { ParentKey = "two", Id = 21, Column = "Beta", Value = "Beta" }
    ];

    private static System.Func<object?[], RowSourceFilterInput, object?> CorrelateChildren(
        ICollection<object?>? observedKeys = null)
    {
        return (parameters, source) =>
        {
            var key = parameters[0];
            observedKeys?.Add(key);
            return source.Filter(row => Equals(row.ParentKey, key)).ToArray();
        };
    }

    public sealed class ApplyParent
    {
        private string? _pattern;

        public string? Key { get; set; }

        public string? Column { get; set; }

        public string? Pattern
        {
            get
            {
                PatternReads++;
                if (PatternReadException != null)
                    throw PatternReadException;
                return _pattern;
            }
            set => _pattern = value;
        }

        public int PatternReads { get; private set; }

        public Exception? PatternReadException { get; init; }
    }

    public sealed class ApplyChild
    {
        public string? ParentKey { get; set; }

        public int Id { get; set; }

        public string? Column { get; set; }

        public string? Value { get; set; }
    }

    public sealed class ApplyGrandchild
    {
        public int ParentId { get; set; }

        public int Id { get; set; }

        public string? Pattern { get; set; }
    }
}
