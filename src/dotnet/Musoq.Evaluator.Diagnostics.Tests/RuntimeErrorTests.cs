using System.Threading;
using Musoq.Converter.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class RuntimeErrorTests : NegativeTestsBase
{
    #region 5.3 Invalid RLIKE Pattern

    [TestMethod]
    public void RE020_InvalidRegexPattern_ShouldFailDuringBinding()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name RLIKE '[invalid('"));

        AssertSingleError(ex, DiagnosticCode.MQ3094_InvalidConstantRegex, DiagnosticPhase.Bind);
    }

    #endregion

    #region 5.1 Data Conversion Runtime Errors

    [TestMethod]
    public void RE001_ToInt32OnNonNumericString_ReturnsNullInsteadOfThrowing()
    {
        using var vm = CompileQuery("SELECT ToInt32('not_a_number') FROM #test.single()");
        using var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0].ReadValue(0));
    }

    [TestMethod]
    public void RE002_ToDecimalOnGarbageString_ReturnsNullInsteadOfThrowing()
    {
        using var vm = CompileQuery("SELECT ToDecimal('abc') FROM #test.single()");
        using var table = vm.Run(CancellationToken.None);
        Assert.AreEqual(1, table.Count);
        Assert.IsNull(table[0].ReadValue(0));
    }

    [TestMethod]
    public void RE004_DivisionByZeroLiteral_ShouldThrowCompilationError()
    {
        // Constant folding detects division by zero at compile time
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 10 / 0 FROM #test.single()"));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    [TestMethod]
    public void RE006_ModuloByZeroLiteral_ShouldThrowCompilationError()
    {
        // Constant folding detects modulo by zero at compile time
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT 10 % 0 FROM #test.single()"));

        AssertSingleError(ex, DiagnosticCode.MQ3008_DivisionByZero, DiagnosticPhase.Bind);
    }

    #endregion
}
