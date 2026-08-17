using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Converter.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Converter.Tests.Schema;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenInstrumentationIsDisabled_ShouldNotEmitProfilingSymbols()
    {
        var result = Inspect("select d.Dummy from #system.dual() d");

        AssertGeneratedCSharpDoesNotContain("QueryProfileRecorder", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("ProfiledEnumerable", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("ProfiledChunkedEnumerable", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("profileRecorder", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("SourceDiagnostics", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("IProfiledRunnable", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenSourceBoundaryInstrumentationIsEnabled_ShouldWrapSourceRows()
    {
        var result = Inspect(
            "select d.Dummy from #system.dual() d",
            CreateSourceBoundaryProfileOptions());

        AssertGeneratedCSharpContains("IProfiledRunnable", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("QueryProfileRecorder", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("CreateAdaptiveSourceRecorder(\"d\")", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("ProfiledChunkedEnumerable<", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("SourceDiagnostics.None", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("BeginOperator", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("AddOperatorInputRows", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenFullInstrumentationIsEnabled_ShouldEmitOperatorProfilingCalls()
    {
        var result = Inspect(
            "select d.Dummy from #system.dual() d",
            new CompilationOptions(instrumentationMode: QueryInstrumentationMode.Full));

        AssertGeneratedCSharpContains("BeginOperatorValue", result.GeneratedCSharpCode);
        AssertGeneratedCSharpContains("AddOutputRows", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenFullInstrumentationIsEnabled_ShouldKeepNormalRunUnprofiled()
    {
        var result = Inspect(
            "select d.Dummy from #system.dual() d",
            new CompilationOptions(instrumentationMode: QueryInstrumentationMode.Full));
        var methods = CSharpSyntaxTree.ParseText(result.GeneratedCSharpCode)
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .ToArray();
        var normalShapeMethod = methods.Single(method =>
            method.Identifier.ValueText.StartsWith("ComputeShapeRows_", StringComparison.Ordinal) &&
            !method.Identifier.ValueText.EndsWith("_Profiled", StringComparison.Ordinal));
        var profiledShapeMethod = methods.Single(method =>
            method.Identifier.ValueText.StartsWith("ComputeShapeRows_", StringComparison.Ordinal) &&
            method.Identifier.ValueText.EndsWith("_Profiled", StringComparison.Ordinal));

        Assert.IsFalse(normalShapeMethod.ToFullString().Contains("profileRecorder", StringComparison.Ordinal));
        Assert.IsFalse(normalShapeMethod.ToFullString().Contains("QueryProfileRecorder", StringComparison.Ordinal));
        StringAssert.Contains(profiledShapeMethod.ToFullString(), "profileRecorder");
        StringAssert.Contains(profiledShapeMethod.ToFullString(), "BeginOperatorValue");

        foreach (var runMethod in methods.Where(method => method.Identifier.ValueText == "Run"))
        {
            Assert.IsFalse(runMethod.ToFullString().Contains("_Profiled", StringComparison.Ordinal));
            Assert.IsFalse(runMethod.ToFullString().Contains("profileRecorder", StringComparison.Ordinal));
        }

        var runWithProfileMethod = methods.Single(method => method.Identifier.ValueText == "RunWithProfile");
        StringAssert.Contains(runWithProfileMethod.ToFullString(), "_Profiled");
    }

    [TestMethod]
    public void CompileForExecution_WhenFullInstrumentationIsEnabled_ShouldStillRunWithoutProfileRecorder()
    {
        var compiled = CompileForExecution(
            "select d.Dummy from #system.dual() d",
            new CompilationOptions(instrumentationMode: QueryInstrumentationMode.Full));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("single", table[0][0]);
    }

    [TestMethod]
    public void RunWithProfile_WhenCompiledForProfile_ShouldReturnSameRowsAndRecordSourceBoundaryStats()
    {
        const string query = "select d.Dummy from #system.dual() d where d.Dummy = 'single'";
        var normal = CompileForExecution(query);
        var profiled = InstanceCreator.CompileForProfile(
            query,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        var normalTable = normal.Run();
        var profileResult = profiled.RunWithProfile();
        var source = profileResult.Profile.Sources.Single();

        AssertTablesEqual(normalTable, profileResult.Result);
        Assert.AreEqual("d", source.Name);
        Assert.AreEqual(1, source.RowsRead);
        Assert.IsTrue(source.FirstRowLatency.HasValue);
        Assert.IsTrue(source.LastRowTime.HasValue);
        Assert.IsTrue(source.MoveNextWaitTime >= TimeSpan.Zero);
        Assert.IsTrue(source.ConsumerGapTime >= TimeSpan.Zero);
        Assert.AreNotEqual(SourceProfileDiagnosis.Unknown, source.Diagnosis);

        StringAssert.Contains(profileResult.ProfileText, "Rows read: 1");
        StringAssert.Contains(profileResult.ProfileText, "MoveNext wait:");
        StringAssert.Contains(profileResult.ProfileText, "Consumer gap:");
        StringAssert.Contains(profileResult.ProfileText, "Diagnosis:");
    }

    [TestMethod]
    public void InstanceCreatorProfile_ShouldWorkForCommonQueryShapes()
    {
        var cases = new (string Name, string Query, CompilationOptions? Options)[]
        {
            ("select", "select d.Dummy from #system.dual() d", null),
            ("aggregate", "select Count(d.Dummy) as Count from #system.dual() d", null),
            ("join", "select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy", null),
            ("cte", "with x as (select d.Dummy from #system.dual() d) select x.Dummy from x", null),
            ("parallel", "with a as (select d.Dummy from #system.dual() d), b as (select e.Dummy from #system.dual() e) select a.Dummy from a inner join b on a.Dummy = b.Dummy", new CompilationOptions(useCteParallelization: true))
        };

        foreach (var testCase in cases)
        {
            var result = InstanceCreator.Profile(
                testCase.Query,
                Guid.NewGuid().ToString(),
                _schemaProvider,
                _loggerResolver,
                testCase.Options);

            Assert.IsGreaterThanOrEqualTo(1, result.Result.Count, testCase.Name);
            Assert.IsGreaterThanOrEqualTo(1, result.Profile.Sources.Count, testCase.Name);
            StringAssert.Contains(result.ProfileText, "Musoq query profile");
            StringAssert.Contains(result.ProfileText, "Sources:");
            StringAssert.Contains(result.ProfileText, "Operators:");
        }
    }

    [TestMethod]
    public void CompileForProfile_WhenParameterized_ShouldAllowParametersBeforeProfiling()
    {
        var compiled = InstanceCreator.CompileForProfile(
            "param(expected: string) select d.Dummy from #system.dual() d where d.Dummy = $expected",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        compiled.Parameters["expected"] = "single";

        var result = compiled.RunWithProfile();

        Assert.AreEqual(1, result.Result.Count);
        Assert.AreEqual("single", result.Result[0][0]);
        Assert.AreEqual(1, result.Profile.Sources.Single().RowsRead);
    }

    [TestMethod]
    public void RunWithProfile_ShouldPreservePhaseAndDataSourceProgressEvents()
    {
        var compiled = InstanceCreator.CompileForProfile(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            new ReportingSystemSchemaProvider(),
            _loggerResolver);
        var queryPhases = new List<QueryPhase>();
        var dataSourcePhases = new List<DataSourcePhase>();

        compiled.PhaseChanged += (_, args) => queryPhases.Add(args.Phase);
        compiled.DataSourceProgress += (_, args) => dataSourcePhases.Add(args.Phase);

        var result = compiled.RunWithProfile();

        Assert.AreEqual(1, result.Result.Count);
        CollectionAssert.Contains(queryPhases, QueryPhase.Begin);
        CollectionAssert.Contains(queryPhases, QueryPhase.From);
        CollectionAssert.Contains(queryPhases, QueryPhase.Select);
        CollectionAssert.Contains(queryPhases, QueryPhase.End);
        CollectionAssert.Contains(dataSourcePhases, DataSourcePhase.Begin);
        CollectionAssert.Contains(dataSourcePhases, DataSourcePhase.RowsKnown);
        CollectionAssert.Contains(dataSourcePhases, DataSourcePhase.RowsRead);
        CollectionAssert.Contains(dataSourcePhases, DataSourcePhase.End);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldEmitStableOperatorIdsActualRowsAndTimings()
    {
        var result = InstanceCreator.ExplainAnalyze(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.AreEqual(1, result.Result.Count);
        Assert.IsGreaterThanOrEqualTo(1, result.Profile.Operators.Count);
        Assert.AreEqual("op1", result.Profile.Operators[0].Id);
        StringAssert.Contains(result.ExecutionPlanText, "[op1] ExecutionPlan");
        StringAssert.Contains(result.ExplainAnalyzeText, "actual rows=");
        StringAssert.Contains(result.ExplainAnalyzeText, "elapsed=");
        Assert.IsTrue(result.Profile.Operators.Any(static operation => operation.HasActualStats));
    }

    [TestMethod]
    public void DiagnosticSqlCommandParser_ShouldClassifyProfileExplainAnalyzeAndNormalScripts()
    {
        Assert.IsTrue(DiagnosticSqlCommandParser.TryParse(
            "profile select d.Dummy from #system.dual() d",
            out var profileCommand,
            out var profileDiagnostics));
        Assert.IsNotNull(profileCommand);
        Assert.IsNull(profileDiagnostics);
        Assert.AreEqual(DiagnosticSqlCommandKind.Profile, profileCommand.Kind);
        Assert.AreEqual("select d.Dummy from #system.dual() d", profileCommand.InnerScript);

        Assert.IsTrue(DiagnosticSqlCommandParser.TryParse(
            "EXPLAIN ANALYZE select d.Dummy from #system.dual() d",
            out var explainCommand,
            out var explainDiagnostics));
        Assert.IsNotNull(explainCommand);
        Assert.IsNull(explainDiagnostics);
        Assert.AreEqual(DiagnosticSqlCommandKind.ExplainAnalyze, explainCommand.Kind);
        Assert.AreEqual("select d.Dummy from #system.dual() d", explainCommand.InnerScript);

        Assert.IsFalse(DiagnosticSqlCommandParser.TryParse(
            "select d.Dummy from #system.dual() d",
            out var normalCommand,
            out var normalDiagnostics));
        Assert.IsNull(normalCommand);
        Assert.IsNull(normalDiagnostics);
    }

    [TestMethod]
    public void DiagnosticSqlCommandParser_ShouldSkipRuntimePreambleStatements()
    {
        var cases = new (string Name, string Script, DiagnosticSqlCommandKind ExpectedKind, string ExpectedInnerScript)[]
        {
            (
                "param",
                "param(expected: string); PROFILE select d.Dummy from #system.dual() d where d.Dummy = $expected",
                DiagnosticSqlCommandKind.Profile,
                "param(expected: string); select d.Dummy from #system.dual() d where d.Dummy = $expected"),
            (
                "let",
                "let expected: string = 'single'; PROFILE select d.Dummy from #system.dual() d where d.Dummy = $expected",
                DiagnosticSqlCommandKind.Profile,
                "let expected: string = 'single'; select d.Dummy from #system.dual() d where d.Dummy = $expected"),
            (
                "table",
                "table Items { Name: string }; PROFILE select d.Dummy from #system.dual() d",
                DiagnosticSqlCommandKind.Profile,
                "table Items { Name: string }; select d.Dummy from #system.dual() d"),
            (
                "table-couple",
                "table Items { Name: string }; couple #test.whatever with table Items as Items; EXPLAIN ANALYZE select d.Dummy from #system.dual() d",
                DiagnosticSqlCommandKind.ExplainAnalyze,
                "table Items { Name: string }; couple #test.whatever with table Items as Items; select d.Dummy from #system.dual() d"),
            (
                "binary-schema",
                "binary Rec { Len: byte }; PROFILE select d.Dummy from #system.dual() d",
                DiagnosticSqlCommandKind.Profile,
                "binary Rec { Len: byte }; select d.Dummy from #system.dual() d"),
            (
                "text-schema",
                "text Data { Content: rest }; EXPLAIN ANALYZE select d.Dummy from #system.dual() d",
                DiagnosticSqlCommandKind.ExplainAnalyze,
                "text Data { Content: rest }; select d.Dummy from #system.dual() d")
        };

        foreach (var testCase in cases)
        {
            Assert.IsTrue(
                DiagnosticSqlCommandParser.TryParse(
                    testCase.Script,
                    out var command,
                    out var diagnostics),
                testCase.Name);
            Assert.IsNotNull(command, testCase.Name);
            Assert.IsNull(diagnostics, testCase.Name);
            Assert.AreEqual(testCase.ExpectedKind, command.Kind, testCase.Name);
            Assert.AreEqual(testCase.ExpectedInnerScript, command.InnerScript, testCase.Name);
        }
    }

    [TestMethod]
    public void DiagnosticSqlCommandParser_WhenResultProducingStatementComesFirst_ShouldNotClassify()
    {
        Assert.IsFalse(DiagnosticSqlCommandParser.TryParse(
            "select d.Dummy from #system.dual() d; PROFILE select e.Dummy from #system.dual() e",
            out var command,
            out var diagnostics));
        Assert.IsNull(command);
        Assert.IsNull(diagnostics);
    }

    [TestMethod]
    public void DiagnosticSqlCommandParser_WhenDiagnosticFormIsInvalid_ShouldReturnDiagnostics()
    {
        Assert.IsTrue(DiagnosticSqlCommandParser.TryParse(
            "explain select d.Dummy from #system.dual() d",
            out var command,
            out var diagnostics));

        Assert.IsNull(command);
        Assert.IsNotNull(diagnostics);
        Assert.IsGreaterThanOrEqualTo(1, diagnostics.Count);
        StringAssert.Contains(diagnostics[0].Message, "EXPLAIN without ANALYZE");
    }

    [TestMethod]
    public void CompileForExplainAnalyze_WhenParameterized_ShouldAllowParametersBeforeRun()
    {
        using var compiled = InstanceCreator.CompileForExplainAnalyze(
            "param(expected: string) select d.Dummy from #system.dual() d where d.Dummy = $expected",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        compiled.Parameters["expected"] = "single";

        var result = compiled.Run();

        Assert.AreEqual(1, result.Result.Count);
        Assert.AreEqual("single", result.Result[0][0]);
        Assert.AreEqual(1, compiled.ParameterDefinitions.Count);
        Assert.AreEqual(1, compiled.ParameterContracts.Count);
        StringAssert.Contains(result.ExplainAnalyzeText, "actual rows=");
    }

    [TestMethod]
    public void CompileForExplainAnalyze_WhenCancellationIsRequestedBeforeRun_ShouldThrow()
    {
        using var cancellation = new CancellationTokenSource();
        using var compiled = InstanceCreator.CompileForExplainAnalyze(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => compiled.Run(cancellation.Token));
    }

    [TestMethod]
    public void CompileForExplainAnalyze_ShouldPassRuntimeCancellationToDatasource()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingSystemSchemaProvider(cancellation.Cancel);
        using var compiled = InstanceCreator.CompileForExplainAnalyze(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.Throws<OperationCanceledException>(() => compiled.Run(cancellation.Token));
        Assert.IsTrue(provider.TokenReachedSource);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldRecordActualRowsForCoreGeneratedOperators()
    {
        var select = InstanceCreator.ExplainAnalyze(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
        var appendRow = AssertActualOperator(select, "AppendShape");
        var forEach = AssertActualOperator(select, "ChunkedForEach");

        Assert.AreEqual(select.Profile.Sources.Single().RowsRead, forEach.InputRows);
        Assert.AreEqual(select.Profile.Sources.Single().RowsRead, forEach.OutputRows);
        Assert.AreEqual(1, appendRow.OutputRows);

        var ordered = InstanceCreator.ExplainAnalyze(
            "select d.Dummy from #system.dual() d order by d.Dummy skip 0 take 1",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
        var appendRecord = AssertActualOperator(ordered, "AppendRecord");
        var materializeRecords = AssertActualOperator(ordered, "MaterializeRecordListToShapeRows");

        Assert.AreEqual(1, appendRecord.OutputRows);
        Assert.AreEqual(1, materializeRecords.InputRows);
        Assert.AreEqual(1, materializeRecords.OutputRows);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldRecordLoopRowsEvenWhenFilterRejectsRows()
    {
        var result = InstanceCreator.ExplainAnalyze(
            "select d.Dummy from #system.dual() d where false",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        var forEach = AssertActualOperator(result, "ChunkedForEach");
        var appendRow = result.Profile.Operators.FirstOrDefault(
            static operation => operation.Name.Equals("AppendShape", StringComparison.Ordinal));

        Assert.AreEqual(0, result.Result.Count);
        Assert.AreEqual(1, result.Profile.Sources.Single().RowsRead);
        Assert.AreEqual(1, forEach.InputRows);
        Assert.AreEqual(1, forEach.OutputRows);
        Assert.IsNotNull(appendRow);
        Assert.AreEqual(0, appendRow.OutputRows);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldRecordStoredGeneratedRowsLoopRows()
    {
        var result = InstanceCreator.ExplainAnalyze(
            "with x as (select d.Dummy from #system.dual() d) select x.Dummy from x",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        var forEachLoops = result.Profile.Operators
            .Where(static operation =>
                operation.Name.Equals("ForEach", StringComparison.Ordinal) ||
                operation.Name.Equals("ChunkedForEach", StringComparison.Ordinal))
            .ToArray();

        Assert.IsTrue(
            forEachLoops.Any(static operation => operation.HasActualStats && operation.InputRows == 1 && operation.OutputRows == 1),
            result.ExplainAnalyzeText);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldRecordActualStatsForJoinAggregateAndCteHelpers()
    {
        var join = InstanceCreator.ExplainAnalyze(
            "select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
        var hashAdd = AssertActualOperator(join, "HashAdd");
        var hashProbe = AssertActualOperator(join, "HashProbe");

        Assert.AreEqual(1, hashAdd.OutputRows);
        Assert.AreEqual(1, hashProbe.InputRows);

        var aggregate = InstanceCreator.ExplainAnalyze(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        AssertActualOperator(aggregate, "ParallelSingleKeyAggregateLoop");

        var cte = InstanceCreator.ExplainAnalyze(
            "with p as (select d.Dummy as Dummy from #system.dual() d), q as (select Dummy from p), r as (select Dummy from p) select q.Dummy, r.Dummy from q inner join r on q.Dummy = r.Dummy",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        StringAssert.Contains(cte.ExecutionPlanText, "CtePhase");
        AssertActualOperator(cte, "CreateTable");
        AssertActualOperator(cte, "AppendRow");
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldReportCounterOnlyLeafElapsedAsUnavailable()
    {
        var result = InstanceCreator.ExplainAnalyze(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        var lookup = AssertActualOperator(result, "AppendShape");

        Assert.IsFalse(lookup.HasElapsedTime);
        StringAssert.Contains(result.ExplainAnalyzeText, "AppendShape");
        StringAssert.Contains(result.ExplainAnalyzeText, "elapsed=n/a");
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldReportOperatorStatsForCommonQueryShapes()
    {
        var cases = new (string Name, string Query, CompilationOptions? Options)[]
        {
            ("aggregate", "select Count(d.Dummy) as Count from #system.dual() d", null),
            ("join", "select d.Dummy, e.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy = e.Dummy", null),
            ("cte", "with x as (select d.Dummy from #system.dual() d) select x.Dummy from x", null),
            ("parallel", "with a as (select d.Dummy from #system.dual() d), b as (select e.Dummy from #system.dual() e) select a.Dummy from a inner join b on a.Dummy = b.Dummy", new CompilationOptions(useCteParallelization: true))
        };

        foreach (var testCase in cases)
        {
            var result = InstanceCreator.ExplainAnalyze(
                testCase.Query,
                Guid.NewGuid().ToString(),
                _schemaProvider,
                _loggerResolver,
                testCase.Options);

            Assert.IsGreaterThanOrEqualTo(1, result.Result.Count, testCase.Name);
            Assert.IsGreaterThanOrEqualTo(2, result.Profile.Operators.Count, testCase.Name);
            StringAssert.Contains(result.ExplainAnalyzeText, "actual rows=");
            StringAssert.Contains(result.ExplainAnalyzeText, "elapsed=");
            Assert.IsTrue(result.Profile.Operators.Any(static operation => operation.HasActualStats), testCase.Name);
        }
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldKeepSourceWaitAndConsumerGapSeparateFromOperatorElapsed()
    {
        var result = InstanceCreator.ExplainAnalyze(
            "select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.IsGreaterThanOrEqualTo(1, result.Profile.Sources.Count);
        StringAssert.Contains(result.ExplainAnalyzeText, "Source boundary stats:");
        StringAssert.Contains(result.ExplainAnalyzeText, "MoveNext wait:");
        StringAssert.Contains(result.ExplainAnalyzeText, "Consumer gap:");
        StringAssert.Contains(result.ExplainAnalyzeText, "actual rows=");
    }

    [TestMethod]
    public void InstanceCreatorProfile_WhenCancellationIsRequestedBeforeRun_ShouldThrow()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            InstanceCreator.Profile(
                "select d.Dummy from #system.dual() d",
                Guid.NewGuid().ToString(),
                _schemaProvider,
                _loggerResolver,
                cancellation.Token));
    }

    [TestMethod]
    public void InstanceCreatorExplainAnalyze_WhenCancellationIsRequestedBeforeRun_ShouldThrow()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            InstanceCreator.ExplainAnalyze(
                "select d.Dummy from #system.dual() d",
                Guid.NewGuid().ToString(),
                _schemaProvider,
                _loggerResolver,
                cancellation.Token));
    }

    [TestMethod]
    public void InstanceCreatorProfile_ShouldPassRuntimeCancellationToDatasource()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingSystemSchemaProvider(cancellation.Cancel);

        Assert.Throws<OperationCanceledException>(() =>
            InstanceCreator.Profile(
                "select d.Dummy from #system.dual() d",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                cancellation.Token));

        Assert.IsTrue(provider.TokenReachedSource);
    }

    [TestMethod]
    public void InstanceCreatorExplainAnalyze_ShouldPassRuntimeCancellationToDatasource()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingSystemSchemaProvider(cancellation.Cancel);

        Assert.Throws<OperationCanceledException>(() =>
            InstanceCreator.ExplainAnalyze(
                "select d.Dummy from #system.dual() d",
                Guid.NewGuid().ToString(),
                provider,
                _loggerResolver,
                cancellation.Token));

        Assert.IsTrue(provider.TokenReachedSource);
    }

    [TestMethod]
    public void ExplainAnalyze_ShouldReuseProfileCompilationForExecutionPlan()
    {
        const string query = "select d.Dummy from #system.dual() d";
        var baselineProvider = new CountingSystemSchemaProvider();
        var explainAnalyzeProvider = new CountingSystemSchemaProvider();

        var baseline = InstanceCreator.CompileForProfile(
            query,
            Guid.NewGuid().ToString(),
            baselineProvider,
            _loggerResolver,
            new CompilationOptions(instrumentationMode: QueryInstrumentationMode.Full));
        baseline.RunWithProfile();

        InstanceCreator.ExplainAnalyze(
            query,
            Guid.NewGuid().ToString(),
            explainAnalyzeProvider,
            _loggerResolver);

        Assert.AreEqual(baselineProvider.GetSchemaCalls, explainAnalyzeProvider.GetSchemaCalls);
    }

    [TestMethod]
    public void ProfileSqlCommand_ShouldReturnDiagnosticsTextTable()
    {
        var table = CompileForExecution("profile select d.Dummy from #system.dual() d").Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Musoq query profile");
        StringAssert.Contains(text, "Rows read: 1");
        Assert.IsFalse(table.Columns.Any(column => column.ColumnName == "d.Dummy"));
    }

    [TestMethod]
    public void ExplainAnalyzeSqlCommand_ShouldReturnAnnotatedPlanTextTable()
    {
        var table = CompileForExecution("EXPLAIN ANALYZE select d.Dummy from #system.dual() d").Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Musoq explain analyze");
        StringAssert.Contains(text, "[op1] ExecutionPlan");
        StringAssert.Contains(text, "actual rows=");
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenParameterized_ShouldUseOuterParameters()
    {
        var query = CompileForExecution(
            "param(expected: string) PROFILE select d.Dummy from #system.dual() d where d.Dummy = $expected");

        query.Parameters["expected"] = "single";

        var table = query.Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Rows read: 1");
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenLetPreambleBeforeProfile_ShouldReturnDiagnosticsTextTable()
    {
        var table = CompileForExecution(
            "let expected: string = 'single'; PROFILE select d.Dummy from #system.dual() d where d.Dummy = $expected")
            .Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Musoq query profile");
        StringAssert.Contains(text, "Rows read: 1");
        Assert.IsFalse(table.Columns.Any(column => column.ColumnName == "d.Dummy"));
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenLetPreambleBeforeExplainAnalyze_ShouldReturnDiagnosticsTextTable()
    {
        var table = CompileForExecution(
            "let expected: string = 'single'; EXPLAIN ANALYZE select d.Dummy from #system.dual() d where d.Dummy = $expected")
            .Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Musoq explain analyze");
        StringAssert.Contains(text, "[op1] ExecutionPlan");
        StringAssert.Contains(text, "actual rows=");
        Assert.IsFalse(table.Columns.Any(column => column.ColumnName == "d.Dummy"));
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenParamAndLetPreambleBeforeProfile_ShouldUseBothPreambles()
    {
        var query = CompileForExecution(
            "param(expected: string); let marker: string = 'single'; PROFILE select d.Dummy from #system.dual() d where d.Dummy = $expected and d.Dummy = $marker");

        query.Parameters["expected"] = "single";

        var table = query.Run();
        var text = ReadDiagnosticsText(table);

        AssertDiagnosticTextTable(table);
        StringAssert.Contains(text, "Musoq query profile");
        StringAssert.Contains(text, "Rows read: 1");
        Assert.IsFalse(table.Columns.Any(column => column.ColumnName == "d.Dummy"));
    }

    [TestMethod]
    public void ExplainAnalyzeSqlCommand_ShouldReuseInnerCompilationForExecutionPlan()
    {
        const string query = "select d.Dummy from #system.dual() d";
        var baselineProvider = new CountingSystemSchemaProvider();
        var commandProvider = new CountingSystemSchemaProvider();

        var baseline = InstanceCreator.CompileForProfile(
            query,
            Guid.NewGuid().ToString(),
            baselineProvider,
            _loggerResolver,
            new CompilationOptions(instrumentationMode: QueryInstrumentationMode.Full));
        baseline.RunWithProfile();

        var command = InstanceCreator.CompileForExecution(
            "EXPLAIN ANALYZE " + query,
            Guid.NewGuid().ToString(),
            commandProvider,
            _loggerResolver);
        command.Run();

        Assert.AreEqual(baselineProvider.GetSchemaCalls, commandProvider.GetSchemaCalls);
    }

    [TestMethod]
    public void DiagnosticSqlCommand_ShouldPassRuntimeCancellationToDatasource()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingSystemSchemaProvider(cancellation.Cancel);
        var command = InstanceCreator.CompileForExecution(
            "profile select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver);

        Assert.Throws<OperationCanceledException>(() => command.Run(cancellation.Token));
        Assert.IsTrue(provider.TokenReachedSource);
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenExplainWithoutAnalyze_ShouldReturnClearDiagnostic()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "explain select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DiagnosticCode.MQ2040_InvalidDiagnosticCommand, result.Errors[0].Code);
        StringAssert.Contains(result.Errors[0].Message, "EXPLAIN without ANALYZE is not supported");
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenStandaloneAnalyze_ShouldReturnClearDiagnostic()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "analyze select d.Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DiagnosticCode.MQ2040_InvalidDiagnosticCommand, result.Errors[0].Code);
        StringAssert.Contains(result.Errors[0].Message, "Standalone ANALYZE is not implemented");
    }

    [TestMethod]
    public void DiagnosticSqlCommand_WhenUnsupportedInnerForm_ShouldReturnParserDiagnostic()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "profile table temp {Name: string}",
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(DiagnosticCode.MQ2040_InvalidDiagnosticCommand, result.Errors[0].Code);
        StringAssert.Contains(result.Errors[0].Message, "Expected SELECT, FROM, WITH, PIVOT, or UNPIVOT");
    }

    [TestMethod]
    public void CompileForExecution_ShouldKeepDefaultAndProfiledCacheEntriesSeparate()
    {
        const string query = "select d.Dummy from #system.dual() d where d.Dummy = 'single'";
        var normal = CompileForExecution(query);
        var profiled = CompileForExecution(query, CreateSourceBoundaryProfileOptions());

        var normalRunnable = GetRunnable(normal);
        var profiledRunnable = GetRunnable(profiled);

        Assert.IsFalse(normalRunnable is IProfiledRunnable);
        Assert.IsTrue(profiledRunnable is IProfiledRunnable);
        Assert.AreNotEqual(normalRunnable.GetType(), profiledRunnable.GetType());
    }

    private static CompilationOptions CreateSourceBoundaryProfileOptions() =>
        new(instrumentationMode: QueryInstrumentationMode.SourceBoundaries);

    private static ITableRunnable GetRunnable(CompiledQuery query)
    {
        var field = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(typeof(CompiledQuery).FullName, "_runnable");

        return (ITableRunnable)(field.GetValue(query)
                           ?? throw new InvalidOperationException("CompiledQuery has no runnable instance."));
    }

    private static void AssertTablesEqual(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        Assert.AreEqual(expected.Columns.Count(), actual.Columns.Count());

        for (var rowIndex = 0; rowIndex < expected.Count; rowIndex++)
            CollectionAssert.AreEqual(expected[rowIndex].Values, actual[rowIndex].Values);
    }

    private static void AssertDiagnosticTextTable(Table table)
    {
        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual("LineNumber", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual(typeof(int), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual("Text", table.Columns.ElementAt(1).ColumnName);
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(1).ColumnType);
        Assert.IsGreaterThanOrEqualTo(1, table.Count);
    }

    private static string ReadDiagnosticsText(Table table)
    {
        return string.Join("\n", table.Rows.Select(row => row[1]?.ToString() ?? string.Empty));
    }

    private static OperatorProfileSnapshot AssertActualOperator(ExplainAnalyzeResult result, string name)
    {
        var operation = result.Profile.Operators.FirstOrDefault(
            op => op.HasActualStats && op.Name.Equals(name, StringComparison.Ordinal));

        Assert.IsNotNull(operation, $"Expected actual operator stats for {name}.\n{result.ExplainAnalyzeText}");
        return operation;
    }

    private sealed class ReportingSystemSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ReportingSystemSchema();
    }

    private sealed class CountingSystemSchemaProvider : ISchemaProvider
    {
        public int GetSchemaCalls { get; private set; }

        public ISchema GetSchema(string schema)
        {
            GetSchemaCalls++;
            return new SystemSchema();
        }
    }

    private sealed class CancellingSystemSchemaProvider(Action cancel) : ISchemaProvider
    {
        public bool TokenReachedSource { get; private set; }

        public ISchema GetSchema(string schema) => new CancellingSystemSchema(cancel, () => TokenReachedSource = true);
    }

    private sealed class CancellingSystemSchema(Action cancel, Action tokenReachedSource) : SystemSchema
    {
        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return name.Equals("dual", StringComparison.OrdinalIgnoreCase)
                ? EnsureSourceType<T, DualEntity>(name, new CancellingDualRowSource(executionContext, cancel, tokenReachedSource))
                : base.GetRowSource<T>(name, executionContext, parameters);
        }
    }

    private sealed class CancellingDualRowSource(
        SourceExecutionContext context,
        Action cancel,
        Action tokenReachedSource) : RowSource<DualEntity>
    {
        public override IEnumerable<IReadOnlyList<DualEntity>> Chunks
        {
            get
            {
                if (context.EndWorkToken.CanBeCanceled)
                    tokenReachedSource();

                cancel();
                context.EndWorkToken.ThrowIfCancellationRequested();
                yield return [new DualEntity()];
            }
        }
    }

    private sealed class ReportingSystemSchema : SystemSchema
    {
        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return name.Equals("dual", StringComparison.OrdinalIgnoreCase)
                ? EnsureSourceType<T, DualEntity>(name, new ReportingDualRowSource(executionContext))
                : base.GetRowSource<T>(name, executionContext, parameters);
        }
    }

    private sealed class ReportingDualRowSource(SourceExecutionContext context) : RowSource<DualEntity>
    {
        public override IEnumerable<IReadOnlyList<DualEntity>> Chunks
        {
            get
            {
                const string sourceName = "#system.dual";

                context.ReportDataSourceBegin(sourceName);
                context.ReportDataSourceRowsKnown(sourceName, 1);
                context.ReportDataSourceRowsRead(sourceName, 1, 1);
                yield return [new DualEntity()];
                context.ReportDataSourceEnd(sourceName, 1);
            }
        }
    }
}
