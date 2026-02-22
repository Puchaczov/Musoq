using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class TypeErrorTests : NegativeTestsBase
{
    #region 3.1 Comparison Type Mismatches

    [TestMethod]
    public void TE001_ComparingStringToInteger_DoesNotThrow()
    {
        var vm = CompileQuery("SELECT * FROM #test.people() WHERE Name = 42");
        Assert.IsNotNull(vm, "String-to-int comparison compiles without error in Musoq.");
    }

    [TestMethod]
    public void TE003_ComparingIntToBool_ShouldThrowCompilationError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT * FROM #test.types() WHERE IntCol = true"));
    }

    [TestMethod]
    public void TE004_ComparingDecimalToGuid_ShouldThrowCompilationError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT * FROM #test.types() WHERE DecimalCol = GuidCol"));
    }

    #endregion

    #region 3.2 Arithmetic Type Mismatches

    [TestMethod]
    public void TE010_AddingStringToInt_ShouldThrowInvalidOperandTypesException()
    {
        Assert.Throws<InvalidOperandTypesException>(() =>
            CompileQuery("SELECT Name + Age FROM #test.people()"));
    }

    [TestMethod]
    public void TE011_MultiplyingStringByInt_ShouldThrowInvalidOperandTypesException()
    {
        Assert.Throws<InvalidOperandTypesException>(() =>
            CompileQuery("SELECT Name * 3 FROM #test.people()"));
    }

    [TestMethod]
    public void TE012_DividingByString_ShouldThrowInvalidOperandTypesException()
    {
        Assert.Throws<InvalidOperandTypesException>(() =>
            CompileQuery("SELECT Age / 'two' FROM #test.people()"));
    }

    [TestMethod]
    public void TE013_ModuloWithString_ShouldThrowInvalidOperandTypesException()
    {
        Assert.Throws<InvalidOperandTypesException>(() =>
            CompileQuery("SELECT Age % 'three' FROM #test.people()"));
    }

    [TestMethod]
    public void TE014_ArithmeticOnDateTimeDirectly_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT BirthDate + BirthDate FROM #test.people()"));
    }

    [TestMethod]
    public void TE015_ArithmeticOnBool_ShouldThrowCompilationError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT BoolCol + BoolCol FROM #test.types()"));
    }

    [TestMethod]
    public void TE016_ArithmeticOnGuid_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT GuidCol + 1 FROM #test.types()"));
    }

    #endregion

    #region 3.3 Logical Operator Type Mismatches

    [TestMethod]
    public void TE020_AndWithNonBooleanOperands_ShouldThrowCompilationError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name AND Age"));
    }

    [TestMethod]
    public void TE021_OrWithNonBooleanOperands_ShouldThrowCompilationError()
    {
        Assert.Throws<CompilationException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE 'hello' OR 42"));
    }

    [TestMethod]
    public void TE022_NotOnNonBoolean_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE NOT Name"));
    }

    #endregion

    #region 3.4 Function Argument Type Errors

    [TestMethod]
    public void TE031_SubstringWithNonIntegerStart_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT Substring(Name, 'zero', 5) FROM #test.people()"));
    }

    [TestMethod]
    public void TE033_WrongNumberOfArgumentsToFunction_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT Substring(Name) FROM #test.people()"));
    }

    [TestMethod]
    public void TE034_TooManyArgumentsToFunction_DoesNotThrow()
    {
        var vm = CompileQuery("SELECT ToUpper(Name, 'extra') FROM #test.people()");
        Assert.IsNotNull(vm, "ToUpper with extra args compiles without error.");
    }

    [TestMethod]
    public void TE035_AggregateFunctionOnIncompatibleType_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT Sum(Name) FROM #test.people()"));
    }

    [TestMethod]
    public void TE036_AvgOnString_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT Avg(City) FROM #test.people()"));
    }

    [TestMethod]
    public void TE038_NonexistentFunction_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT CompletelyFakeFunction(Name) FROM #test.people()"));
    }

    #endregion

    #region 3.5 Set Operation Type Mismatches

    [TestMethod]
    public void TE040_UnionWithMismatchedColumnTypes_ShouldThrowTypeError()
    {
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION ALL (Name) SELECT Age AS Name FROM #test.people()"));
    }

    [TestMethod]
    public void TE041_ExceptWithMismatchedTypes_ShouldThrowTypeError()
    {
        Assert.Throws<SetOperatorMustHaveSameTypesOfColumnsException>(() =>
            CompileQuery("SELECT Age FROM #test.people() EXCEPT (Age) SELECT Name AS Age FROM #test.people()"));
    }

    [TestMethod]
    public void TE042_UnionWithWrongNumberOfColumnsInList_DoesNotThrow()
    {
        var vm = CompileQuery(
            "SELECT Name, Age FROM #test.people() UNION ALL (Name) SELECT Name, Age FROM #test.people()");
        Assert.IsNotNull(vm, "UNION ALL with fewer key columns than selected columns compiles without error.");
    }

    [TestMethod]
    public void TE043_UnionColumnListReferencesNonexistentColumn_DoesNotThrow()
    {
        var vm = CompileQuery("SELECT Name FROM #test.people() UNION ALL (FakeColumn) SELECT Name FROM #test.people()");
        Assert.IsNotNull(vm, "UNION ALL with fake column in key list compiles without error.");
    }

    #endregion

    #region 3.7 Pattern Operator Type Mismatches

    [TestMethod]
    public void TE060_LikeOnNonStringColumn_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age LIKE '%5%'"));
    }

    [TestMethod]
    public void TE061_RlikeOnNonStringColumn_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age RLIKE '\\d+'"));
    }

    [TestMethod]
    public void TE062_ContainsOnNonStringColumn_ShouldThrowError()
    {
        Assert.Throws<Exception>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age CONTAINS '5'"));
    }

    #endregion
}
