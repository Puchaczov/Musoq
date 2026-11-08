using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void IndexedRecordMemberArithmetic_ShouldRetainElementType()
    {
        var table = RunBinary("binary Item { Value: byte }; binary Rec { Entries: Item[1], Next: Entries[0].Value + 1 };",
            "r.Next", [42]);
        TableMaterializationTestHelper.AssertColumns(table, ("Next", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [43]);
    }

    [TestMethod]
    public void FloatingStrictCasts_ShouldKeepNullableMetadataAndCorePrecedence()
    {
        var table = RunBinary("binary Rec { F: float le, Converted: F::Decimal, Sum: F::Decimal + 0.5, Chain: (F + F)::Decimal::Double };",
            "r.Converted, r.Sum, r.Chain", [0, 0, 192, 63]);
        TableMaterializationTestHelper.AssertColumns(table, ("Converted", typeof(decimal?)), ("Sum", typeof(decimal?)), ("Chain", typeof(double?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1.5m, 2m, 3d]);
    }

    [TestMethod]
    public void StrictCastOfEnclosingValue_ShouldPreserveParentBinding()
    {
        var table = RunBinary("binary Body { Converted: parent.F::Decimal }; binary Rec { F: float le, Body: Body };",
            "r.Body.Converted as Converted", [0, 0, 192, 63]);
        TableMaterializationTestHelper.AssertColumns(table, ("Converted", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1.5m]);
    }

    [TestMethod]
    public void StrictCast_ShouldPreserveNullAndSkipUnselectedRuntimeConversion()
    {
        var table = RunBinary("binary Rec { Text: string[1] ascii, Missing: null::Decimal, Safe: CASE WHEN Text = 'X' THEN 7 ELSE Text::Int32 END };",
            "r.Missing, r.Safe", [88]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, 7]);
    }

    [TestMethod]
    public void StrictCastInRepeatCondition_ShouldUseTheCurrentElement()
    {
        var table = RunBinary("binary Item { F: float le }; binary Rec { Items: Item repeat until Items[-1].F::Decimal = 0.5, Tail: byte };",
            "r.Items.Length, r.Tail", [0, 0, 0, 0, 0, 0, 0, 63, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, (byte)99]);
    }

    [TestMethod]
    public void StrictBooleanCast_ShouldWorkThroughAComputedGuard()
    {
        var table = RunBinary("binary Rec { Flag: byte, Enabled: Flag::Boolean, Value: byte when Enabled, Tail: byte };",
            "r.Value, r.Tail", [0, 99], [1, 7, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, (byte)99], [(byte)7, (byte)99]);
    }

    [TestMethod]
    [DataRow("'bad'::Decimal", DiagnosticCode.MQ3091_InvalidConstantCast)]
    [DataRow("1::Unsupported", DiagnosticCode.MQ3090_UnsupportedCastTarget)]
    public void InvalidSchemaStrictCast_ShouldReportBindingDiagnostic(string expression, DiagnosticCode code)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            RunBinary($"binary Rec {{ Value: {expression} }};", "r.Value", []));
        Assert.AreEqual(code, exception.PrimaryEnvelope.Code);
    }

    [TestMethod]
    [DataRow("+")]
    [DataRow("-")]
    [DataRow("*")]
    [DataRow("/")]
    [DataRow("%")]
    public void ImplicitFloatingDecimalArithmetic_ShouldRejectBothOperandOrders(string operation)
    {
        foreach (var type in new[] { "float le", "double le" })
        foreach (var expression in new[] { $"F {operation} 0.25", $"0.25 {operation} F" })
        {
            var exception = Assert.Throws<MusoqQueryException>(() =>
                RunBinary($"binary Rec {{ F: {type}, Value: = {expression} }};", "r.Value", new byte[8]));
            Assert.AreEqual(DiagnosticCode.MQ3007_InvalidOperandTypes, exception.PrimaryEnvelope.Code, expression);
        }
    }

    [TestMethod]
    [DataRow("F = 0.5", true)]
    [DataRow("0.5 = F", true)]
    [DataRow("F <> 0.5", false)]
    [DataRow("0.5 <> F", false)]
    [DataRow("F < 0.75", true)]
    [DataRow("0.75 > F", true)]
    [DataRow("F <= 0.5", true)]
    [DataRow("0.5 >= F", true)]
    [DataRow("F > 0.75", false)]
    [DataRow("0.75 < F", false)]
    [DataRow("F >= 0.5", true)]
    [DataRow("0.5 <= F", true)]
    public void ImplicitFloatingDecimalComparison_ShouldPromoteBothOperandOrders(string expression, bool expected)
    {
        var singleResult = RunBinary($"binary Rec {{ F: float le, Value: = {expression} }};", "r.Value", [0, 0, 0, 63]);
        var doubleResult = RunBinary($"binary Rec {{ F: double le, Value: = {expression} }};", "r.Value", [0, 0, 0, 0, 0, 0, 224, 63]);
        TableMaterializationTestHelper.AssertRowsInOrder(singleResult, [expected]);
        TableMaterializationTestHelper.AssertRowsInOrder(doubleResult, [expected]);
    }
}
