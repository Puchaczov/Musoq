using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ParallelFilterProjectionSamples_WhenCheckedIn_ShouldUseParallelOnlyForMethodHeavyPaths()
    {
        var samples = ReadNamedSamples(
                "Q01_SimpleSelectWhere.cs",
                LargeInClauseSampleFileName,
                CompilationSimpleSelectSampleFileName,
                RuntimeV2CseNoDuplicateRegressionSampleFileName,
                RuntimeV2StringFilterSampleFileName,
                RuntimeV2ParallelFilterProjectSampleFileName)
            .ToDictionary(static sample => sample.FileName);
        var simpleSelect = samples["Q01_SimpleSelectWhere.cs"].Content;
        var largeInClause = samples[LargeInClauseSampleFileName].Content;
        var compilationSimpleSelect = samples[CompilationSimpleSelectSampleFileName].Content;
        var cseNoDuplicate = samples[RuntimeV2CseNoDuplicateRegressionSampleFileName].Content;
        var stringFilter = samples[RuntimeV2StringFilterSampleFileName].Content;
        var heavyProjection = samples[RuntimeV2ParallelFilterProjectSampleFileName].Content;

        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", simpleSelect);
        Assert.IsFalse(simpleSelect.Contains(ParallelFilterProjectLoopPattern, StringComparison.Ordinal));
        Assert.IsFalse(simpleSelect.Contains(ParallelProjectionRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(simpleSelect.Contains(ParallelProjectRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(simpleSelect.Contains(TableParallelProjectRowsPattern, StringComparison.Ordinal));

        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", largeInClause);
        Assert.IsFalse(largeInClause.Contains(ParallelFilterProjectLoopPattern, StringComparison.Ordinal));
        Assert.IsFalse(largeInClause.Contains(ParallelProjectionRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(largeInClause.Contains(ParallelProjectRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(largeInClause.Contains(TableParallelProjectRowsPattern, StringComparison.Ordinal));

        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", compilationSimpleSelect);
        Assert.IsFalse(compilationSimpleSelect.Contains(ParallelFilterProjectLoopPattern, StringComparison.Ordinal));
        Assert.IsFalse(compilationSimpleSelect.Contains(ParallelProjectionRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(compilationSimpleSelect.Contains(ParallelProjectRowsPattern, StringComparison.Ordinal));
        Assert.IsFalse(compilationSimpleSelect.Contains(TableParallelProjectRowsPattern, StringComparison.Ordinal));

        Assert.Contains(ParallelFilterProjectLoopPattern, cseNoDuplicate);
        Assert.Contains(ParallelProjectionRowsPattern, cseNoDuplicate);
        Assert.Contains(TableParallelProjectRowsPattern, cseNoDuplicate);
        Assert.Contains(AddRowsDirectPattern, cseNoDuplicate);
        Assert.DoesNotContain("SequentialKernel", cseNoDuplicate);
        Assert.DoesNotContain("TableProjectionRows.ProjectRowsSerial", cseNoDuplicate);

        Assert.Contains(ParallelFilterProjectLoopPattern, stringFilter);
        Assert.Contains(ParallelProjectionRowsPattern, stringFilter);
        Assert.Contains(TableParallelProjectRowsPattern, stringFilter);
        Assert.Contains(AddRowsDirectPattern, stringFilter);
        Assert.Contains("new ResultRow0(ko3iko.FirstName, ko3iko.LastName, ko3iko.Email)", stringFilter);
        Assert.DoesNotContain("SequentialKernel", stringFilter);
        Assert.DoesNotContain("TableProjectionRows.ProjectRowsSerial", stringFilter);

        Assert.Contains(ParallelFilterProjectLoopPattern, heavyProjection);
        Assert.Contains(ParallelProjectionRowsPattern, heavyProjection);
        Assert.Contains(TableParallelProjectRowsPattern, heavyProjection);
        Assert.Contains(AddRowsDirectPattern, heavyProjection);
        Assert.DoesNotContain("SequentialKernel", heavyProjection);
        Assert.DoesNotContain("TableProjectionRows.ProjectRowsSerial", heavyProjection);
    }
}
