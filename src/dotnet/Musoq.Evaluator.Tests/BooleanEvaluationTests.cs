using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class BooleanEvaluationTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnknownLogicalResults_ShouldAdvertiseNullableBooleanMetadata()
    {
        const string query = "select null and true as A, null or false as O, not null as N from values { (Id: 1) } v";
        using var compiled = CreateAndRunVirtualMachine(query, EmptySources());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("A", typeof(bool?)), ("O", typeof(bool?)), ("N", typeof(bool?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, new object?[] { null, null, null });
    }

    [TestMethod]
    [DataRow("false and false", false)]
    [DataRow("false and true", false)]
    [DataRow("false and null", false)]
    [DataRow("true and false", false)]
    [DataRow("true and true", true)]
    [DataRow("true and null", null)]
    [DataRow("null and false", false)]
    [DataRow("null and true", null)]
    [DataRow("null and null", null)]
    [DataRow("false or false", false)]
    [DataRow("false or true", true)]
    [DataRow("false or null", null)]
    [DataRow("true or false", true)]
    [DataRow("true or true", true)]
    [DataRow("true or null", true)]
    [DataRow("null or false", null)]
    [DataRow("null or true", true)]
    [DataRow("null or null", null)]
    [DataRow("not false", true)]
    [DataRow("not true", false)]
    [DataRow("not null", null)]
    public void LogicalOperators_ShouldFollowThreeValuedTruthTables(string expression, bool? expected)
    {
        var query = $"select {expression} as Value from values {{ (N: 1) }} v";
        using var folded = CreateAndRunVirtualMachine(query, EmptySources(), new CompilationOptions(useConstantFolding: true));
        using var unfolded = CreateAndRunVirtualMachine(query, EmptySources(), new CompilationOptions(useConstantFolding: false));
        using var foldedTable = folded.Run(TestContext.CancellationToken);
        using var unfoldedTable = unfolded.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(foldedTable, new object?[] { expected });
        TableMaterializationTestHelper.AssertRowsUnordered(unfoldedTable, new object?[] { expected });
    }

    [TestMethod]
    [DataRow("TextValue = 'yes' and TextValue::int > 0", false)]
    [DataRow("TextValue <> 'yes' or TextValue::int > 0", true)]
    [DataRow("false and (1 / 0 > 0)", false)]
    [DataRow("true or (1 / 0 > 0)", true)]
    public void DecisiveLeftOperand_ShouldNotEvaluateFailingRightOperand(string expression, bool expected)
    {
        var query = $"select {expression} as Value from values {{ (TextValue: 'invalid') }} v";
        using var compiled = CreateAndRunVirtualMachine(query, EmptySources(),
            TestCompilationOptions.WithStabilityAwareScalarReuse().WithLoopInvariantCodeMotion());
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected]);
    }

    [TestMethod]
    [DataRow("TextValue = 'yes' and TextValue::int > 0", 0)]
    [DataRow("TextValue <> 'yes' or TextValue::int > 0", 1)]
    public void GuardedWherePredicate_ShouldPreserveSkippedEvaluation(string predicate, int expectedRows)
    {
        var query = $"select TextValue from values {{ (TextValue: 'invalid') }} v where {predicate}";
        using var compiled = CreateAndRunVirtualMachine(query, EmptySources(),
            TestCompilationOptions.WithStabilityAwareScalarReuse().WithLoopInvariantCodeMotion());
        using var table = compiled.Run(TestContext.CancellationToken);

        Assert.AreEqual(expectedRows, table.Count);
    }

    [TestMethod]
    [DoNotParallelize]
    [DataRow("(NextValue() > 0) or null", true)]
    [DataRow("(NextValue() < 0) and null", false)]
    [DataRow("null and (NextValue() < 0)", false)]
    [DataRow("null or (NextValue() > 0)", true)]
    public void RequiredLogicalOperand_ShouldBeEvaluatedOnce(string expression, bool expected)
    {
        var baseline = new Library().NextValue();
        var query = $"select {expression} as Value, NextValue() as Calls from #A.Entities()";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity()]
        });
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, [expected, baseline + 2]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources() => new();
}
