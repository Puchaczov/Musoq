using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionProviderQueryTests
{
    internal const string IndexedMembersQuery = "select Position, Children[-1].Value as Last, Children[8].Value as Missing, " +
        "ChildrenByName['present'].Value as Present, ChildrenByName['missing'].Value as MissingKey " +
        "from #schema.first() order by Position";

    [TestMethod]
    public void TypedIndexedMembers_WhenElementsAreMissing_ShouldPreserveNativeDefaults()
    {
        const string query = IndexedMembersQuery;
        IndexedParent[] input =
        [
            new(1, [new(3), new(7)], new Dictionary<string, IndexedChild> { ["present"] = new(11) }),
            new(2, null, new Dictionary<string, IndexedChild>())
        ];
        using var compiled = CreateAndRunVirtualMachine<IndexedParent, LibraryBase>(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Last", typeof(int)),
            ("Missing", typeof(int)), ("Present", typeof(int)), ("MissingKey", typeof(int)));
        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows.Select(row => row.GetValue<int>(0)).ToArray());
        CollectionAssert.AreEqual(new[] { 7, 0 }, rows.Select(row => row.GetValue<int>(1)).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 0 }, rows.Select(row => row.GetValue<int>(2)).ToArray());
        CollectionAssert.AreEqual(new[] { 11, 0 }, rows.Select(row => row.GetValue<int>(3)).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 0 }, rows.Select(row => row.GetValue<int>(4)).ToArray());
    }

    public sealed record IndexedChild(int Value);
    public sealed record IndexedParent(int Position, IndexedChild[]? Children,
        IReadOnlyDictionary<string, IndexedChild> ChildrenByName);
}
