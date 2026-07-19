using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_Join_SemiJoin()
    {
        var table = RunDirectJoinSpecQuery("select a.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id");
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table,
            new object?[] { "A1" },
            new object?[] { "A3" });
    }

    [TestMethod]
    public void Spec_Join_AntiJoin()
    {
        var table = RunDirectJoinSpecQuery("select a.Name from #A.Entities() a anti join #B.Entities() b on a.Id = b.Id");
        TableMaterializationTestHelper.AssertColumns(table, ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { "A2" });
    }

    private Tables.Table RunDirectJoinSpecQuery(string query)
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("A1") { Id = 1 },
                    new BasicEntity("A2") { Id = 2 },
                    new BasicEntity("A3") { Id = 3 }
                ]
            },
            {
                "#B", [
                    new BasicEntity("B1") { Id = 1 },
                    new BasicEntity("B1Duplicate") { Id = 1 },
                    new BasicEntity("B3") { Id = 3 }
                ]
            }
        };

        return CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);
    }
}
