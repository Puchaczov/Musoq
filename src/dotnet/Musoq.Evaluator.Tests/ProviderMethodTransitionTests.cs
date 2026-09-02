using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ProviderMethodTransitionTests : BasicEntityTestBase
{

    [TestMethod]
    public void NullableOuterJoinSide_ShouldRetainProviderMethodBinding()
    {
        const string query = @"
select a.Id, b.GetCountry()
from #A.entities() a
left outer join #B.entities() b on a.Id = b.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }],
            ["#B"] = [new BasicEntity("Poland", "Warsaw") { Id = 2 }]
        };

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("b.GetCountry()", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, null],
            [2, "Poland"]);
    }

    [TestMethod]
    public void WithOrdinality_ShouldRetainProviderMethodBindingOnAppliedRows()
    {
        const string query = @"
select child.Name(), child.Ordinal
from #A.entities() a
cross apply a.Children child with ordinality
order by child.Ordinal";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Id = 1 }]
        };

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("child.Name()", typeof(string)),
            ("child.Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["child1", 0],
            ["child2", 1]);
    }

}
