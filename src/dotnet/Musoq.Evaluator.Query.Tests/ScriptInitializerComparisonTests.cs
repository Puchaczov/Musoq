using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class ScriptInitializerComparisonTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("null = 1", false, null)]
    [DataRow("null = null", true, null)]
    [DataRow("5 between null and 10", true, null)]
    [DataRow("5 between 1 and null", false, null)]
    [DataRow("11 not between null and 10", true, true)]
    [DataRow("3 not between 1 and 4", false, false)]
    [DataRow("null is not distinct from null", true, true)]
    public void NullableComparison_ShouldReturnTheEstablishedLetAndProjectionResults(
        string expression,
        bool initialized,
        bool? projected
    )
    {
        var sql =
            $"let n: bool? = {expression}; select $n as Initialized, ({expression}) as Projected from values {{(Seed:1)}} s";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var rows = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(rows, [initialized, projected]);
    }

    [TestMethod]
    [DataRow("false and null", false)]
    [DataRow("true or null", true)]
    [DataRow("not null", null)]
    public void NullableBooleanOperator_ShouldReportTheLetBoundaryAndReturnTheProjectedTruthValue(
        string expression,
        bool? expected
    )
    {
        var schema = new Dictionary<string, IEnumerable<BasicEntity>>();
        var declaration = $"let n: bool? = {expression}; select $n from values {{(Seed:1)}} s";
        var failure = Assert.ThrowsExactly<MusoqQueryException>(() => CreateAndRunVirtualMachine(declaration, schema));
        Assert.AreEqual(DiagnosticCode.MQ3065_InvalidScriptVariableInitializer, failure.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
        using var compiled = CreateAndRunVirtualMachine(
            $"select ({expression}) as Projected from values {{(Seed:1)}} s",
            schema
        );
        using var rows = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(rows, new object?[] { expected });
    }
}
