using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CteReadOnceFusionDeterminismTests : BasicEntityTestBase
{
    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicProjectionIsReadTwice_ShouldMaterializeProducerValue()
    {
        const string query = @"
            with p as (select NextValue() as r from #A.Entities())
            select p.r, p.r as again from p";

        var table = RunSingleRowQuery(query);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(table[0][0], table[0][1]);
    }

    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicAliasIsComparedWithItself_ShouldMaterializeBeforeFiltering()
    {
        const string query = @"
            with p as (select NextValue() as r from #A.Entities())
            select p.r from p where p.r = p.r";

        var table = RunSingleRowQuery(query);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicFilteredColumnIsPruned_ShouldMaterializeBeforeFiltering()
    {
        const string query = @"
            with p as (select NextValue() as r, Name from #A.Entities())
            select p.Name from p where p.r = p.r";

        var table = RunSingleRowQuery(query);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A", table[0][0]);
    }

    private Table RunSingleRowQuery(string query)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("A")] }
        };

        return CreateAndRunVirtualMachine(query, sources).Run();
    }
}