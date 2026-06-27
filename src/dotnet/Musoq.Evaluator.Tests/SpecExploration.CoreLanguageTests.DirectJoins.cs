using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_Join_SemiJoin()
    {
        var table = RunDirectJoinSpecQuery("select a.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id");
        var rows = table.Select(row => (string)row[0]).OrderBy(name => name).ToArray();

        CollectionAssert.AreEqual(new[] { "A1", "A3" }, rows);
    }

    [TestMethod]
    public void Spec_Join_AntiJoin()
    {
        var table = RunDirectJoinSpecQuery("select a.Name from #A.Entities() a anti join #B.Entities() b on a.Id = b.Id");
        var rows = table.Select(row => (string)row[0]).ToArray();

        CollectionAssert.AreEqual(new[] { "A2" }, rows);
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