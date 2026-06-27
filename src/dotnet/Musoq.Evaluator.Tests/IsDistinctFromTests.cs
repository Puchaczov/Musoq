using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class IsDistinctFromTests : BasicEntityTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void IsDistinctFrom_WithNullsAndValues_ShouldUseNullSafeTruthTable()
    {
        const string query = @"
from values {
    { Label: 'both-null', LeftValue: null, RightValue: null },
    { Label: 'left-null', LeftValue: null, RightValue: 1 },
    { Label: 'right-null', LeftValue: 1, RightValue: null },
    { Label: 'equal', LeftValue: 1, RightValue: 1 },
    { Label: 'different', LeftValue: 1, RightValue: 2 }
} pairs
where pairs.LeftValue is distinct from pairs.RightValue
select pairs.Label
order by pairs.Label";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("different", table[0][0]);
        Assert.AreEqual("left-null", table[1][0]);
        Assert.AreEqual("right-null", table[2][0]);
    }

    [TestMethod]
    public void IsNotDistinctFrom_WithNullsAndValues_ShouldUseNullSafeTruthTable()
    {
        const string query = @"
from values {
    { Label: 'both-null', LeftValue: null, RightValue: null },
    { Label: 'left-null', LeftValue: null, RightValue: 1 },
    { Label: 'right-null', LeftValue: 1, RightValue: null },
    { Label: 'equal', LeftValue: 1, RightValue: 1 },
    { Label: 'different', LeftValue: 1, RightValue: 2 }
} pairs
where pairs.LeftValue is not distinct from pairs.RightValue
select pairs.Label
order by pairs.Label";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("both-null", table[0][0]);
        Assert.AreEqual("equal", table[1][0]);
    }

    [TestMethod]
    public void IsNotDistinctFrom_WithStrings_ShouldUseExistingTypedEquality()
    {
        const string query = @"
from values {
    { Label: 'alpha', LeftValue: 'Alpha', RightValue: 'Alpha' },
    { Label: 'beta', LeftValue: 'Beta', RightValue: 'Gamma' },
    { Label: 'both-null', LeftValue: null, RightValue: null }
} pairs
where pairs.LeftValue is not distinct from pairs.RightValue
select pairs.Label
order by pairs.Label";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("alpha", table[0][0]);
        Assert.AreEqual("both-null", table[1][0]);
    }

    [TestMethod]
    public void IsDistinctFrom_WithIncompatibleOperands_ShouldUseTypeMismatchDiagnostic()
    {
        const string query = @"
from values {
    { LeftValue: 1, Flag: true }
} pairs
where pairs.LeftValue is distinct from pairs.Flag
select pairs.LeftValue";

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticPhase.Bind,
            "cannot compare");
        MusoqExceptionAssertions.AssertHasGuidance(exception);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources()
    {
        return new();
    }
}
