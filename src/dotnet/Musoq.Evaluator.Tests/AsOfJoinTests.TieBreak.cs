using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void WhenAsOfJoinTieBreakAscending_ShouldPickLowestTieKey()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
tie break by b.Name asc";

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B-Alpha", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakDescending_ShouldPickHighestTieKey()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
tie break by b.Name desc";

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B-Zulu", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakNullsLast_ShouldUseExplicitNullOrdering()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
tie break by b.NullableValue asc nulls last";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            {
                "#B", [
                    new BasicEntity { Name = "B-Null", Population = 90, NullableValue = null },
                    new BasicEntity { Name = "B-Value", Population = 90, NullableValue = 5 }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B-Value", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinTieBreakDescending_ShouldTieBreakMatchesAndNullExtendMisses()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population
tie break by b.Name desc
order by a.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 },
                    new BasicEntity { Name = "A2", Population = 10 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B-Alpha", Population = 90 },
                    new BasicEntity { Name = "B-Zulu", Population = 90 }
                ]
            }
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);
        var rows = table.ToList();

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B-Zulu", rows[0][1]);
        Assert.AreEqual("A2", rows[1][0]);
        Assert.IsNull(rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithoutTieBreak_ShouldKeepFirstDuplicateRightRow()
    {
        const string query = @"
select a.Name, b.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var table = CreateAndRunVirtualMachine(query, CreateTieBreakSources()).Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("B-Zulu", table[0][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinTieBreakReferencesLeftSide_ShouldThrow()
    {
        const string query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
tie break by a.Name";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, CreateTieBreakSources()));

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
            DiagnosticPhase.Bind,
            "right-side");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateTieBreakSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            {
                "#B", [
                    new BasicEntity { Name = "B-Zulu", Population = 90 },
                    new BasicEntity { Name = "B-Alpha", Population = 90 },
                    new BasicEntity { Name = "B-Older", Population = 50 }
                ]
            }
        };
    }
}
