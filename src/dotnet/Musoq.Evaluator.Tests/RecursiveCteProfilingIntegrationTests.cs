using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RecursiveCteProfilingIntegrationTests
{
    [TestMethod]
    public void RunWithProfile_WhenUnionAllRecurses_ShouldReportAggregateFrontierRows()
    {
        var profile = RunProfiled("Q188_RecursiveUnionAllCounter", expectedResultRows: 4);
        var recursive = profile.Operators.Single(operation => operation.Name == "RecursiveCte");
        var appends = profile.Operators.Where(operation => operation.Name == "RecursiveAppend").ToArray();

        Assert.AreEqual(4, recursive.InputRows);
        Assert.AreEqual(3, recursive.OutputRows);
        Assert.HasCount(2, appends);
        Assert.AreEqual((1L, 1L), (appends[0].InputRows, appends[0].OutputRows));
        Assert.AreEqual((3L, 3L), (appends[1].InputRows, appends[1].OutputRows));
    }

    [TestMethod]
    public void RunWithProfile_WhenKeyedUnionRejectsCycle_ShouldSeparateCandidatesFromAcceptedRows()
    {
        var profile = RunProfiled("Q193_RecursiveUnionSingleKeyCycle", expectedResultRows: 2);
        var recursive = profile.Operators.Single(operation => operation.Name == "RecursiveCte");
        var appends = profile.Operators.Where(operation => operation.Name == "RecursiveAppend").ToArray();

        Assert.AreEqual(2, recursive.InputRows);
        Assert.AreEqual(1, recursive.OutputRows);
        Assert.HasCount(2, appends);
        Assert.AreEqual((1L, 1L), (appends[0].InputRows, appends[0].OutputRows));
        Assert.AreEqual((2L, 1L), (appends[1].InputRows, appends[1].OutputRows));
    }

    private static Musoq.Evaluator.Diagnostics.QueryProfileSnapshot RunProfiled(
        string sampleName,
        int expectedResultRows)
    {
        var testCase = RecursiveCteSupportedCaseCatalog.GetBySampleName(sampleName);
        var schemaProvider = testCase.CreateSchemaProvider?.Invoke() ??
                             GeneratedCodeSamplesCatalog.CreateBasicSchemaProvider();
        var query = InstanceCreator.CompileForExecution(
            testCase.Query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            new TestsLoggerResolver(),
            testCase.CompilationOptions.WithInstrumentationMode(QueryInstrumentationMode.Full));

        var result = query.RunWithProfile(CancellationToken.None);

        Assert.AreEqual(expectedResultRows, result.Result.Count);
        return result.Profile;
    }
}
