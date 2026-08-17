using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.NegativeTests;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ScriptParameterTypeCompatibilityTests : NegativeTestsBase
{
    [TestMethod]
    public void CompileForExecution_WhenIntColumnIsComparedToStringParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(age: string) select Name from #test.people() where Age = $age"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$age");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenDateTimeColumnIsComparedToStringParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(since: string) select OrderId from #test.orders() where OrderDate > $since"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$since");
        AssertMessageContains(exception, "DateTime");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenStringParameterIsComparedToNumericLiteral_ShouldRejectInsteadOfImplicitlyConverting()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(limit: string) select Name from #test.people() where $limit > 10"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$limit");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenParameterTypesAreCompatible_ShouldCompile()
    {
        var query = CompileQuery("param(age: int, since: datetime) select Name from #test.people() where Age = $age and BirthDate >= $since");

        Assert.IsNotNull(query);
    }

    [TestMethod]
    public void CompileForExecution_WhenParameterUsesExplicitConversion_ShouldCompile()
    {
        var query = CompileQuery("param(ageText: string) select Name from #test.people() where Age = ToInt32($ageText)");

        Assert.IsNotNull(query);
    }

    [TestMethod]
    public void CompileForInspection_WhenParameterComparisonIsGenerated_ShouldBindOnceAndNotWrapParameterInImplicitConversion()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "param(age: int) select Name from #test.people() where Age >= $age",
            Guid.NewGuid().ToString(),
            CreateSchemaProvider(),
            LoggerResolver,
            TestCompilationOptions);

        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "ScriptParameterBinder.GetRequired<int>(__musoqExecutionState.Parameters, \"age\")"));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "paramAge");
        Assert.DoesNotContain("TryConvertToInt32", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("TryConvertToInt64", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("TryConvertToDecimal", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenInListContainsIncompatibleParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(ageText: string) select Name from #test.people() where Age in ($ageText, 42)"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$ageText");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenInParameterHasIncompatibleElementType_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(ids: string[]) select Name from #test.people() where Age in $ids"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$ids");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenInParameterIsScalar_ShouldRequireCollectionParameter()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(id: int) select Name from #test.people() where Age in $id"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3007_InvalidOperandTypes, DiagnosticPhase.Bind, "$id");
        AssertMessageContains(exception, "one-dimensional array script parameter");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenContainsListContainsIncompatibleParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(age: int) select Name from #test.people() where Name contains ($age)"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$age");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenBetweenBoundContainsIncompatibleParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(low: string) select Name from #test.people() where Age between $low and 40"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$low");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenCollectionAndRangeParameterTypesAreCompatible_ShouldCompile()
    {
        var query = CompileQuery(
            "param(age: int, minAge: int, maxAge: long, name: string) " +
            "select Name from #test.people() where Age in ($age, 42) and Age between $minAge and $maxAge and Name contains ($name)");

        Assert.IsNotNull(query);
    }

    [TestMethod]
    public void CompileForExecution_WhenFunctionReceivesIncompatibleParameter_ShouldExplainDeclaredParameterType()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(limit: int) select Length($limit) from #test.people()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3088_NoMatchingCallableOverload, DiagnosticPhase.Bind, "$limit");
        AssertMessageContains(exception, "Int32");
        AssertMessageContains(exception, "Length");
        AssertMessageContains(exception, "Expected overloads");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenFunctionReceivesBooleanParameterForStringFunction_ShouldExplainDeclaredParameterType()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(flag: bool) select ToUpper($flag) from #test.people()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3088_NoMatchingCallableOverload, DiagnosticPhase.Bind, "$flag");
        AssertMessageContains(exception, "Boolean");
        AssertMessageContains(exception, "ToUpper");
        AssertMessageContains(exception, "Expected overloads");
        AssertMessageContains(exception, "explicit conversion");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenFunctionParameterUsesExplicitConversion_ShouldCompile()
    {
        var query = CompileQuery(
            "param(limit: int, flag: bool) select Length(ToString($limit)), ToUpper(ToString($flag)) from #test.people()");

        Assert.IsNotNull(query);
    }

    [TestMethod]
    public void CompileForExecution_WhenWhereUsesNonBooleanParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(flag: string) select Name from #test.people() where $flag"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$flag");
        AssertMessageContains(exception, "WHERE clause requires a boolean expression");
        AssertMessageContains(exception, "String");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenHavingUsesNonBooleanParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(flag: string) select City, Count(Name) from #test.people() group by City having $flag"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$flag");
        AssertMessageContains(exception, "HAVING clause requires a boolean expression");
        AssertMessageContains(exception, "String");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenQualifyUsesNonBooleanParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery(
                "param(flag: string) select Name, RowNumber() over(order by Name) as rn from #test.people() " +
                "qualify case when RowNumber() over(order by Name) = 1 then $flag else $flag end"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$flag");
        AssertMessageContains(exception, "QUALIFY clause requires a boolean expression");
        AssertMessageContains(exception, "String");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenCaseWhenUsesNonBooleanParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(label: string) select case when $label then 'yes' else 'no' end from #test.people()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$label");
        AssertMessageContains(exception, "CASE WHEN requires a boolean expression");
        AssertMessageContains(exception, "String");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenJoinPredicateComparesColumnToIncompatibleParameter_ShouldUseParameterAwareTypeMismatch()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(personId: string) select p.Name from #test.people() p inner join #test.orders() o on o.PersonId = $personId"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3005_TypeMismatch, DiagnosticPhase.Bind, "$personId");
        AssertMessageContains(exception, "String");
        AssertMessageContains(exception, "Int32");
        AssertHasGuidance(exception);
    }

    [TestMethod]
    public void CompileForExecution_WhenSetOperationComparesParameterProjectionToDifferentColumnType_ShouldKeepSetOperatorDiagnostic()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("param(label: string) select $label as V from #test.people() union all (V) select Age as V from #test.people()"));

        AssertErrorEnvelope(exception, DiagnosticCode.MQ3020_SetOperatorColumnTypes, DiagnosticPhase.Bind, "same types");
        AssertHasGuidance(exception);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
