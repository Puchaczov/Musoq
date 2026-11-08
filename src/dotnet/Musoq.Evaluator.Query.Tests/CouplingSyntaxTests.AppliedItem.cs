using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P3-014")]
    public void AppliedCollectionItem_ShouldAllowItemOutputAlias()
    {
        const string query = "select item as Item from #test.Entities() p cross apply p.Array item";
        var entities = new[] { new BasicEntity { Id = 1 } };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        CollectionAssert.AreEqual(new[] { "Item" }, result.Columns.Select(column => column.ColumnName).ToArray());
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 0 }, new object?[] { 1 }, new object?[] { 2 });
    }

    [TestMethod]
    [DataRow("select v.N as Item, v.N + 1 as Item_ from values { (N: 7) } v")]
    [DataRow("with Data as (select v.N as Item, v.N + 1 as Item_ from values { (N: 7) } v) select d.Item as Item, d.Item_ as Item_ from Data d")]
    public void ItemAliasWithSuffixCollision_ShouldPreserveBothOutputColumns(string query)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>();
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, ("Item", typeof(int)), ("Item_", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 7, 8 });
    }
}
