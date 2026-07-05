using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeProfiledSamplesShapeTests
{
    private const string GeneratedCodeSectionMarker = "// === SyntaxTree:";
    private const string DisabledSimpleSelectWhereFileName = "P01_SimpleSelectWhere_Disabled.cs";
    private const string SourceBoundariesSimpleSelectWhereFileName = "P02_SimpleSelectWhere_SourceBoundaries.cs";
    private const string FullSimpleSelectWhereFileName = "P03_SimpleSelectWhere_Full.cs";
    private const string InnerJoinFullFileName = "P04_InnerJoin_Full.cs";
    private const string GroupBySingleFullFileName = "P05_GroupBySingle_Full.cs";
    private const string OrderBySkipTakeFullFileName = "P06_OrderBySkipTake_Full.cs";
    private const string ParallelCteFullFileName = "P07_ParallelCte_Full.cs";

    private static readonly string[] CuratedFullSampleFileNames =
    [
        InnerJoinFullFileName,
        GroupBySingleFullFileName,
        OrderBySkipTakeFullFileName,
        ParallelCteFullFileName
    ];

    private static readonly string[] RegularProfiledSelectSampleFileNames =
    [
        SourceBoundariesSimpleSelectWhereFileName,
        FullSimpleSelectWhereFileName,
        InnerJoinFullFileName,
        GroupBySingleFullFileName,
        OrderBySkipTakeFullFileName,
        ParallelCteFullFileName
    ];

    private static readonly Regex OperatorScopePattern = new(
        @"\bvar __op\d+Scope = profileRecorder\?\.BeginOperatorValue\(__op\d+Handle\) \?\? OperatorProfileValueScope\.None",
        RegexOptions.Compiled);

    private static readonly string[] SourceBoundaryMarkers =
    [
        "QueryProfileRecorder",
        "CreateAdaptiveSourceRecorder",
        "CreateDiagnostics",
        "SourceDiagnostics.None",
        "ProfiledChunkedEnumerable"
    ];

    private static readonly string[] FullSourceBoundaryMarkers =
    [
        "QueryProfileRecorder",
        "CreateSourceRecorder",
        "CreateDiagnostics",
        "SourceDiagnostics.None",
        "ProfiledChunkedEnumerable"
    ];

    private static readonly string[] FullOperatorMarkers =
    [
        "GetOperatorHandle",
        "OperatorProfileHandle.None",
        "BeginOperatorValue",
        "OperatorProfileValueScope.None",
        "AddOperatorOutputRows"
    ];

    private static readonly string[] ProfileExceptionBoundaryMarkers =
    [
        "GetCurrentOperatorScopeDepth",
        "RecordActiveOperatorException",
        "DisposeActiveOperatorScopes"
    ];

    private static readonly string[] PerOperatorExceptionMarkers =
    [
        "catch (Exception __op",
        "RecordException("
    ];

    private static readonly string[] DisabledProfilingMarkers =
    [
        "QueryProfileRecorder",
        "ProfiledChunkedEnumerable",
        "SourceDiagnostics",
        "GetOperatorHandle",
        "OperatorProfileHandle",
        "OperatorProfileValueScope",
        "BeginOperator",
        "AddOperatorInputRows",
        "AddOperatorOutputRows",
        "AddInputRows",
        "AddOutputRows"
    ];

    private static readonly string[] SourceBoundariesOnlyAbsentMarkers =
    [
        "GetOperatorHandle",
        "OperatorProfileHandle",
        "OperatorProfileValueScope",
        "BeginOperator",
        "AddOperatorInputRows",
        "AddOperatorOutputRows",
        "AddInputRows",
        "AddOutputRows"
    ];

    private static readonly string[] RetiredHelperPatterns =
    [
        "EvaluationHelper.GetColumnValue(",
        "EvaluationHelper.SmartForEach(",
        "EvaluationHelper.ConvertTableToSource(",
        "TableRowSource"
    ];

    [TestMethod]
    public void DisabledSimpleSelectWhereSample_WhenCheckedIn_ShouldNotContainProfilingMarkers()
    {
        var sample = ReadProfiledSample(DisabledSimpleSelectWhereFileName);

        AssertDoesNotContainAny(sample, DisabledProfilingMarkers, DisabledSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void SourceBoundariesSimpleSelectWhereSample_WhenCheckedIn_ShouldContainSourceBoundaryMarkers()
    {
        var sample = ReadProfiledSample(SourceBoundariesSimpleSelectWhereFileName);

        AssertContainsAll(sample, SourceBoundaryMarkers, SourceBoundariesSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void SourceBoundariesSimpleSelectWhereSample_WhenCheckedIn_ShouldNotContainFullOperatorMarkers()
    {
        var sample = ReadProfiledSample(SourceBoundariesSimpleSelectWhereFileName);

        AssertDoesNotContainAny(sample, SourceBoundariesOnlyAbsentMarkers, SourceBoundariesSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void FullSimpleSelectWhereSample_WhenCheckedIn_ShouldContainSourceAndOperatorMarkers()
    {
        var sample = ReadProfiledSample(FullSimpleSelectWhereFileName);

        AssertContainsAll(sample, FullSourceBoundaryMarkers, FullSimpleSelectWhereFileName);
        AssertContainsAll(sample, FullOperatorMarkers, FullSimpleSelectWhereFileName);
        AssertContainsAll(
            sample,
            [
                "ProfiledOperatorEnumerable<ResultShape0>.Create",
                "profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0"
            ],
            FullSimpleSelectWhereFileName);
        AssertDoesNotContainAny(sample, PerOperatorExceptionMarkers, FullSimpleSelectWhereFileName);
        Assert.IsTrue(
            OperatorScopePattern.IsMatch(sample),
            $"{FullSimpleSelectWhereFileName} should contain generated __op...Scope variables.");

        var appendRow = ResolveOperator(sample, FullSimpleSelectWhereFileName, "AppendShape", "ResultShape0(Name: ko3iko.Name");
        var loop = ResolveOperator(sample, FullSimpleSelectWhereFileName, "ChunkedForEach", "ko3iko in ko3ikoRows");
        var let = ResolveOperator(sample, FullSimpleSelectWhereFileName, "Let", "population: decimal");
        var branch = ResolveOperator(sample, FullSimpleSelectWhereFileName, "If", "population > 0");

        AssertContainsCounterOutputRows(sample, FullSimpleSelectWhereFileName, appendRow, "1");
        AssertDoesNotContainBeginOperator(sample, FullSimpleSelectWhereFileName, appendRow);
        AssertDoesNotContainBeginOperator(sample, FullSimpleSelectWhereFileName, let);
        AssertDoesNotContainBeginOperator(sample, FullSimpleSelectWhereFileName, branch);
        AssertContainsLoopRowCounters(sample, FullSimpleSelectWhereFileName, loop);
    }

    [TestMethod]
    public void SimpleSelectWhereProfiledSamples_WhenCheckedIn_ShouldStreamFinalShapeRowsWhenNonBlocking()
    {
        var sourceBoundaries = ReadProfiledSample(SourceBoundariesSimpleSelectWhereFileName);
        var full = ReadProfiledSample(FullSimpleSelectWhereFileName);

        AssertContainsAll(
            sourceBoundaries,
            [
                "yield return new ResultShape0(ko3iko.Name, population);",
                "yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population);"
            ],
            SourceBoundariesSimpleSelectWhereFileName);
        AssertDoesNotContainAny(
            sourceBoundaries,
            [
                "var __musoqFinalShapeRows = new List<ResultShape0>();",
                "__musoqFinalShapeRows.Add(",
                "return __musoqFinalShapeRows;",
                "ProfiledOperatorEnumerable<ResultShape0>.Create"
            ],
            SourceBoundariesSimpleSelectWhereFileName);

        AssertContainsAll(
            full,
            [
                "ProfiledOperatorEnumerable<ResultShape0>.Create",
                "profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0",
                "yield return new ResultShape0(ko3iko.Name, population);",
                "yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population);"
            ],
            FullSimpleSelectWhereFileName);
        AssertDoesNotContainAny(
            full,
            [
                "var __musoqFinalShapeRows = new List<ResultShape0>();",
                "__musoqFinalShapeRows.Add(",
                "return __musoqFinalShapeRows;",
                "catch (Exception __profileException)when"
            ],
            FullSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void RegularProfiledSelectSamples_WhenCheckedIn_ShouldUseDeferredShapeRows()
    {
        var samples = ReadProfiledSamples(RegularProfiledSelectSampleFileNames);

        foreach (var sample in samples)
        {
            var generatedCode = ExtractGeneratedCodeSection(sample.Content);

            AssertContainsAll(
                generatedCode,
                [
                    "QueryRows.DeferredTable<ResultRow0>",
                    "public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)",
                    "private IEnumerable<ResultRow0> ComputeRows_compiled_0(",
                    "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(",
                    "CancellationToken token, QueryProfileRecorder profileRecorder",
                    "foreach (var __musoqShapeRow in",
                    "yield return new ResultRow0(",
                    "private sealed class ResultShape0"
                ],
                sample.FileName);

            AssertDoesNotContainAny(
                generatedCode,
                [
                    "ComputeTable_compiled_0",
                    "private Table",
                    "new Table(\"result\"",
                    "result.AddDirect("
                ],
                sample.FileName);
        }
    }

    [TestMethod]
    public void FullSimpleSelectWhereSample_WhenCheckedIn_ShouldKeepNormalQuerySemanticsVisible()
    {
        var sample = ReadProfiledSample(FullSimpleSelectWhereFileName);

        Assert.Contains(
            "GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>",
            sample,
            FullSimpleSelectWhereFileName);
        Assert.Contains(
            "var ko3ikoRows = ko3ikoRowsProfile == null ? ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Create",
            sample,
            FullSimpleSelectWhereFileName);
        Assert.Contains(
            "yield return new ResultShape0(ko3iko.Name, population);",
            sample,
            FullSimpleSelectWhereFileName);
        Assert.Contains(
            "yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Population);",
            sample,
            FullSimpleSelectWhereFileName);
        AssertDoesNotContainAny(sample, RetiredHelperPatterns, FullSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void FullSimpleSelectWhereSample_WhenCheckedIn_ShouldBatchCounterOnlyRows()
    {
        var sample = ReadProfiledSample(FullSimpleSelectWhereFileName);
        var appendRow = ResolveOperator(sample, FullSimpleSelectWhereFileName, "AppendShape", "ResultShape0(Name: ko3iko.Name");
        var loop = ResolveOperator(sample, FullSimpleSelectWhereFileName, "ChunkedForEach", "ko3iko in ko3ikoRows");

        AssertContainsLoopRowCounters(sample, FullSimpleSelectWhereFileName, loop);
        AssertContainsCounterOutputRows(sample, FullSimpleSelectWhereFileName, appendRow, "1");
        AssertDoesNotContainAny(
            sample,
            [
                $"{CreateScopePrefix(appendRow)}Scope",
                $"{CreateScopePrefix(appendRow)}Scope.AddOutputRows(1)"
            ],
            FullSimpleSelectWhereFileName);
    }

    [TestMethod]
    public void CuratedFullProfiledSamples_WhenCheckedIn_ShouldContainSourceAndOperatorMarkers()
    {
        var samples = ReadProfiledSamples(CuratedFullSampleFileNames);

        foreach (var sample in samples)
        {
            AssertContainsAll(sample.Content, FullSourceBoundaryMarkers, sample.FileName);
            AssertContainsAll(sample.Content, FullOperatorMarkers, sample.FileName);
            AssertContainsAll(sample.Content, ProfileExceptionBoundaryMarkers, sample.FileName);
            AssertDoesNotContainAny(sample.Content, PerOperatorExceptionMarkers, sample.FileName);
            Assert.IsTrue(
                OperatorScopePattern.IsMatch(sample.Content),
                $"{sample.FileName} should contain generated __op...Scope variables.");
        }
    }

    [TestMethod]
    public void InnerJoinFullSample_WhenCheckedIn_ShouldShowHashBuildProbeProfiling()
    {
        var sample = ReadProfiledSample(InnerJoinFullFileName);
        var hashAdd = ResolveOperator(sample, InnerJoinFullFileName, "HashAdd", "bHash[b.Id] += b");
        var hashProbe = ResolveOperator(sample, InnerJoinFullFileName, "HashProbe", "bHash[a.Id] -> bHashMatches");
        var matchLoop = ResolveOperator(sample, InnerJoinFullFileName, "ForEach", "b in bHashMatches");
        var appendRow = ResolveOperator(sample, InnerJoinFullFileName, "AppendShape", "ResultShape0(a.Name: a.Name");

        AssertContainsAll(
            sample,
            [
                "PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]",
                "CreateHash [bHash: int -> BasicEntity]",
                "HashAdd [bHash[b.Id] += b]",
                "HashProbe [bHash[a.Id] -> bHashMatches]",
                "var bHash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();",
                "foreach (var b in bHashMatches)",
                "__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));",
                "CancellationToken token",
                "token.ThrowIfCancellationRequested();"
            ],
            InnerJoinFullFileName);
        AssertContainsCounterOutputRows(sample, InnerJoinFullFileName, hashAdd, "1");
        AssertContainsCounterInputRows(sample, InnerJoinFullFileName, hashProbe, "1");
        AssertContainsCounterLoopRows(sample, InnerJoinFullFileName, matchLoop);
        AssertContainsCounterOutputRows(sample, InnerJoinFullFileName, appendRow, "1");
        AssertDoesNotContainBeginOperator(sample, InnerJoinFullFileName, hashAdd);
        AssertDoesNotContainBeginOperator(sample, InnerJoinFullFileName, hashProbe);
        AssertDoesNotContainBeginOperator(sample, InnerJoinFullFileName, matchLoop);
    }

    [TestMethod]
    public void InnerJoinFullSample_WhenCheckedIn_ShouldDeclareOperatorProfilingOnlyWhereUsed()
    {
        var sample = ReadProfiledSample(InnerJoinFullFileName);
        var hashAdd = ResolveOperator(sample, InnerJoinFullFileName, "HashAdd", "bHash[b.Id] += b");
        var hashProbe = ResolveOperator(sample, InnerJoinFullFileName, "HashProbe", "bHash[a.Id] -> bHashMatches");
        var matchLoop = ResolveOperator(sample, InnerJoinFullFileName, "ForEach", "b in bHashMatches");
        var appendRow = ResolveOperator(sample, InnerJoinFullFileName, "AppendShape", "ResultShape0(a.Name: a.Name");

        var shapeRowsMethod = ExtractMethod(sample, InnerJoinFullFileName, "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(");

        AssertContainsCounterOutputRows(shapeRowsMethod, InnerJoinFullFileName, hashAdd, "1");
        AssertContainsCounterInputRows(shapeRowsMethod, InnerJoinFullFileName, hashProbe, "1");
        AssertContainsCounterLoopRows(shapeRowsMethod, InnerJoinFullFileName, matchLoop);
        AssertContainsCounterOutputRows(shapeRowsMethod, InnerJoinFullFileName, appendRow, "1");
        AssertDoesNotContainAny(
            sample,
            [
                "private Table ComputeTable_compiled_0(",
                "private static void BuildBHash(",
                "private static void AppendHashJoinRows("
            ],
            InnerJoinFullFileName);
    }

    [TestMethod]
    public void GroupBySingleFullSample_WhenCheckedIn_ShouldShowAggregateProfiling()
    {
        var sample = ReadProfiledSample(GroupBySingleFullFileName);
        var aggregateContext = ResolveOperator(sample, GroupBySingleFullFileName, "CreateSingleKeyAggregateContext", "groups: string");
        var aggregateLoop = ResolveOperator(sample, GroupBySingleFullFileName, "ParallelSingleKeyAggregateLoop", "ko3iko in ko3ikoRows");
        var aggregateSet = ResolveOperator(sample, GroupBySingleFullFileName, "TypedAggregateSet", "Set(group.__agg0, city)");

        AssertContainsAll(
            sample,
            [
                "PhysicalSingleKeyAggregate [key: City (String)] [aggs: Count(City)]",
                "AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]",
                "CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]",
                "ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group ResultAggregateGroup]",
                "TypedAggregateSet [Set(group.__agg0, city)]",
                "ParallelSingleKeyAggregate_0(groupsToFinalizeParallelRows, 24, token);"
            ],
            GroupBySingleFullFileName);
        Assert.DoesNotContain("SerialSingleKeyAggregate", sample);
        AssertContainsBeginOperator(sample, GroupBySingleFullFileName, aggregateContext);
        AssertContainsBeginOperator(sample, GroupBySingleFullFileName, aggregateLoop);
        AssertDoesNotContainBeginOperator(sample, GroupBySingleFullFileName, aggregateSet);
        AssertContainsOutputRows(sample, GroupBySingleFullFileName, aggregateLoop, "groupsToFinalize.Count");
    }

    [TestMethod]
    public void OrderBySkipTakeFullSample_WhenCheckedIn_ShouldShowTopOffsetProfiling()
    {
        var sample = ReadProfiledSample(OrderBySkipTakeFullFileName);
        var boundedList = ResolveOperator(sample, OrderBySkipTakeFullFileName, "CreateBoundedRecordList", "resultOrderRecords");
        var materialize = ResolveOperator(sample, OrderBySkipTakeFullFileName, "MaterializeRecordListToShapeRows", "resultOrderRecords -> result");

        AssertContainsAll(
            sample,
            [
                "PhysicalTopOffset [skip 2, take 5] [ko3iko.Population DESC]",
                "CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by Population DESC, skip 2, take 5]",
                "MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1]",
                "new EvaluationHelper.BoundedTopRecordList<ResultRow0WithSortKeys>(2, 5, ResultRow0WithSortKeysComparer.Instance)"
            ],
            OrderBySkipTakeFullFileName);
        AssertContainsBeginOperator(sample, OrderBySkipTakeFullFileName, boundedList);
        AssertContainsBeginOperator(sample, OrderBySkipTakeFullFileName, materialize);
        AssertContainsInputRows(sample, OrderBySkipTakeFullFileName, materialize, "resultOrderRecords.Count");
        AssertContainsOutputRows(sample, OrderBySkipTakeFullFileName, materialize, "__musoqFinalShapeRows.Count");
    }

    [TestMethod]
    public void ParallelCteFullSample_WhenCheckedIn_ShouldPassProfileRecorderThroughHelpers()
    {
        var sample = ReadProfiledSample(ParallelCteFullFileName);

        AssertContainsAll(
            sample,
            [
                "ParallelBlock [cte-level-0, tasks 2, maxDegree 2]",
                "var _cteRowResults = new CteRowResults();",
                "var _cteIndexResults = new CteIndexResults();",
                "var cteLevel0Runner = new CteLevel0Runner(",
                "private readonly Musoq.Evaluator.Diagnostics.QueryProfileRecorder _profileRecorder;",
                "private static List<Cte0Row0> BuildCteLevel0Task0",
                "private static object BuildCteLevel0Task1",
                "Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder",
                "CteRowResults _cteRowResults",
                "CteIndexResults _cteIndexResults",
                "BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _profileRecorder",
                "BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _profileRecorder",
                "_cteRowResults.Slot0 = __parallelCteLevel0Task0Result",
                "_cteIndexResults.Slot0 = cte1HashSidecar0Name",
                "var __storedTable0Rows = _cteRowResults.Slot0;",
                "var qHash = _cteIndexResults.Slot0;",
                "CreateSourceRecorder(\"ko3iko\")",
                "CreateSourceRecorder(\"vo04qt\")"
            ],
            ParallelCteFullFileName);

        AssertDoesNotContainAny(
            sample,
            [
                "private static Musoq.Evaluator.Tables.Table BuildCteLevel0Task0",
                "private static Musoq.Evaluator.Tables.Table BuildCteLevel0Task1",
                "new Table(\"cte0\"",
                "new Table(\"cte1\"",
                "_tableResults[0]",
                "_tableResults[1]",
                "CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)",
                "CastGeneratedRows<Cte1Row0>(_tableResults[1].Rows)",
                "_cteRowResults.Slot1",
                "var __storedTable1Rows = _cteRowResults.Slot1;"
            ],
            ParallelCteFullFileName);
    }

    [TestMethod]
    public void ParallelCteFullSample_WhenCheckedIn_ShouldCountSidecarHashProbeRows()
    {
        var sample = ReadProfiledSample(ParallelCteFullFileName);
        var hashProbe = ResolveOperator(sample, ParallelCteFullFileName, "HashProbe", "qHash[p.Name] -> qHashMatches");

        AssertContainsAll(
            sample,
            [
                "LoadCteIndex [qHash <- _cteIndexResults.Slot0 Hash: string]",
                "HashProbe [qHash[p.Name] -> qHashMatches]",
                "var qHash = _cteIndexResults.Slot0;"
            ],
            ParallelCteFullFileName);
        AssertContainsCounterInputRows(sample, ParallelCteFullFileName, hashProbe, "1");
        AssertDoesNotContainBeginOperator(sample, ParallelCteFullFileName, hashProbe);
    }

    private static readonly Lazy<IReadOnlyDictionary<string, string>> ProfiledSamples = new(CreateProfiledSamples);

    private static string ReadProfiledSample(string fileName)
    {
        if (ProfiledSamples.Value.TryGetValue(fileName, out var content))
            return content;

        Assert.Fail($"Unknown profiled generated sample '{fileName}'.");
        return string.Empty;
    }

    private static IReadOnlyDictionary<string, string> CreateProfiledSamples()
    {
        var loggerResolver = new TestsLoggerResolver();

        return GeneratedCodeProfiledSamplesCatalog.Samples
            .ToDictionary(
                static sample => sample.FileName,
                sample => GeneratedCodeSampleArtifacts.Generate(sample, loggerResolver),
                StringComparer.Ordinal);
    }

    private static string ExtractGeneratedCodeSection(string sample)
    {
        var index = sample.IndexOf(GeneratedCodeSectionMarker, StringComparison.Ordinal);

        return index < 0 ? sample : sample[index..];
    }

    private static (string FileName, string Content)[] ReadProfiledSamples(IEnumerable<string> fileNames)
    {
        return fileNames
            .Select(fileName => (fileName, ReadProfiledSample(fileName)))
            .ToArray();
    }

    private static void AssertContainsAll(string sample, IEnumerable<string> markers, string fileName)
    {
        var missingMarkers = markers
            .Where(marker => !sample.Contains(marker, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(missingMarkers, $"{fileName} is missing markers: {string.Join(", ", missingMarkers)}");
    }

    private static void AssertDoesNotContainAny(string sample, IEnumerable<string> markers, string fileName)
    {
        var presentMarkers = markers
            .Where(marker => sample.Contains(marker, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(presentMarkers, $"{fileName} contains unexpected markers: {string.Join(", ", presentMarkers)}");
    }

    private static void AssertContainsTryFinallyOperatorScope(string sample, string fileName, string scopePrefix)
    {
        AssertContainsAll(
            sample,
            [
                "try",
                "finally",
                $"{scopePrefix}Scope.Dispose();"
            ],
            fileName);
    }

    private static void AssertContainsBeginOperator(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertContainsAll(
            sample,
            [
                $"var {scopePrefix}Handle = profileRecorder?.GetOperatorHandle(\"{descriptor.Id}\", \"{descriptor.NodeKind}\") ?? OperatorProfileHandle.None;",
                $"BeginOperatorValue({scopePrefix}Handle)"
            ],
            fileName);
    }

    private static void AssertDoesNotContainBeginOperator(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertDoesNotContainAny(
            sample,
            [
                $"BeginOperatorValue({scopePrefix}Handle)",
                $"var {scopePrefix}Scope ="
            ],
            fileName);
    }

    private static void AssertContainsInputRows(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor,
        string expression)
    {
        Assert.Contains(
            $"{CreateScopePrefix(descriptor)}Scope.AddInputRows({expression})",
            sample,
            fileName);
    }

    private static void AssertContainsOutputRows(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor,
        string expression)
    {
        Assert.Contains(
            $"{CreateScopePrefix(descriptor)}Scope.AddOutputRows({expression})",
            sample,
            fileName);
    }

    private static void AssertContainsCounterInputRows(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor,
        string expression)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertContainsAll(
            sample,
            [
                $"long {scopePrefix}InputRows = 0L;",
                $"{scopePrefix}InputRows += {expression};",
                $"profileRecorder?.AddOperatorInputRows({scopePrefix}Handle, {scopePrefix}InputRows);"
            ],
            fileName);
    }

    private static void AssertContainsCounterOutputRows(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor,
        string expression)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertContainsAll(
            sample,
            [
                $"long {scopePrefix}OutputRows = 0L;",
                $"{scopePrefix}OutputRows += {expression};",
                $"profileRecorder?.AddOperatorOutputRows({scopePrefix}Handle, {scopePrefix}OutputRows);"
            ],
            fileName);
    }

    private static void AssertContainsLoopRowCounters(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertContainsAll(
            sample,
            [
                $"long {scopePrefix}InputRows = 0L;",
                $"long {scopePrefix}OutputRows = 0L;",
                $"{scopePrefix}InputRows++;",
                $"{scopePrefix}OutputRows++;",
                $"{scopePrefix}Scope.AddInputRows({scopePrefix}InputRows);",
                $"{scopePrefix}Scope.AddOutputRows({scopePrefix}OutputRows);"
            ],
            fileName);
        AssertDoesNotContainAny(
            sample,
            [
                $"{scopePrefix}Scope.AddInputRows(1);",
                $"{scopePrefix}Scope.AddOutputRows(1);"
            ],
            fileName);
    }

    private static void AssertContainsCounterLoopRows(
        string sample,
        string fileName,
        ExecutionPlanOperatorDescriptor descriptor)
    {
        var scopePrefix = CreateScopePrefix(descriptor);

        AssertContainsAll(
            sample,
            [
                $"long {scopePrefix}InputRows = 0L;",
                $"long {scopePrefix}OutputRows = 0L;",
                $"{scopePrefix}InputRows++;",
                $"{scopePrefix}OutputRows++;",
                $"profileRecorder?.AddOperatorInputRows({scopePrefix}Handle, {scopePrefix}InputRows);",
                $"profileRecorder?.AddOperatorOutputRows({scopePrefix}Handle, {scopePrefix}OutputRows);"
            ],
            fileName);
    }

    private static ExecutionPlanOperatorDescriptor ResolveOperator(
        string sample,
        string fileName,
        string nodeKind,
        string displayNameFragment,
        int occurrence = 0)
    {
        var catalog = ExecutionPlanOperatorCatalog.Create(ExtractIntermediateRepresentation(sample, fileName));
        var matches = catalog.Operators
            .Where(operation =>
                operation.NodeKind.Equals(nodeKind, StringComparison.Ordinal) &&
                operation.DisplayName.Contains(displayNameFragment, StringComparison.Ordinal))
            .ToArray();

        Assert.IsLessThan(
            matches.Length,
            occurrence,
            $"{fileName} should contain operator {nodeKind} with text '{displayNameFragment}'.");

        return matches[occurrence];
    }

    private static string ExtractIntermediateRepresentation(string sample, string fileName)
    {
        const string header = "intermediate representation";

        var headerIndex = sample.IndexOf(header, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, headerIndex, $"{fileName} should contain an intermediate representation block.");

        var contentStart = sample.IndexOf('\n', headerIndex);
        Assert.AreNotEqual(-1, contentStart, $"{fileName} should contain an intermediate representation body.");

        var contentEnd = sample.IndexOf("*/", contentStart, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, contentEnd, $"{fileName} should close the intermediate representation block.");

        return sample[(contentStart + 1)..contentEnd].Trim();
    }

    private static string ExtractMethod(string sample, string fileName, string methodSignaturePrefix)
    {
        var signatureIndex = sample.IndexOf(methodSignaturePrefix, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, signatureIndex, $"{fileName} should contain method '{methodSignaturePrefix}'.");

        var openingBraceIndex = sample.IndexOf('{', signatureIndex);
        Assert.AreNotEqual(-1, openingBraceIndex, $"{fileName} should contain a body for method '{methodSignaturePrefix}'.");

        var depth = 0;
        for (var index = openingBraceIndex; index < sample.Length; index++)
        {
            switch (sample[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return sample[signatureIndex..(index + 1)];
                    break;
            }
        }

        Assert.Fail($"{fileName} should close method '{methodSignaturePrefix}'.");
        return string.Empty;
    }

    private static string CreateScopePrefix(ExecutionPlanOperatorDescriptor descriptor) => $"__{descriptor.Id}";
}
