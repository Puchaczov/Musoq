using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void SchemaBooleanExpressions_ShouldFollowThreeValuedTruthTable()
    {
        var table = RunBinary("binary Rec { Flag: byte, Optional: int le when Flag = 1, Unknown: Optional = null, Negated: NOT Unknown, FalseAnd: Unknown AND false, TrueAnd: Unknown AND true, FalseOr: Unknown OR false, TrueOr: Unknown OR true };",
            "r.Unknown, r.Negated, r.FalseAnd, r.TrueAnd, r.FalseOr, r.TrueOr", [0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null, false, null, null, true]);
    }

    [TestMethod]
    public void SchemaCaseNullableAndNumericArms_ShouldInferConcreteTypes()
    {
        var table = RunBinary("binary Rec { Flag: byte, Number: double le, Mixed: CASE WHEN Flag = 1 THEN Number ELSE 7 END, Maybe: CASE WHEN Flag = 1 THEN 9 ELSE null END };",
            "r.Mixed, r.Maybe", [0, 0, 0, 0, 0, 0, 0, 248, 63]);

        TableMaterializationTestHelper.AssertColumns(table, ("Mixed", typeof(double)), ("Maybe", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [7d, null]);
    }

    [TestMethod]
    public void ParentContextBeforeLocalShadow_ShouldKeepEachReferenceMeaning()
    {
        var table = RunBinary("binary Header { Count: byte }; binary Child { Before: parent.Count, parent: Header, After: parent.Count }; binary Rec { Count: byte, Body: Child };",
            "r.Body.Before, r.Body.After", [2, 3]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)2, (byte)3]);
    }

    [TestMethod]
    public void ParentDependentRepeat_ShouldUseContainingRecordForEveryElement()
    {
        var table = RunBinaryQuery("binary Child { Values: byte[parent.Count] }; binary Rec { Count: byte, Rest: Child repeat until eof }; select i.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Rest c cross apply c.Values i", [1, 10, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void ParentDependentRepeatCondition_ShouldReadEnclosingStopValue()
    {
        var table = RunBinaryQuery("binary Body { Values: byte repeat until Values = parent.Stop }; binary Rec { Stop: byte, Body: Body, Tail: byte }; select i.Value, r.Tail from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Values i", [0, 10, 0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10, (byte)99], [(byte)0, (byte)99]);
    }

    [TestMethod]
    public void InlineComputedCase_ShouldUseParentContext()
    {
        var table = RunBinary("binary Rec { Flag: byte, Body: { Value: byte, Derived: CASE WHEN parent.Flag = 1 THEN Value ELSE 9 END } };",
            "r.Body.Derived", [1, 7]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [7]);
    }

    [TestMethod]
    public void NullPosition_ShouldReportInvalidPosition()
    {
        var exception = Assert.Throws<ParseException>(() => RunBinary("binary Rec { Flag: byte, Position: int le when Flag = 1, V: byte at Position };", "r.V", [0]));

        Assert.AreEqual(ParseErrorCode.InvalidPosition, exception.ErrorCode);
    }

    [TestMethod]
    public void ConditionalFieldCheck_ShouldValidateItsParsedValue()
    {
        var table = RunBinary("binary Rec { Flag: byte, V: byte when Flag = 1 check V > 0, Tail: byte };",
            "r.V, r.Tail", [1, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99], [null, (byte)99]);
    }

    [TestMethod]
    public void ConditionalStringComparison_ShouldPreserveUnknownThroughComputedGuard()
    {
        var table = RunBinary("binary Rec { Flag: byte, Text: string[1] ascii when Flag = 1, Enabled: Text = 'x', V: byte when Enabled, Tail: byte };",
            "r.Enabled, r.V, r.Tail", [1, 120, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, (byte)7, (byte)99], [null, null, (byte)99]);
    }

    [TestMethod]
    public void AbsentConditionalRecordStringPath_ShouldYieldNull()
    {
        var table = RunBinary("binary Body { Name: string[1] ascii }; binary Rec { Flag: byte, Body: Body when Flag = 1 };",
            "r.Body.Name", [1, 120], [0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["x"], new object?[] { null });
    }

    [TestMethod]
    public void ParentContextFromAbsentHeader_ShouldCaptureNullWithoutDereferencing()
    {
        var table = RunBinary("binary Header { Count: byte }; binary Body { Values: byte[parent.Header.Count] when parent.Enabled }; binary Rec { Flag: byte, Header: Header when Flag = 1, Enabled: Flag = 1, Body: Body, Tail: byte };",
            "r.Tail", [0, 99], [1, 1, 7, 88]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)99], [(byte)88]);
    }

    [TestMethod]
    public void UnknownCheck_ShouldReportValidationFailure()
    {
        var exception = Assert.Throws<ParseException>(() => RunBinary("binary Rec { Flag: byte, Optional: int le when Flag = 1, Tail: byte check Optional > 0 };", "r.Tail", [0, 99]));

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
    }

    [TestMethod]
    public void InlineCaseBooleanGuard_ShouldUseComputedType()
    {
        var table = RunBinary("binary Rec { Flag: byte, Body: { Enabled: CASE WHEN parent.Flag = 1 THEN true ELSE false END, V: byte when Enabled }, Tail: byte };",
            "r.Body.V, r.Tail", [1, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99], [null, (byte)99]);
    }

    [TestMethod]
    [DataRow("CASE WHEN Flag = 1 THEN 7 ELSE END", "MQ2001")]
    [DataRow("CASE WHEN Flag = 1 THEN 7 ELSE 'bad' END", "MQ4006")]
    public void InvalidSchemaCase_ShouldReportDiagnostic(string expression, string code)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary($"binary Rec {{ Flag: byte, V: {expression} }};", "r.V", [1]));

        StringAssert.Contains(exception.Message, code);
    }
}
