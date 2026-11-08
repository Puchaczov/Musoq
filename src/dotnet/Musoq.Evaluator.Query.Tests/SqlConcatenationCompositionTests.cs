using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SqlConcatenationCompositionTests : Schema.Basic.BasicEntityTestBase
{
    [TestMethod]
    [DataRow("select Max(e.Name) || Max(e.Name) as A from #A.Entities() e", "xx")]
    [DataRow("select e.Name || '!' as A from #A.Entities() e group by e.Name || '!'", "x!")]
    [DataRow("with q as (select e.Name as N from #A.Entities() e) select q.N || '!' as A from q", "x!")]
    [DataRow("select Min(e.Name) over (partition by e.Id) || '!' as A from #A.Entities() e", "x!")]
    public void Concatenation_ShouldComposeWithAggregateGroupCteAndWindow(string sql, string expected)
    {
        using var query = CreateAndRunVirtualMachine(sql,
            new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<Schema.Basic.BasicEntity>>
            { ["#A"] = [new Schema.Basic.BasicEntity("x") { Id = 1 }] });
        using var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
    }
}
