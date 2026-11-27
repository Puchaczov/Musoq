using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    [DataRow("NaN", true)]
    [DataRow("1", false)]
    public void RecursiveUnionKey_AdmitsEachNaNWhileDeduplicatingFiniteKeys(string key, bool admitsChild)
    {
        var sql = $$"""
            with recursive r(K,D) as (
                select x.T::double,0 from values { (T:'{{key}}') } x
                union (K) select p.K,p.D+1 from r p where p.D<1
            ) select q.D from r q
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            admitsChild
                ?
                [
                    [0],
                    [1],
                ]
                :
                [
                    [0],
                ]
        );
    }

    [TestMethod]
    public void RecursiveUnionFullRow_PreservesDuplicateNaNAnchors()
    {
        const string sql = """
            with recursive r(K) as (
                select x.T::double from values { (T:'NaN'),(T:'NaN') } x
                union select p.K from r p where 1=0
            ) select Count(*) from r q
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2L]);
    }
}
