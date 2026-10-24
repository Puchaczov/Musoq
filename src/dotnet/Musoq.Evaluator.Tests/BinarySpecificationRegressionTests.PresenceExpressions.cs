using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Interpreters;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("false and (1 / Zero > Missing)", false)]
    [DataRow("true or (1 / Zero > Missing)", true)]
    [DataRow("false and false", false)]
    [DataRow("false and true", false)]
    [DataRow("true and false", false)]
    [DataRow("true and true", true)]
    [DataRow("true and null", null)]
    [DataRow("null and null", null)]
    [DataRow("false or false", false)]
    [DataRow("false or true", true)]
    [DataRow("true or false", true)]
    [DataRow("true or true", true)]
    [DataRow("false or null", null)]
    [DataRow("null or null", null)]
    [DataRow("not null", null)]
    [DataRow("not false", true)]
    [DataRow("not true", false)]
    [DataRow("null and false", false)]
    [DataRow("null and true", null)]
    [DataRow("null or false", null)]
    [DataRow("null or true", true)]
    [DataRow("false and null", false)]
    [DataRow("true or null", true)]
    public void SchemaLogicalExpressions_ShouldPreserveLazyThreeValuedResults(string expression, bool? expected)
    {
        using var table = RunBinary($"binary Rec {{ Flag: byte, Zero: byte, Missing: int le when Flag = 1, Value: {expression} }};",
            "r.Value", [0, 0]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [expected]);
    }

    [TestMethod]
    public void AbsentLengthWithExplicitFallback_ShouldProducePresentEmptyArray()
    {
        using var table = RunBinary("binary Rec { Flag: byte, Length: int le when Flag = 1, Absent: byte[Length] when Flag = 1, Empty: byte[Length ?? 0], Tail: byte };",
            "r.Absent is null as IsAbsent, r.Empty.Length, r.Tail", [0, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, 0, (byte)99]);
    }

    [TestMethod]
    public void CoalesceWithMixedNumericTypes_ShouldUseLosslessCommonType()
    {
        using var table = RunBinary("binary Rec { Flag: byte, Length: int le when Flag = 1, Value: Length ?? 1.5 };",
            "r.Value", [0], [1, 7, 0, 0, 0]);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1.5m], [7m]);
    }

    [TestMethod]
    [DataRow("Flag ?? (1 / Zero)")]
    [DataRow("case when Flag = 1 then 1 else (1 / Zero) end")]
    public void UnneededFallback_ShouldNotEvaluateFailingExpression(string expression)
    {
        using var table = RunBinary($"binary Rec {{ Flag: byte, Zero: byte, Value: {expression} }};", "r.Value", [1, 0]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
    }

    [TestMethod]
    public void UnguardedNullSize_ShouldRemainInvalid()
    {
        var failure = Assert.Throws<ParseException>(() =>
            RunBinary("binary Rec { Flag: byte, Length: int le when Flag = 1, Data: byte[Length] };", "r.Flag", [0]));
        Assert.AreEqual(ParseErrorCode.InvalidSize, failure.ErrorCode);
    }
    [TestMethod]
    [DataRow("1 ?? Unknown")]
    [DataRow("CASE WHEN false THEN Unknown ELSE 7 END")]
    public void UnreachableSchemaBranch_ShouldStillBindAllReferences(string expression)
    {
        var error = Assert.Throws<MusoqQueryException>(() =>
            RunBinary($"binary Rec {{ Flag: byte, Value: {expression} }};", "r.Value", [0]));
        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, error.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void IncompatibleCoalesceDomains_ShouldFailBinding()
    {
        var error = Assert.Throws<MusoqQueryException>(() =>
            RunBinary("binary Rec { Flag: byte, Number: float le when Flag = 1, Value: Number ?? 1.5 };", "r.Value", [0]));
        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, error.PrimaryEnvelope.Code);
    }
}
