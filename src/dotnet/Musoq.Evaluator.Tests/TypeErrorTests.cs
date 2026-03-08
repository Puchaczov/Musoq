using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.types() WHERE IntCol = true"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "cannot compare");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE004_ComparingDecimalToGuid_ShouldThrowCompilationError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.types() WHERE DecimalCol = GuidCol"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "cannot compare");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 3.2 Arithmetic Type Mismatches

    [TestMethod]
    public void TE010_AddingStringToInt_ShouldThrowInvalidOperandTypesException()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name + Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "String");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE011_MultiplyingStringByInt_ShouldThrowInvalidOperandTypesException()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name * 3 FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "String");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE012_DividingByString_ShouldThrowInvalidOperandTypesException()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Age / 'two' FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "String");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE013_ModuloWithString_ShouldThrowInvalidOperandTypesException()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Age % 'three' FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "String");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE014_ArithmeticOnDateTimeDirectly_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT BirthDate + BirthDate FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "DateTime");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE015_ArithmeticOnBool_ShouldThrowCompilationError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT BoolCol + BoolCol FROM #test.types()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "Boolean");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE016_ArithmeticOnGuid_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT GuidCol + 1 FROM #test.types()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "Guid");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 3.3 Logical Operator Type Mismatches

    [TestMethod]
    public void TE020_AndWithNonBooleanOperands_ShouldThrowCompilationError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Name AND Age"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "boolean operands");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE021_OrWithNonBooleanOperands_ShouldThrowCompilationError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE 'hello' OR 42"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "boolean operands");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE022_NotOnNonBoolean_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE NOT Name"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    #endregion

    #region 3.4 Function Argument Type Errors

    [TestMethod]
    public void TE031_SubstringWithNonIntegerStart_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Substring(Name, 'zero', 5) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "Substring");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE033_WrongNumberOfArgumentsToFunction_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Substring(Name) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "Substring");
        AssertHasGuidance(ex);
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
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Sum(Name) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "SetSum");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE036_AvgOnString_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Avg(City) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "SetAvg");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE038_NonexistentFunction_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT CompletelyFakeFunction(Name) FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3029_UnresolvableMethod, DiagnosticPhase.Bind, "CompletelyFakeFunction");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 3.5 Set Operation Type Mismatches

    [TestMethod]
    public void TE040_UnionWithMismatchedColumnTypes_ShouldThrowTypeError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION ALL (Name) SELECT Age AS Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE041_ExceptWithMismatchedTypes_ShouldThrowTypeError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Age FROM #test.people() EXCEPT (Age) SELECT Name AS Age FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE042_UnionWithWrongNumberOfColumnsInList_DoesNotThrow()
    {
        var vm = CompileQuery(
            "SELECT Name, Age FROM #test.people() UNION ALL (Name) SELECT Name, Age FROM #test.people()");
        Assert.IsNotNull(vm, "UNION ALL with fewer key columns than selected columns compiles without error.");
    }

    [TestMethod]
    public void TE043_UnionColumnListReferencesNonexistentColumn_ShouldThrowUnknownColumnError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT Name FROM #test.people() UNION ALL (FakeColumn) SELECT Name FROM #test.people()"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "FakeColumn");
        AssertHasGuidance(ex);
    }

    #endregion

    #region 3.7 Pattern Operator Type Mismatches

    [TestMethod]
    public void TE060_LikeOnNonStringColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age LIKE '%5%'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "requires string operands");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE061_RlikeOnNonStringColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age RLIKE '\\d+'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "requires string operands");
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void TE062_ContainsOnNonStringColumn_ShouldThrowError()
    {
        var ex = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("SELECT * FROM #test.people() WHERE Age CONTAINS '5'"));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ2001_UnexpectedToken, DiagnosticPhase.Parse);
        AssertHasGuidance(ex);
    }

    #endregion
}
