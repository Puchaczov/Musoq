using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void SchemaNullPredicatesAndCase_ShouldDistinguishAbsentAndZeroValues()
    {
        using var table = RunBinary(
            "binary Rec { Flag: byte, Optional: byte when Flag = 1, Missing: Optional IS NULL, Present: Optional IS NOT NULL, Width: case when Optional IS NOT NULL then 2 else 1 end };",
            "r.Missing, r.Present, r.Width",
            [0],
            [1, 0],
            [1, 7]
        );

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Missing", typeof(bool)),
            ("Present", typeof(bool)),
            ("Width", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, false, 1], [false, true, 2], [false, true, 2]);
    }

    [TestMethod]
    public void SchemaNullPredicatesOnPresentValuesAndNullLiterals_ShouldReturnBooleanValues()
    {
        using var table = RunBinary(
            "binary Rec { Flag: byte, Present: Flag IS NOT NULL, Missing: Flag IS NULL, NullMissing: null IS NULL, NullPresent: null IS NOT NULL };",
            "r.Present, r.Missing, r.NullMissing, r.NullPresent",
            [0]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, false, true, false]);
    }

    [TestMethod]
    public void SchemaNullPredicateGuard_ShouldConsumeOnlyTheSelectedField()
    {
        using var table = RunBinary(
            "binary Rec { Flag: byte, Optional: byte when Flag = 1, Value: byte when Optional IS NULL, Tail: byte };",
            "r.Value, r.Tail",
            [0, 7, 99],
            [1, 0, 99]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99], [null, (byte)99]);
    }

    [TestMethod]
    public void SchemaNullPredicateInCheck_ShouldAcceptAbsentOptionalField()
    {
        using var table = RunBinary(
            "binary Rec { Flag: byte, Optional: byte when Flag = 1, Value: byte check Optional IS NULL };",
            "r.Value",
            [0, 7]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7]);
    }

    [TestMethod]
    public void SchemaNullPredicateInCheck_ShouldRejectPresentOptionalField()
    {
        var error = Assert.Throws<ParseException>(() =>
            RunBinary(
                "binary Rec { Flag: byte, Optional: byte when Flag = 1, Value: byte check Optional IS NULL };",
                "r.Value",
                [1, 0, 7]
            )
        );

        Assert.AreEqual(ParseErrorCode.ValidationFailed, error.ErrorCode);
    }

    [TestMethod]
    public void SchemaNullPredicateInRepeat_ShouldStopAndPreserveTheTail()
    {
        using var table = RunBinary(
            "binary Rec { Marker: byte when false, Items: byte repeat until Marker IS NULL, Tail: byte };",
            "r.Items.Length, r.Tail",
            [7, 99]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, (byte)99]);
    }

    [TestMethod]
    public void SchemaNullPredicateThroughAbsentNestedRecord_ShouldRemainNullSafe()
    {
        using var table = RunBinary(
            "binary Child { Value: byte }; binary Rec { Flag: byte, Child: Child when Flag = 1, Missing: Child.Value IS NULL };",
            "r.Missing",
            [0],
            [1, 0]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true], [false]);
    }

    [TestMethod]
    public void SchemaNullPredicateOnEnclosingField_ShouldPreserveTheParentNullability()
    {
        using var table = RunBinary(
            "binary Child { Missing: parent.Optional IS NULL }; binary Rec { Flag: byte, Optional: byte when Flag = 1, Child: Child };",
            "r.Child.Missing",
            [0],
            [1, 0]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true], [false]);
    }

    [TestMethod]
    public void SchemaNullPredicatesOnOptionalRecordsAndArrays_ShouldReturnPresenceBooleans()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Flag: byte, Child: Item when Flag = 1, Items: Item[1] when Flag = 1, RecordMissing: Child IS NULL, ArrayMissing: Items IS NULL };",
            "r.RecordMissing, r.ArrayMissing",
            [0],
            [1, 0, 9]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [true, true], [false, false]);
    }

    [TestMethod]
    [DataRow("Unknown IS NULL")]
    [DataRow("Later IS NOT NULL")]
    public void SchemaNullPredicateUnknownOrForwardReference_ShouldFailBinding(string expression)
    {
        var error = Assert.Throws<MusoqQueryException>(() =>
            RunBinary($"binary Rec {{ Flag: byte, Missing: {expression}, Later: byte }};", "r.Missing", [0, 7])
        );

        Assert.AreEqual(DiagnosticCode.MQ2030_UnsupportedSyntax, error.PrimaryEnvelope.Code);
    }

    [TestMethod]
    public void SchemaKeywordFieldInNullPredicate_ShouldExecuteAsAFieldReference()
    {
        using var table = RunBinary("binary Rec { Case: byte, Missing: Case IS NULL };", "r.Missing", [0]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [false]);
    }
}
