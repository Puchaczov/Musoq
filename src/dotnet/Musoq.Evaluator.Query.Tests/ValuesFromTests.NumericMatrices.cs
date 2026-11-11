using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ValuesFromTests
{
    [TestMethod]
    public void ValuesSource_WithCompatibleNumericRows_ShouldInferPromotedTypes()
    {
        const string query = (
            "\n"
            + "select numbers.SmallSigned,\n"
            + "       numbers.IntLong,\n"
            + "       numbers.IntUInt,\n"
            + "       numbers.UIntLong,\n"
            + "       numbers.UIntULong,\n"
            + "       numbers.IntDecimal,\n"
            + "       numbers.HexInt,\n"
            + "       numbers.NullUInt\n"
            + "from values {\n"
            + "    (\n"
            + "        SmallSigned: 1b,\n"
            + "        IntLong: 1,\n"
            + "        IntUInt: 1,\n"
            + "        UIntLong: 1ui,\n"
            + "        UIntULong: 1ui,\n"
            + "        IntDecimal: 1,\n"
            + "        HexInt: 0x10,\n"
            + "        NullUInt: null\n"
            + "    ),\n"
            + "    (\n"
            + "        SmallSigned: 2s,\n"
            + "        IntLong: 2l,\n"
            + "        IntUInt: 2ui,\n"
            + "        UIntLong: 2l,\n"
            + "        UIntULong: 2ul,\n"
            + "        IntDecimal: 2d,\n"
            + "        HexInt: 2,\n"
            + "        NullUInt: 3ui\n"
            + "    )\n"
            + "} numbers\n"
            + "order by numbers.IntLong"
        );

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "SmallSigned", typeof(int));
        AssertColumn(table, 1, "IntLong", typeof(long));
        AssertColumn(table, 2, "IntUInt", typeof(uint));
        AssertColumn(table, 3, "UIntLong", typeof(long));
        AssertColumn(table, 4, "UIntULong", typeof(ulong));
        AssertColumn(table, 5, "IntDecimal", typeof(decimal));
        AssertColumn(table, 6, "HexInt", typeof(long));
        AssertColumn(table, 7, "NullUInt", typeof(uint?));

        Assert.AreEqual(1, table[0].ReadValue(0));
        Assert.AreEqual(1L, table[0].ReadValue(1));
        Assert.AreEqual(1u, table[0].ReadValue(2));
        Assert.AreEqual(1L, table[0].ReadValue(3));
        Assert.AreEqual(1UL, table[0].ReadValue(4));
        Assert.AreEqual(1m, table[0].ReadValue(5));
        Assert.AreEqual(16L, table[0].ReadValue(6));
        Assert.IsNull(table[0].ReadValue(7));
        Assert.AreEqual(2, table[1].ReadValue(0));
        Assert.AreEqual(2L, table[1].ReadValue(1));
        Assert.AreEqual(2u, table[1].ReadValue(2));
        Assert.AreEqual(2L, table[1].ReadValue(3));
        Assert.AreEqual(2UL, table[1].ReadValue(4));
        Assert.AreEqual(2m, table[1].ReadValue(5));
        Assert.AreEqual(2L, table[1].ReadValue(6));
        Assert.AreEqual(3u, table[1].ReadValue(7));
    }

    [TestMethod]
    public void ValuesSource_WithNumericLiteralPairMatrix_ShouldInferPromotedTypes()
    {
        var pairs = new List<(string Column, string LeftLiteral, string RightLiteral, Type ExpectedType)>();
        foreach (var left in NumericLiteralCases())
        foreach (var right in NumericLiteralCases())
        {
            if (!TryResolveExpectedValuesNumericColumnType(left.Type, right.Type, out var expectedType))
                continue;

            pairs.Add(
                (
                    $"{left.Name}_{right.Name}",
                    left.Literal,
                    right.Literal,
                    expectedType ?? throw new AssertFailedException("Expected a numeric promotion type.")
                )
            );
        }

        var selectList = string.Join(",\n       ", pairs.Select(pair => $"numbers.{pair.Column}"));
        var firstRowFields = string.Join(",\n        ", pairs.Select(pair => $"{pair.Column}: {pair.LeftLiteral}"));
        var secondRowFields = string.Join(",\n        ", pairs.Select(pair => $"{pair.Column}: {pair.RightLiteral}"));
        var query =
            $"\nselect {selectList}\nfrom values {{\n    (\n        {firstRowFields}\n    ),\n    (\n        {secondRowFields}\n    )\n}} numbers";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(pairs.Count, table.Columns.Count());

        for (var i = 0; i < pairs.Count; i++)
            AssertColumn(table, i, $"{pairs[i].Column}", pairs[i].ExpectedType);
    }

    [TestMethod]
    public void ValuesSource_WithNullAndEachNumericLiteral_ShouldInferNullableNumericTypes()
    {
        var cases = NumericLiteralCases();
        var selectList = string.Join(",\n       ", cases.Select(testCase => $"numbers.Null_{testCase.Name}"));
        var nullFields = string.Join(",\n        ", cases.Select(testCase => $"Null_{testCase.Name}: null"));
        var numericFields = string.Join(
            ",\n        ",
            cases.Select(testCase => $"Null_{testCase.Name}: {testCase.Literal}")
        );
        var query =
            $"\nselect {selectList}\nfrom values {{\n    (\n        {nullFields}\n    ),\n    (\n        {numericFields}\n    )\n}} numbers";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(cases.Length, table.Columns.Count());

        for (var i = 0; i < cases.Length; i++)
        {
            AssertColumn(table, i, $"Null_{cases[i].Name}", typeof(Nullable<>).MakeGenericType(cases[i].Type));
            Assert.IsNull(table[0].ReadValue(i));
            Assert.IsNotNull(table[1].ReadValue(i));
        }
    }

    [TestMethod]
    public void ValuesSource_WithULongAndSignedInteger_ShouldThrow()
    {
        const string query = (
            "\n"
            + "from values {\n"
            + "    ( Value: 1ul ),\n"
            + "    ( Value: 2 )\n"
            + "} numbers\n"
            + "select numbers.Value"
        );

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "cannot safely mix ulong with signed integer values"
        );
        MusoqExceptionAssertions.AssertHasGuidance(ex);
    }

    [TestMethod]
    public void ValuesSource_WithUnsafeNumericLiteralPairMatrix_ShouldThrow()
    {
        var ulongCase = NumericLiteralCases().Single(testCase => testCase.Type == typeof(ulong));
        var signedCases = NumericLiteralCases().Where(testCase => IsSignedIntegerType(testCase.Type)).ToArray();
        var unsafePairs = signedCases
            .Select(testCase => (Left: testCase, Right: ulongCase))
            .Concat(signedCases.Select(testCase => (Left: ulongCase, Right: testCase)))
            .ToArray();

        Assert.IsNotEmpty(unsafePairs, "The signed/ulong rejection matrix must contain cases.");
        foreach (var pair in unsafePairs)
        {
            var query =
                $"\nfrom values {{\n    ( Value: {pair.Left.Literal} ),\n    ( Value: {pair.Right.Literal} )\n}} numbers\nselect numbers.Value";

            var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

            MusoqExceptionAssertions.AssertSingleError(
                ex,
                DiagnosticCode.MQ3055_InvalidValuesSource,
                DiagnosticPhase.Bind,
                "cannot safely mix ulong with signed integer values"
            );
            MusoqExceptionAssertions.AssertHasGuidance(ex);
        }
    }
}
