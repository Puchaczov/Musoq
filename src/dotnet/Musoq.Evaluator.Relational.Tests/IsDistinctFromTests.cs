using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed partial class IsDistinctFromTests : BasicEntityTestBase
{
    [TestMethod]
    public void IsDistinctFrom_WithNullsAndValues_ShouldUseNullSafeTruthTable()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Label: 'both-null', LeftValue: null, RightValue: null ),\n"
            + "    ( Label: 'left-null', LeftValue: null, RightValue: 1 ),\n"
            + "    ( Label: 'right-null', LeftValue: 1, RightValue: null ),\n"
            + "    ( Label: 'equal', LeftValue: 1, RightValue: 1 ),\n"
            + "    ( Label: 'different', LeftValue: 1, RightValue: 2 )\n"
            + "} pairs\n"
            + "where pairs.LeftValue is distinct from pairs.RightValue\n"
            + "select pairs.Label\n"
            + "order by pairs.Label"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["different"], ["left-null"], ["right-null"]);
    }

    [TestMethod]
    public void IsNotDistinctFrom_WithNullsAndValues_ShouldUseNullSafeTruthTable()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Label: 'both-null', LeftValue: null, RightValue: null ),\n"
            + "    ( Label: 'left-null', LeftValue: null, RightValue: 1 ),\n"
            + "    ( Label: 'right-null', LeftValue: 1, RightValue: null ),\n"
            + "    ( Label: 'equal', LeftValue: 1, RightValue: 1 ),\n"
            + "    ( Label: 'different', LeftValue: 1, RightValue: 2 )\n"
            + "} pairs\n"
            + "where pairs.LeftValue is not distinct from pairs.RightValue\n"
            + "select pairs.Label\n"
            + "order by pairs.Label"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["both-null"], ["equal"]);
    }

    [TestMethod]
    public void IsNotDistinctFrom_WithStrings_ShouldUseExistingTypedEquality()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Label: 'alpha', LeftValue: 'Alpha', RightValue: 'Alpha' ),\n"
            + "    ( Label: 'beta', LeftValue: 'Beta', RightValue: 'Gamma' ),\n"
            + "    ( Label: 'both-null', LeftValue: null, RightValue: null )\n"
            + "} pairs\n"
            + "where pairs.LeftValue is not distinct from pairs.RightValue\n"
            + "select pairs.Label\n"
            + "order by pairs.Label"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["alpha"], ["both-null"]);
    }

    [TestMethod]
    public void IsDistinctFrom_WithIncompatibleOperands_ShouldUseTypeMismatchDiagnostic()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( LeftValue: 1, Flag: true )\n"
            + "} pairs\n"
            + "where pairs.LeftValue is distinct from pairs.Flag\n"
            + "select pairs.LeftValue"
        );

        var exception = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertErrorEnvelope(
            exception,
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticPhase.Bind,
            "cannot compare"
        );
        MusoqExceptionAssertions.AssertHasGuidance(exception);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources()
    {
        return new();
    }
}
