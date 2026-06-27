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
    public void WhenAsOfJoinInequalityReferencesOneSide_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= a.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 50m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrCondition_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population or a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMultipleInequalities_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population and a.Money > b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1" }] },
            { "#B", [new BasicEntity { Name = "B1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenAsOfJoinWithReversedOperandOrder_ShouldSwapAndMatch()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof join #B.entities() b on b.Population >= a.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 10 },
                    new BasicEntity { Name = "A2", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 20 },
                    new BasicEntity { Name = "B2", Population = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // b.Population >= a.Population: find smallest right key >= left probe
        // A1 (10) -> B1 (20) — smallest B >= 10
        Assert.AreEqual("A1", rows[0][0]);
        Assert.AreEqual("B1", rows[0][1]);

        // A2 (50) -> B2 (60) — smallest B >= 50
        Assert.AreEqual("A2", rows[1][0]);
        Assert.AreEqual("B2", rows[1][1]);
    }

    [TestMethod]
    public void WhenAsOfJoinWithDuplicateRightKeys_ShouldPickOne()
    {
        var query = @"
select
    a.Name,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 },
                    new BasicEntity { Name = "B2", Population = 90 },
                    new BasicEntity { Name = "B3", Population = 50 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        // A1 (100) should match one of B1/B2 (both 90) — the closest keys
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(90m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithNullInequalityKey_ShouldReturnNulls()
    {
        var query = @"
select
    a.Name,
    b.Name
from #A.entities() a
asof left join #B.entities() b on a.NullableValue >= b.NullableValue";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", NullableValue = null },
                    new BasicEntity { Name = "A2", NullableValue = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", NullableValue = 30 },
                    new BasicEntity { Name = "B2", NullableValue = 60 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        var rows = table.OrderBy(r => (string)r[0]).ToList();

        // A2 (50) >= B1 (30) — match
        var a2Row = rows.First(r => (string)r[0] == "A2");
        Assert.AreEqual("B1", a2Row[1]);

        // A1 (null) — null probe should not match, left join returns nulls
        var a1Row = rows.First(r => (string)r[0] == "A1");
        Assert.IsNull(a1Row[1]);
    }

    [TestMethod]
    public void WhenAsOfJoinChainedWithAsOfJoin_ShouldWorkCorrectly()
    {
        var query = @"
select
    a.Name,
    b.Name,
    c.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population
asof join #C.entities() c on a.Population >= c.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 100 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 90 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "C1", Population = 80 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B1", table[0][1]);
        Assert.AreEqual("C1", table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinGreaterThanWithDuplicateExactKeys_ShouldSkipAllDuplicates()
    {
        var query = @"
select
    a.Name,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population > b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 50 },
                    new BasicEntity { Name = "B4", Population = 50 },
                    new BasicEntity { Name = "B5", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(10m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinLessThanWithDuplicateExactKeys_ShouldSkipAllDuplicates()
    {
        var query = @"
select
    a.Name,
    b.Name,
    b.Population
from #A.entities() a
asof join #B.entities() b on a.Population < b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "A1", Population = 50 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "B1", Population = 10 },
                    new BasicEntity { Name = "B2", Population = 50 },
                    new BasicEntity { Name = "B3", Population = 50 },
                    new BasicEntity { Name = "B4", Population = 90 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual(90m, (decimal)table[0][2]);
    }

    [TestMethod]
    public void WhenAsOfJoinInequalityReferencesOnlyRightSide_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on b.Population >= b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 50m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind, "both sides");
    }

    [TestMethod]
    public void WhenAsOfJoinWithOrNestedInsideAnd_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Name = b.Name and (a.City = b.City or a.Country = b.Country)";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "C1", Country = "US" }] },
            { "#B", [new BasicEntity { Name = "B1", City = "C2", Country = "UK" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMultipleEqualitiesAndNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Name = b.Name and a.City = b.City";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", City = "C1" }] },
            { "#B", [new BasicEntity { Name = "B1", City = "C1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind, "inequality");
    }

    [TestMethod]
    public void WhenAsOfJoinWithMixedInequalityOperators_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof join #B.entities() b on a.Population >= b.Population and a.Money <= b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind, "exactly one");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinInequalityReferencesOneSide_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= a.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 50m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides, DiagnosticPhase.Bind, "both sides");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithOrCondition_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population or a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3038_AsOfJoinOrNotSupported, DiagnosticPhase.Bind, "OR");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithMultipleInequalities_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof left join #B.entities() b on a.Population >= b.Population and a.Money > b.Money";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100, Money = 1m }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 2m }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities, DiagnosticPhase.Bind, "exactly one");
    }

    [TestMethod]
    public void WhenAsOfLeftJoinWithNoInequality_ShouldThrow()
    {
        var query = @"
select a.Name
from #A.entities() a
asof left join #B.entities() b on a.Name = b.Name";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1" }] },
            { "#B", [new BasicEntity { Name = "B1" }] }
        };

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, sources));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3036_AsOfJoinMissingInequality, DiagnosticPhase.Bind, "inequality");
    }
}
