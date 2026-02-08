using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class RuntimeErrorTests : NegativeTestsBase
{
    #region 5.1 Data Conversion Runtime Errors

    [TestMethod]
    public void RE001_ToInt32OnNonNumericString_ReturnsNullInsteadOfThrowing()
    {
        
        var vm = CompileQuery("SELECT ToInt32('not_a_number') FROM #test.single()");
        var table = vm.Run(CancellationToken.None);
        Assert.IsNotNull(table, "ToInt32 on non-numeric string does not throw — returns null or default.");
    }

    [TestMethod]
    public void RE002_ToDecimalOnGarbageString_ReturnsNullInsteadOfThrowing()
    {
        
        var vm = CompileQuery("SELECT ToDecimal('abc') FROM #test.single()");
        var table = vm.Run(CancellationToken.None);
        Assert.IsNotNull(table, "ToDecimal on garbage string does not throw — returns null or default.");
    }

    [TestMethod]
    public void RE004_DivisionByZeroLiteral_ShouldThrowCompilationError()
    {
        
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT 10 / 0 FROM #test.single()"));
    }

    [TestMethod]
    public void RE006_ModuloByZeroLiteral_ShouldThrowCompilationError()
    {
        
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT 10 % 0 FROM #test.single()"));
    }

    #endregion

    #region 5.3 Invalid RLIKE Pattern

    [TestMethod]
    public void RE020_InvalidRegexPattern_ShouldThrowRuntimeError()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE Name RLIKE '[invalid('");
        Assert.Throws<Exception>(() => vm.Run(CancellationToken.None));
    }

    #endregion
}
