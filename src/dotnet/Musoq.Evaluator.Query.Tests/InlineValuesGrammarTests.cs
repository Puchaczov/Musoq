using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class InlineValuesGrammarTests : BasicEntityTestBase
{
    [TestMethod]
    public void StaticValuesOperators_ShouldReturnTheirNamedTypedFields()
    {
        const string sql =
            "select * from values {(Text:'a' || 'b', Compared:1 < 2, Fallback:null ?? 7, Bits:5 ^ 3, Flag:not true, Different:2 is distinct from 1)} v";
        using var compiled = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var rows = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(rows, ["ab", true, 7, 6, false, true]);
        TableMaterializationTestHelper.AssertColumns(
            rows,
            ("Text", typeof(string)),
            ("Compared", typeof(bool)),
            ("Fallback", typeof(int)),
            ("Bits", typeof(int)),
            ("Flag", typeof(bool)),
            ("Different", typeof(bool))
        );
    }

    [TestMethod]
    [DataRow("case when 1=1 then 7 else 8 end")]
    [DataRow("2 between 1 and 3")]
    [DataRow("null is null")]
    public void UnsupportedValuesExpression_ShouldReportTheFieldDuringBinding(string expression)
    {
        var query = $"select X from values {{(X:{expression})}} v";
        var failure = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>())
        );
        Assert.AreEqual(DiagnosticCode.MQ3055_InvalidValuesSource, failure.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, failure.PrimaryEnvelope.Phase);
        Assert.AreEqual("X", failure.PrimaryEnvelope.Arguments["field"]);
    }

    [TestMethod]
    public void ScalarParameterDefault_ShouldTakeASignedLiteralAndRejectArithmetic()
    {
        var schema = new Dictionary<string, IEnumerable<BasicEntity>>();
        var failure = Assert.ThrowsExactly<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("param(n:int=1+2) select $n from values {(Seed:1)} s", schema)
        );
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, failure.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, failure.PrimaryEnvelope.Phase);
        using var compiled = CreateAndRunVirtualMachine(
            "param(n:int=-1) select $n as Value from values {(Seed:1)} s",
            schema
        );
        using var rows = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(rows, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(rows, [-1]);
    }
}
