using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private const string ScriptVariableWhereSelectSampleFileName = "Q130_ScriptVariableWhereSelect.cs";
    private const string ScriptVariablePrimitiveValuesSampleFileName = "Q131_ScriptVariablePrimitiveValues.cs";
    private const string ScriptVariableSourceArgumentSampleFileName = "Q132_ScriptVariableSourceArgument.cs";
    private const string ScriptVariableGroupByHavingCaptureSampleFileName =
        "Q133_ScriptVariableGroupByHavingCapture.cs";
    private const string ScriptVariableJoinHelperCaptureSampleFileName =
        "Q134_ScriptVariableJoinHelperCapture.cs";
    private const string ScriptVariableCteHelperCaptureSampleFileName =
        "Q135_ScriptVariableCteHelperCapture.cs";
    private const string ScriptVariableWindowHelperCaptureSampleFileName =
        "Q136_ScriptVariableWindowHelperCapture.cs";
    private const string ScriptVariableParallelHelperCaptureSampleFileName =
        "Q137_ScriptVariableParallelHelperCapture.cs";

    [TestMethod]
    public void ScriptVariableSamples_WhenCheckedIn_ShouldUseCompileTimeLocals()
    {
        var samples = ReadNamedSamples(
                ScriptVariableWhereSelectSampleFileName,
                ScriptVariablePrimitiveValuesSampleFileName,
                ScriptVariableSourceArgumentSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var whereSelect = samples[ScriptVariableWhereSelectSampleFileName].Content;
        var primitiveValues = samples[ScriptVariablePrimitiveValuesSampleFileName].Content;
        var sourceArgument = samples[ScriptVariableSourceArgumentSampleFileName].Content;
        var sourceArgumentCode = ExtractGeneratedCodeSection(sourceArgument);

        Assert.AreEqual(0, CountOccurrences(whereSelect, "ScriptParameterBinder.Get"));
        Assert.Contains("const string letCountry = \"Poland\";", whereSelect);
        Assert.Contains("const int letMinPopulation = 100;", whereSelect);
        Assert.Contains("if (((ko3iko.Country == letCountry) && (population > letMinPopulation)))", whereSelect);
        Assert.Contains("yield return new ResultShape0(ko3iko.Name, population, letCountry);", whereSelect);
        Assert.Contains("yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population, __musoqShapeRow.RequestedCountry);", whereSelect);

        Assert.AreEqual(0, CountOccurrences(primitiveValues, "ScriptParameterBinder.Get"));
        Assert.Contains("const bool letFlag = true;", primitiveValues);
        Assert.Contains("const char letCode = 'x';", primitiveValues);
        Assert.Contains("int? letLimit = default(int?);", primitiveValues);
        Assert.Contains("Guid letId = new Guid(\"2ffcf6fa-3369-4300-946a-bb131a037985\");", primitiveValues);
        Assert.Contains("DateTime letCreated = new DateTime(638397614450000000L, DateTimeKind.Utc);", primitiveValues);
        Assert.Contains("TimeSpan letElapsed = new TimeSpan(54000000000L);", primitiveValues);
        Assert.Contains("new ResultRow0(letFlag, letCode, letLimit, letId, letCreated, letElapsed)", primitiveValues);

        Assert.AreEqual(0, CountOccurrences(sourceArgument, "ScriptParameterBinder.Get"));
        Assert.IsFalse(sourceArgumentCode.Contains("const string letPrefix = \"KEY\";", StringComparison.Ordinal));
        Assert.Contains("const string letKey = \"KEY_1\";", sourceArgumentCode);
        Assert.Contains("new object[] { letKey }", sourceArgumentCode);
        AssertTopLevelBindingBefore(
            ScriptVariableSourceArgumentSampleFileName,
            sourceArgument,
            "const string letKey = ",
            "var __ko3ikoSchema = ");
    }

    [TestMethod]
    public void ScriptVariableHelperCaptureSamples_WhenCheckedIn_ShouldPassCompileTimeLocalsToHelpers()
    {
        var samples = ReadNamedSamples(
                ScriptVariableGroupByHavingCaptureSampleFileName,
                ScriptVariableJoinHelperCaptureSampleFileName,
                ScriptVariableCteHelperCaptureSampleFileName,
                ScriptVariableWindowHelperCaptureSampleFileName,
                ScriptVariableParallelHelperCaptureSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var groupBy = samples[ScriptVariableGroupByHavingCaptureSampleFileName].Content;
        var join = samples[ScriptVariableJoinHelperCaptureSampleFileName].Content;
        var cte = samples[ScriptVariableCteHelperCaptureSampleFileName].Content;
        var window = samples[ScriptVariableWindowHelperCaptureSampleFileName].Content;
        var parallel = samples[ScriptVariableParallelHelperCaptureSampleFileName].Content;

        AssertScriptVariableBindingCount(ScriptVariableGroupByHavingCaptureSampleFileName, groupBy, 2);
        Assert.Contains("ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token, letSuffix);", groupBy);
        Assert.DoesNotContain("SerialSingleKeyAggregate", groupBy);
        Assert.Contains("CancellationToken cancellationToken, string letSuffix)", groupBy);
        Assert.Contains("private readonly string _letSuffix;", groupBy);
        Assert.Contains("string groupKey = (ko3iko.Country + letSuffix);", groupBy);
        Assert.Contains("if ((finalGroup.__agg0.Count >= letMinCount))", groupBy);
        AssertTopLevelBindingBefore(
            ScriptVariableGroupByHavingCaptureSampleFileName,
            groupBy,
            "const string letSuffix = ",
            "ParallelSingleKeyAggregate_0(");

        AssertScriptVariableBindingCount(ScriptVariableJoinHelperCaptureSampleFileName, join, 2);
        Assert.Contains("_cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, letSuffix);", join);
        Assert.Contains("CteRowResults _cteRowResults, string letSuffix)", join);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", join);
        Assert.Contains("Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];", join);
        Assert.Contains("string key = (b.City + letSuffix);", join);
        Assert.Contains("__resultLibraryBase0.Coalesce<string>((ab.b_Name + letSuffix), letFallback)", join);
        AssertTopLevelBindingBefore(
            ScriptVariableJoinHelperCaptureSampleFileName,
            join,
            "const string letSuffix = ",
            "_cteRowResults.Slot0 = BuildCte0(");

        AssertScriptVariableBindingCount(ScriptVariableCteHelperCaptureSampleFileName, cte, 1);
        Assert.Contains("_cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults, letCountry);", cte);
        Assert.Contains("CteRowResults _cteRowResults, CteIndexResults _cteIndexResults, string letCountry)", cte);
        Assert.Contains("if ((ko3iko.Country == letCountry))", cte);
        AssertTopLevelBindingBefore(
            ScriptVariableCteHelperCaptureSampleFileName,
            cte,
            "const string letCountry = ",
            "_cteRowResults.Slot0 = BuildCte0(");

        AssertScriptVariableBindingCount(ScriptVariableWindowHelperCaptureSampleFileName, window, 2);
        Assert.Contains("ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersPartitionKeys, resultRowNumbersOrderKeys, letCountry, letLabel);", window);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name, (long)resultRowNumbers[windowIndex], letLabel));", window);
        Assert.IsFalse(window.Contains("AppendResultWindowRows(resultWindowRows, result, resultRowNumbers, letLabel);", StringComparison.Ordinal));
        Assert.Contains("WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys, string letCountry, string letLabel)", window);
        Assert.Contains("resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey((ko3iko.Name + letLabel));", window);
        AssertTopLevelBindingBefore(
            ScriptVariableWindowHelperCaptureSampleFileName,
            window,
            "const string letCountry = ",
            "ExtractResultRowNumbersWindowKeys(");

        AssertScriptVariableBindingCount(ScriptVariableParallelHelperCaptureSampleFileName, parallel, 2);
        Assert.Contains(ParallelFilterProjectLoopPattern, parallel);
        Assert.Contains("EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>", parallel);
        Assert.Contains("QueryRows.FromRowShards(", parallel);
        Assert.Contains("(ko3iko) => (ko3iko.Value > letThreshold)", parallel);
        Assert.Contains("new ResultRow0(ko3iko.Name, letLabel, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value))", parallel);
        AssertTopLevelBindingBefore(
            ScriptVariableParallelHelperCaptureSampleFileName,
            parallel,
            "const int letThreshold = ",
            "var __musoqTableParallelRows = ");

        foreach (var sample in new[] { groupBy, join, cte, window, parallel })
            Assert.IsFalse(sample.Contains("ScriptParameterBinder.Get", StringComparison.Ordinal));
    }

    private static void AssertScriptVariableBindingCount(string fileName, string content, int expectedCount)
    {
        Assert.AreEqual(
            expectedCount,
            CountOccurrences(content, "const string let") + CountOccurrences(content, "const int let"),
            $"{fileName}: script variables should be emitted once as top-level generated locals.");
    }
}
