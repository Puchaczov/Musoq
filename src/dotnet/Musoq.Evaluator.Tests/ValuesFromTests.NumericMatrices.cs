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
        const string query = @"
select numbers.SmallSigned,
       numbers.IntLong,
       numbers.IntUInt,
       numbers.UIntLong,
       numbers.UIntULong,
       numbers.IntDecimal,
       numbers.HexInt,
       numbers.NullUInt
from values {
    {
        SmallSigned: 1b,
        IntLong: 1,
        IntUInt: 1,
        UIntLong: 1ui,
        UIntULong: 1ui,
        IntDecimal: 1,
        HexInt: 0x10,
        NullUInt: null
    },
    {
        SmallSigned: 2s,
        IntLong: 2l,
        IntUInt: 2ui,
        UIntLong: 2l,
        UIntULong: 2ul,
        IntDecimal: 2d,
        HexInt: 2,
        NullUInt: 3ui
    }
} numbers
order by numbers.IntLong";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        AssertColumn(table, 0, "numbers.SmallSigned", typeof(int));
        AssertColumn(table, 1, "numbers.IntLong", typeof(long));
        AssertColumn(table, 2, "numbers.IntUInt", typeof(uint));
        AssertColumn(table, 3, "numbers.UIntLong", typeof(long));
        AssertColumn(table, 4, "numbers.UIntULong", typeof(ulong));
        AssertColumn(table, 5, "numbers.IntDecimal", typeof(decimal));
        AssertColumn(table, 6, "numbers.HexInt", typeof(long));
        AssertColumn(table, 7, "numbers.NullUInt", typeof(uint?));

        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(1u, table[0][2]);
        Assert.AreEqual(1L, table[0][3]);
        Assert.AreEqual(1UL, table[0][4]);
        Assert.AreEqual(1m, table[0][5]);
        Assert.AreEqual(16L, table[0][6]);
        Assert.IsNull(table[0][7]);
        Assert.AreEqual(2, table[1][0]);
        Assert.AreEqual(2L, table[1][1]);
        Assert.AreEqual(2u, table[1][2]);
        Assert.AreEqual(2L, table[1][3]);
        Assert.AreEqual(2UL, table[1][4]);
        Assert.AreEqual(2m, table[1][5]);
        Assert.AreEqual(2L, table[1][6]);
        Assert.AreEqual(3u, table[1][7]);
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

            pairs.Add(($"{left.Name}_{right.Name}", left.Literal, right.Literal,
                expectedType ?? throw new AssertFailedException("Expected a numeric promotion type.")));
        }

        var selectList = string.Join(",\n       ", pairs.Select(pair => $"numbers.{pair.Column}"));
        var firstRowFields = string.Join(",\n        ", pairs.Select(pair => $"{pair.Column}: {pair.LeftLiteral}"));
        var secondRowFields = string.Join(",\n        ", pairs.Select(pair => $"{pair.Column}: {pair.RightLiteral}"));
        var query = $@"
select {selectList}
from values {{
    {{
        {firstRowFields}
    }},
    {{
        {secondRowFields}
    }}
}} numbers";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(pairs.Count, table.Columns.Count());

        for (var i = 0; i < pairs.Count; i++)
            AssertColumn(table, i, $"numbers.{pairs[i].Column}", pairs[i].ExpectedType);
    }

    [TestMethod]
    public void ValuesSource_WithNullAndEachNumericLiteral_ShouldInferNullableNumericTypes()
    {
        var cases = NumericLiteralCases();
        var selectList = string.Join(",\n       ", cases.Select(testCase => $"numbers.Null_{testCase.Name}"));
        var nullFields = string.Join(",\n        ", cases.Select(testCase => $"Null_{testCase.Name}: null"));
        var numericFields = string.Join(",\n        ", cases.Select(testCase => $"Null_{testCase.Name}: {testCase.Literal}"));
        var query = $@"
select {selectList}
from values {{
    {{
        {nullFields}
    }},
    {{
        {numericFields}
    }}
}} numbers";

        var vm = CreateAndRunVirtualMachine(query, EmptySources());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(cases.Length, table.Columns.Count());

        for (var i = 0; i < cases.Length; i++)
        {
            AssertColumn(table, i, $"numbers.Null_{cases[i].Name}", typeof(Nullable<>).MakeGenericType(cases[i].Type));
            Assert.IsNull(table[0][i]);
            Assert.IsNotNull(table[1][i]);
        }
    }

    [TestMethod]
    public void ValuesSource_WithULongAndSignedInteger_ShouldThrow()
    {
        const string query = @"
from values {
    { Value: 1ul },
    { Value: 2 }
} numbers
select numbers.Value";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

        MusoqExceptionAssertions.AssertSingleError(
            ex,
            DiagnosticCode.MQ3055_InvalidValuesSource,
            DiagnosticPhase.Bind,
            "cannot safely mix ulong with signed integer values");
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

        foreach (var pair in unsafePairs)
        {
            var query = $@"
from values {{
    {{ Value: {pair.Left.Literal} }},
    {{ Value: {pair.Right.Literal} }}
}} numbers
select numbers.Value";

            var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, EmptySources()));

            MusoqExceptionAssertions.AssertSingleError(
                ex,
                DiagnosticCode.MQ3055_InvalidValuesSource,
                DiagnosticPhase.Bind,
                "cannot safely mix ulong with signed integer values");
            MusoqExceptionAssertions.AssertHasGuidance(ex);
        }
    }

}
