using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class PortableExecutionSemanticsBaselineTests
{
    [TestMethod]
    public void CSharpClr_WhenArithmeticIsEvaluated_ShouldPreserveCurrentResults()
    {
        var row = ExecuteSingleRow(
            "select 10 / 3 as Divided, 10 % 3 as Modulo, 1.5 + 2.25 as DecimalSum from #system.dual()");

        Assert.AreEqual(3, row[0]);
        Assert.AreEqual(1, row[1]);
        Assert.AreEqual(3.75m, row[2]);
    }

    [TestMethod]
    public void CSharpClr_WhenNullsAreEvaluated_ShouldPreserveCurrentResults()
    {
        var row = ExecuteSingleRow(
            "select 10 + null as NullArithmetic, case when null is null then 'yes' else 'no' end as NullCase from #system.dual()");

        Assert.IsNull(row[0]);
        Assert.AreEqual("yes", row[1]);
    }

    [TestMethod]
    public void CSharpClr_WhenStringsAreCompared_ShouldUseCurrentOrdinalSemantics()
    {
        var row = ExecuteSingleRow(
            "select 'alpha' = 'ALPHA' as EqualValue, case when 'a' < 'b' then 1 else 0 end as OrderedValue from #system.dual()");

        Assert.AreEqual(false, row[0]);
        Assert.AreEqual(1, row[1]);
    }

    [TestMethod]
    public void CSharpClr_WhenTemporalValuesAreEvaluated_ShouldPreserveTicksAndOffsets()
    {
        var row = ExecuteSingleRow(
            "select ToDateTime('2020-01-02') - ToDateTime('2020-01-01') as Difference, ToDateTimeOffset('2020-01-01T01:00:00+01:00') as OffsetValue from #system.dual()");

        Assert.AreEqual(TimeSpan.FromDays(1), row[0]);
        Assert.AreEqual(
            DateTimeOffset.Parse("2020-01-01T01:00:00+01:00", System.Globalization.CultureInfo.InvariantCulture),
            row[1]);
    }

    [TestMethod]
    public void CSharpClr_WhenFloatingPointValuesAreCompared_ShouldPreserveOperatorSemantics()
    {
        var signedZero = ExecuteSingleRow(
            "param(left: double, right: double) select $left = $right as EqualValue from #system.dual()",
            compiled =>
            {
                compiled.Parameters["left"] = 0d;
                compiled.Parameters["right"] = -0d;
            });
        var nan = ExecuteSingleRow(
            "param(left: double, right: double) select $left = $right as EqualValue from #system.dual()",
            compiled =>
            {
                compiled.Parameters["left"] = double.NaN;
                compiled.Parameters["right"] = double.NaN;
            });

        Assert.AreEqual(true, signedZero[0]);
        Assert.AreEqual(false, nan[0]);
    }

    private static Evaluator.Tables.Row ExecuteSingleRow(string query)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString("N"),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        return compiled.Run().Rows.Single();
    }

    private static Evaluator.Tables.Row ExecuteSingleRow(
        string query,
        Action<CompiledQuery> configure)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString("N"),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
        configure(compiled);

        return compiled.Run().Rows.Single();
    }
}
