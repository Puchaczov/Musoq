using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for constant folding optimization.
///     Verifies arithmetic, string, boolean, bitwise, and null folding,
///     division-by-zero detection, arithmetic overflow detection,
///     tautological/contradictory condition warnings, and passthrough for non-constant operands.
/// </summary>
[TestClass]
public class ConstantFoldingTests : GenericEntityTestBase
{
    private static readonly FoldEntity[] SingleEntitySource =
        [new FoldEntity { Name = "a", Value = 1 }];

    #region Integer arithmetic folding

    [TestMethod]
    public void WhenAddingTwoIntegers_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 10 + 20 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(30L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenSubtractingIntegers_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 50 - 30 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(20L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenMultiplyingIntegers_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 3 * 7 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(21L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenDividingIntegers_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 10 / 3 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenModuloIntegers_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 10 % 3 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenComplexArithmetic_ShouldFoldRecursively()
    {
        var vm = CreateAndRunVirtualMachine("select (2 + 3) * (10 - 4) from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(30L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenNestedArithmetic_ShouldFoldCompletely()
    {
        var vm = CreateAndRunVirtualMachine("select 1 + 2 + 3 + 4 + 5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    #endregion

    #region Decimal arithmetic folding

    [TestMethod]
    public void WhenAddingDecimals_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 1.5 + 2.5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4.0m, Convert.ToDecimal(table[0][0]));
    }

    [TestMethod]
    public void WhenMultiplyingDecimals_ShouldFoldToConstant()
    {
        var vm = CreateAndRunVirtualMachine("select 2.5 * 4.0 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(10.0m, Convert.ToDecimal(table[0][0]));
    }

    [TestMethod]
    public void WhenMixingIntAndDecimal_ShouldPromoteToDecimal()
    {
        var vm = CreateAndRunVirtualMachine("select 10 + 2.5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(12.5m, Convert.ToDecimal(table[0][0]));
    }

    #endregion

    #region Division by zero detection

    [TestMethod]
    public void WhenDivisionByZeroConstant_ShouldThrowDivisionByZero()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 10 / 0 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenModuloByZeroConstant_ShouldThrowDivisionByZero()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 10 % 0 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenDivisionByZeroInWhereClause_ShouldThrowDivisionByZero()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select Name from #schema.first() where 10 / 0 > 1", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenDivisionByZeroNested_ShouldThrowDivisionByZero()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 1 + 10 / 0 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenDivisionByZeroDecimal_ShouldThrowDivisionByZero()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 10.5 / 0.0 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenDivisionByNonZero_ShouldNotThrow()
    {
        var vm = CreateAndRunVirtualMachine("select 10 / 2 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(5L, Convert.ToInt64(table[0][0]));
    }

    #endregion

    #region String concatenation folding

    [TestMethod]
    public void WhenConcatenatingStringLiterals_ShouldFoldToString()
    {
        var vm = CreateAndRunVirtualMachine("select 'hello' + ' ' + 'world' from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    [TestMethod]
    public void WhenConcatenatingMultipleStrings_ShouldFoldAll()
    {
        var vm = CreateAndRunVirtualMachine("select 'a' + 'b' + 'c' + 'd' from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("abcd", table[0][0]);
    }

    #endregion

    #region Adjacent string constant folding ('text' + variable + 'text' + 'othertext')

    [TestMethod]
    public void WhenAdjacentStringConstantsAfterVariable_ShouldMerge()
    {
        var source = new[] { new FoldEntity { Name = "hello", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'prefix:' + Name + ' - suffix' + 'More' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("prefix:hello - suffixMore", table[0][0]);
    }

    [TestMethod]
    public void WhenAdjacentConstantsOnBothSides_ShouldMergeOnRight()
    {
        var source = new[] { new FoldEntity { Name = "x", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'aaa' + Name + 'bbb' + 'ccc' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("aaaxbbbccc", table[0][0]);
    }

    [TestMethod]
    public void WhenThreeAdjacentConstants_ShouldMergeAllOnRight()
    {
        var source = new[] { new FoldEntity { Name = "X", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select Name + 'a' + 'b' + 'c' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Xabc", table[0][0]);
    }

    [TestMethod]
    public void WhenVariableSurroundedByConstants_ShouldPreserveCorrectly()
    {
        var source = new[] { new FoldEntity { Name = "World", Value = 1 } };

        var vm = CreateAndRunVirtualMachine(
            "select 'Hello ' + Name + '!' from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Hello World!", table[0][0]);
    }

    [TestMethod]
    public void WhenMultipleVariablesWithConstants_ShouldOnlyFoldAdjacentConstants()
    {
        var source = new[] { new FoldEntity { Name = "A", Value = 42 } };

        var vm = CreateAndRunVirtualMachine(
            "select Name + ':' + ':' + Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A::A", table[0][0]);
    }

    #endregion

    #region Boolean folding

    [TestMethod]
    public void WhenAndWithFalse_ShouldFoldToFalse()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and false", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void WhenOrWithTrue_ShouldFoldToTrue()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where false or true", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenAndWithTrue_ShouldFoldToTrue()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and true", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenOrWithFalse_ShouldFoldToFalse()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where false or false", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(0, table.Count);
    }

    #endregion

    #region Null propagation

    [TestMethod]
    public void WhenAddingNullToConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select 10 + null from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void WhenMultiplyingNullByConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select null * 5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    [TestMethod]
    public void WhenDividingNullByConstant_ShouldFoldToNull()
    {
        var vm = CreateAndRunVirtualMachine("select null / 5 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0][0]);
    }

    #endregion

    #region Bitwise folding

    [TestMethod]
    public void WhenBitwiseAndOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xFF & 0x0F from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBitwiseOrOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xF0 | 0x0F from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(255L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenLeftShiftOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 1 << 3 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(8L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenRightShiftOnConstants_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 16 >> 2 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4L, Convert.ToInt64(table[0][0]));
    }

    #endregion

    #region Non-foldable (passthrough) expressions

    [TestMethod]
    public void WhenColumnInArithmetic_ShouldNotFoldButStillWork()
    {
        var source = new[] { new FoldEntity { Name = "a", Value = 10 } };

        var vm = CreateAndRunVirtualMachine("select Value + 5 from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenColumnInStringConcat_ShouldNotFoldButStillWork()
    {
        var source = new[] { new FoldEntity { Name = "world", Value = 1 } };

        var vm = CreateAndRunVirtualMachine("select 'hello ' + Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("hello world", table[0][0]);
    }

    [TestMethod]
    public void WhenPartiallyConstant_ShouldFoldConstantPartOnly()
    {
        var source = new[] { new FoldEntity { Name = "a", Value = 10 } };

        var vm = CreateAndRunVirtualMachine("select (2 + 3) + Value from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBooleanMixedWithColumn_ShouldNotFoldButStillWork()
    {
        var source = new[]
        {
            new FoldEntity { Name = "a", Value = 10 },
            new FoldEntity { Name = "b", Value = 0 }
        };

        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where true and Value > 5", source);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    #endregion

    #region Mixed expression types

    [TestMethod]
    public void WhenConstantInSelect_ShouldFoldArithmetic()
    {
        var vm = CreateAndRunVirtualMachine(
            "select 100 + 200 from #schema.first() where 1 + 1 > 0", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(300L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenConstantInWhere_ShouldFilterCorrectly()
    {
        var vm = CreateAndRunVirtualMachine(
            "select Name from #schema.first() where 1 + 1 > 0", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void WhenHexLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0xFF + 0x01 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(256L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenBinaryLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0b1010 + 0b0101 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenOctalLiteralsInArithmetic_ShouldFold()
    {
        var vm = CreateAndRunVirtualMachine("select 0o10 + 0o7 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15L, Convert.ToInt64(table[0][0]));
    }

    [TestMethod]
    public void WhenMultipleRows_ShouldFoldOnceAndApplyToAll()
    {
        var source = new[]
        {
            new FoldEntity { Name = "a", Value = 1 },
            new FoldEntity { Name = "b", Value = 2 },
            new FoldEntity { Name = "c", Value = 3 }
        };

        var vm = CreateAndRunVirtualMachine("select 100 * 3, Name from #schema.first()", source);
        var table = vm.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(300L, Convert.ToInt64(table[0][0]));
        Assert.AreEqual(300L, Convert.ToInt64(table[1][0]));
        Assert.AreEqual(300L, Convert.ToInt64(table[2][0]));
    }

    #endregion

    #region Test entity

    public class FoldEntity
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    #endregion

    #region Arithmetic overflow and underflow detection (MQ3032)

    // Sub-int types (sbyte, byte, short, ushort) promote to int for arithmetic (C# spec).
    // Overflow is detected at the int/uint/long/ulong/decimal boundaries.

    // --- sbyte (b suffix): promotes to int, folds correctly ---

    [TestMethod]
    public void WhenSbyteAdditionWithinIntRange_ShouldFoldToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 127b + 1b from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(128, table[0][0]);
    }

    [TestMethod]
    public void WhenSbyteSubtractionBelowZero_ShouldFoldToNegativeInt()
    {
        var vm = CreateAndRunVirtualMachine("select -128b - 1b from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-129, table[0][0]);
    }

    // --- byte (ub suffix): promotes to int, folds correctly ---

    [TestMethod]
    public void WhenByteAdditionBeyondByteMax_ShouldFoldToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 255ub + 1ub from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(256, table[0][0]);
    }

    [TestMethod]
    public void WhenByteSubtractionBelowZero_ShouldFoldToNegativeInt()
    {
        var vm = CreateAndRunVirtualMachine("select 0ub - 1ub from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-1, table[0][0]);
    }

    // --- short (s suffix): promotes to int, folds correctly ---

    [TestMethod]
    public void WhenShortAdditionBeyondShortMax_ShouldFoldToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 32767s + 1s from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(32768, table[0][0]);
    }

    [TestMethod]
    public void WhenShortSubtractionBelowShortMin_ShouldFoldToNegativeInt()
    {
        var vm = CreateAndRunVirtualMachine("select -32768s - 1s from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-32769, table[0][0]);
    }

    // --- ushort (us suffix): promotes to int, folds correctly ---

    [TestMethod]
    public void WhenUshortAdditionBeyondUshortMax_ShouldFoldToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 65535us + 1us from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(65536, table[0][0]);
    }

    [TestMethod]
    public void WhenUshortSubtractionBelowZero_ShouldFoldToNegativeInt()
    {
        var vm = CreateAndRunVirtualMachine("select 0us - 1us from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(-1, table[0][0]);
    }

    // --- int (i suffix / default for small literals): range -2147483648 to 2147483647 ---

    [TestMethod]
    public void WhenIntAdditionOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 2147483647i + 1i from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenIntSubtractionUnderflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select -2147483648i - 1i from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenIntMultiplicationOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 2000000000i * 2i from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenIntAdditionAtBoundary_ShouldFoldSuccessfully()
    {
        var vm = CreateAndRunVirtualMachine("select 2147483646i + 1i from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2147483647, table[0][0]);
    }

    // --- uint (ui suffix): range 0 to 4294967295 ---

    [TestMethod]
    public void WhenUintAdditionOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 4294967295ui + 1ui from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenUintSubtractionUnderflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 0ui - 1ui from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenUintMultiplicationOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 3000000000ui * 2ui from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenUintAdditionAtBoundary_ShouldFoldSuccessfully()
    {
        var vm = CreateAndRunVirtualMachine("select 4294967294ui + 1ui from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(4294967295u, table[0][0]);
    }

    // --- long (l suffix / default for large literals): range -9223372036854775808 to 9223372036854775807 ---

    [TestMethod]
    public void WhenLongAdditionOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 9223372036854775807 + 1 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenLongMultiplicationOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 9223372036854775807 * 2 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenLongAdditionDoubleMaxOverflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select 9223372036854775807 + 9223372036854775807 from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenLongAdditionAtBoundary_ShouldFoldSuccessfully()
    {
        var vm = CreateAndRunVirtualMachine("select 9223372036854775806 + 1 from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(9223372036854775807L, Convert.ToInt64(table[0][0]));
    }

    // --- ulong (ul suffix): range 0 to 18446744073709551615 ---

    [TestMethod]
    public void WhenUlongSubtractionUnderflows_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select 0ul - 1ul from #schema.first()", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void WhenUlongAdditionAtBoundary_ShouldFoldSuccessfully()
    {
        var vm = CreateAndRunVirtualMachine("select 1ul + 1ul from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(2ul, table[0][0]);
    }

    // --- overflow in WHERE clause ---

    [TestMethod]
    public void WhenOverflowInWhereClause_ShouldThrowArithmeticOverflow()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select Name from #schema.first() where 9223372036854775807 + 1 > 0", SingleEntitySource));

        AssertSingleError(ex, DiagnosticCode.MQ3032_ArithmeticOverflow, DiagnosticPhase.Bind);
    }

    // --- sub-int type-specific boundary (folded correctly, promoting to int) ---

    [TestMethod]
    public void WhenSbyteAdditionWithinRange_ShouldFoldCorrectly()
    {
        var vm = CreateAndRunVirtualMachine("select 50b + 20b from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(70, table[0][0]);
    }

    [TestMethod]
    public void WhenByteAdditionWithinRange_ShouldFoldCorrectly()
    {
        var vm = CreateAndRunVirtualMachine("select 100ub + 50ub from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(150, table[0][0]);
    }

    [TestMethod]
    public void WhenShortAdditionWithinRange_ShouldFoldCorrectly()
    {
        var vm = CreateAndRunVirtualMachine("select 10000s + 5000s from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(15000, table[0][0]);
    }

    [TestMethod]
    public void WhenUshortAdditionWithinRange_ShouldFoldCorrectly()
    {
        var vm = CreateAndRunVirtualMachine("select 30000us + 10000us from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(40000, table[0][0]);
    }

    [TestMethod]
    public void WhenSbyteMultiplication_ShouldPromoteToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 100b * 2b from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(200, table[0][0]);
    }

    [TestMethod]
    public void WhenShortMultiplication_ShouldPromoteToInt()
    {
        var vm = CreateAndRunVirtualMachine("select 30000s * 2s from #schema.first()", SingleEntitySource);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(60000, table[0][0]);
    }

    #endregion

    #region Tautological condition warnings (MQ5010)

    [TestMethod]
    public void WhenWhereIsAlwaysTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded, "Query should compile successfully despite tautological warning.");
        Assert.IsTrue(
            result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition),
            $"Expected MQ5010 warning but found: [{string.Join(", ", result.Warnings.Select(w => w.Code))}]");
    }

    [TestMethod]
    public void WhenWhereIsFoldedToTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenWhereOrFoldsToTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where false or true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenHavingIsAlwaysTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name, Count(Name) from #schema.first() group by Name having true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenTautologicalWarningMessage_ShouldMentionWhereClause()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where true", SingleEntitySource);

        var warning = result.Warnings.First(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition);
        StringAssert.Contains(warning.Message, "WHERE");
    }

    #endregion

    #region Contradictory condition warnings (MQ5011)

    [TestMethod]
    public void WhenWhereIsAlwaysFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded, "Query should compile successfully despite contradictory warning.");
        Assert.IsTrue(
            result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            $"Expected MQ5011 warning but found: [{string.Join(", ", result.Warnings.Select(w => w.Code))}]");
    }

    [TestMethod]
    public void WhenWhereIsFoldedToFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenWhereOrOrFoldsToFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where false or false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenHavingIsAlwaysFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name, Count(Name) from #schema.first() group by Name having false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenContradictoryWarningMessage_ShouldMentionNoRows()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where false", SingleEntitySource);

        var warning = result.Warnings.First(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition);
        StringAssert.Contains(warning.Message, "no rows");
    }

    #endregion

    #region No false-positive warnings

    [TestMethod]
    public void WhenWhereHasColumnReference_ShouldNotEmitWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where Value > 0", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(
            result.Warnings.Any(w =>
                w.Code == DiagnosticCode.MQ5010_TautologicalCondition ||
                w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            "No tautological/contradictory warnings expected for column-based conditions.");
    }

    [TestMethod]
    public void WhenWhereMixesColumnAndConstant_ShouldNotEmitWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and Value > 0", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(
            result.Warnings.Any(w =>
                w.Code == DiagnosticCode.MQ5010_TautologicalCondition ||
                w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            "No tautological/contradictory warnings when column references remain.");
    }

    #endregion

    #region Diagnostic helper

    private BuildResult CompileWithDiagnostics<TEntity>(string script, TEntity[] source)
    {
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
            {
                {
                    "first",
                    (new GenericEntityTable<TEntity>(),
                        new GenericRowsSource<TEntity>(
                            source,
                            GenericEntityTable<TEntity>.NameToIndexMap,
                            GenericEntityTable<TEntity>.IndexToObjectAccessMap))
                }
            });

        var schemaProvider = new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#schema", schema }
        });

        return InstanceCreator.CompileWithDiagnostics(
            script,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
    }

    #endregion
}
