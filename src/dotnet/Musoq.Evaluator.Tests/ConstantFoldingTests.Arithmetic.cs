using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

public partial class ConstantFoldingTests
{
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
}
