using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class ApplyOrdinalityTests
{
    [TestMethod]
    public void CrossApply_CustomReadOnlyList_UsesItsEnumeratorInsteadOfIndexer()
    {
        var input = new[] { new ReadOnlyListRow() };
        using var query = CreateAndRunVirtualMachine("""
            select b.Value, b.Ordinal from #schema.first() a
            cross apply a.Values b with ordinality order by b.Ordinal
            """, input);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [4, 0], [8, 1]);
    }

    [TestMethod]
    public void CrossApply_StructEnumerable_PreservesTypedTraversalWithOrdinality()
    {
        var input = new[] { new StructEnumerableRow { Values = [3, 5] } };
        using var query = CreateAndRunVirtualMachine("""
            select b.Value, b.Ordinal from #schema.first() a
            cross apply a.Values b with ordinality order by b.Ordinal
            """, input);
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, 0], [5, 1]);
        var compute = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).ToArray();
        var generatedType = compute.First(static method => method.DeclaringType?.Name == "CompiledQuery").DeclaringType!;
        var stages = generatedType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name.StartsWith("BuildCte", StringComparison.Ordinal));
        Assert.IsFalse(compute.Concat(stages).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    public void OuterApply_NullableScalarList_PreservesNullElementsDuplicatesAndPresence()
    {
        const string query = """
            select a.City, b.Value, b.Ordinal, b is present as Present
            from #schema.first() a
            outer apply a.Values b with ordinality
            order by a.City, b.Ordinal
            """;
        var source = new[]
        {
            new NullableListRow { City = "Values", Values = [null, 0, 2, 2] },
            new NullableListRow { City = "Empty", Values = [] },
            new NullableListRow { City = "Null", Values = null }
        };
        using var queryInstance = CreateAndRunVirtualMachine(query, source);

        var first = queryInstance.Run(TestContext.CancellationToken);
        Assert.AreEqual(6, first.Count);
        var second = queryInstance.Run(TestContext.CancellationToken);
        Assert.AreEqual(6, second.Count);

        TableMaterializationTestHelper.AssertRowsInOrder(first,
            ["Empty", null, null, false], ["Null", null, null, false],
            ["Values", null, 0, true], ["Values", 0, 1, true],
            ["Values", 2, 2, true], ["Values", 2, 3, true]);
        TableMaterializationTestHelper.AssertRowsInOrder(second,
            ["Empty", null, null, false], ["Null", null, null, false],
            ["Values", null, 0, true], ["Values", 0, 1, true],
            ["Values", 2, 2, true], ["Values", 2, 3, true]);
    }

    [TestMethod]
    public void CrossApply_LazyScalars_EnumeratesOnceAndDisposesEnumeration()
    {
        var row = new TrackedEnumerableRow();
        using var query = CreateAndRunVirtualMachine("""
            select b.Value, b.Ordinal from #schema.first() a
            cross apply a.Values b with ordinality order by b.Ordinal
            """, new[] { row });
        row.Reads = 0;
        row.Disposals = 0;
        row.Enumerations = 0;

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, 0], [3, 1], [7, 2]);
        Assert.AreEqual(1, row.Enumerations);
        Assert.AreEqual(1, row.Reads);
        Assert.AreEqual(1, row.Disposals);
    }

    [TestMethod]
    public void CrossApply_LazyScalars_CancellationDisposesActiveEnumerator()
    {
        using var cancellation = new CancellationTokenSource();
        var row = new TrackedEnumerableRow { Cancellation = cancellation };
        using var query = CreateAndRunVirtualMachine("""
            select b.Value from #schema.first() a cross apply a.Values b with ordinality
            """, new[] { row });

        row.Reads = 0;
        row.Disposals = 0;
        Assert.Throws<OperationCanceledException>(() => _ = query.Run(cancellation.Token).Count);
        Assert.AreEqual(1, row.Disposals);
    }

    public sealed class NullableListRow
    {
        public string City { get; init; } = string.Empty;
        public List<int?>? Values { get; init; }
    }

    public sealed class StructEnumerableRow
    {
        public ImmutableArray<int> Values { get; init; }
    }

    public sealed class ReadOnlyListRow
    {
        public IReadOnlyList<int> Values { get; } = new EnumerableOnlyList();
    }

    private sealed class EnumerableOnlyList : IReadOnlyList<int>
    {
        public int Count => throw new InvalidOperationException("Count is not part of the traversal contract");
        public int this[int index] => throw new InvalidOperationException("Indexer is not part of the traversal contract");
        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)new[] { 4, 8 }).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class TrackedEnumerableRow
    {
        public int Reads;
        public int Disposals;
        public int Enumerations;
        public CancellationTokenSource? Cancellation;

        public IEnumerable<int> Values
        {
            get
            {
                Reads++;
                return EnumerateValues();
            }
        }

        private IEnumerable<int> EnumerateValues()
        {
            Enumerations++;
            try
            {
                yield return 3;
                Cancellation?.Cancel();
                yield return 3;
                yield return 7;
            }
            finally
            {
                Disposals++;
            }
        }
    }
}
