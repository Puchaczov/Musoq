using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly RetiredGeneratedCodePattern[] RetiredGeneratedCodePatterns =
    [
        new(": GeneratedRow", RetiredGeneratedCodePatternBudget),
        new("GroupLayout", RetiredGeneratedCodePatternBudget),
        new("GroupSlot", RetiredGeneratedCodePatternBudget),
        new(".GetValue<", RetiredGeneratedCodePatternBudget),
        new(".SetValue(", RetiredGeneratedCodePatternBudget),
        new("GroupKey", RetiredGeneratedCodePatternBudget),
        new("new Group(", RetiredGeneratedCodePatternBudget),
        new("InjectGroup", RetiredGeneratedCodePatternBudget),
        new("ObjectsRow", RetiredGeneratedCodePatternBudget),
        new("ResolverContext", RetiredGeneratedCodePatternBudget),
        new("NestedResolverAccess", RetiredGeneratedCodePatternBudget),
        new("ObjectFallbackShape", RetiredGeneratedCodePatternBudget),
        new("__values", RetiredGeneratedCodePatternBudget),
        new("__cachedContexts", RetiredGeneratedCodePatternBudget),
        new("__ContextKind", RetiredGeneratedCodePatternBudget),
        new("__contextKind", RetiredGeneratedCodePatternBudget),
        new("public override object[] Values", RetiredGeneratedCodePatternBudget),
        new("EvaluationHelper.AggregateSingleKeyParallel", RetiredGeneratedCodePatternBudget),
        new("AggregateSingleKeyParallel_", RetiredGeneratedCodePatternBudget)
    ];

    private static readonly Regex GeneratedRowStagingLocalPattern =
        new(@"\bvar\s+[A-Za-z0-9_@]*Row\d+\s*=\s*new\s+(?![A-Za-z0-9_]*DynamicRow\d+\b)[A-Za-z0-9_]*Row\d+\(");

    private static readonly Regex LowercaseAggregateGroupTypePattern =
        new(@"\b[a-z][A-Za-z0-9_]*AggregateGroup\b");

    private static readonly ShapeBudget CorpusBudget = new()
    {
        GetColumnValue = 0,
        ConvertTableToSource = 0,
        ConvertTableToSourceWithDiscardedContexts = 0,
        TableRowSource = 0,
        ObjectResolver = 0,
        SmartForEach = 0,
        ContextsAccess = 0,
        DynamicDictionaryRead = 0
    };

    private static readonly IReadOnlyDictionary<string, ShapeBudget> OperatorFamilyBudgets =
        new Dictionary<string, ShapeBudget>
    {
        ["Apply"] = new()
        {
            ContextsAccess = 0
        },
        ["Compilation"] = new() { ContextsAccess = 0 },
        ["CTE"] = new() { ContextsAccess = 0 },
        ["Description"] = new() { ContextsAccess = 0 },
        ["Grouping"] = new() { ContextsAccess = 0 },
        ["InClause"] = new() { ContextsAccess = 0 },
        ["Interpretation"] = new()
        {
            GetColumnValue = 0,
            ContextsAccess = 0
        },
        ["Join"] = new()
        {
            GetColumnValue = 0,
            ConvertTableToSource = 0,
            SmartForEach = 0,
            ContextsAccess = 0
        },
        ["Ordering"] = new() { ContextsAccess = 0 },
        ["Pagination"] = new() { ContextsAccess = 0 },
        ["Parameters"] = new() { ContextsAccess = 0 },
        ["Pivot"] = new() { ContextsAccess = 0 },
        ["RuntimeV2"] = new() { ContextsAccess = 0 },
        ["RuntimeV2CastGrouping"] = new() { ContextsAccess = 0 },
        ["Scalar"] = new() { ContextsAccess = 0 },
        ["Scan"] = new() { ContextsAccess = 0 },
        ["Set"] = new(),
        ["Subquery"] = new() { ContextsAccess = 0 },
        ["Unpivot"] = new() { ContextsAccess = 0 },
        ["Values"] = new() { ContextsAccess = 0 },
        ["Variables"] = new() { ContextsAccess = 0 },
        ["Window"] = new()
        {
            ContextsAccess = 0
        }
    };

    private static readonly SourceScanShapeExpectation[] SourceScanShapeExpectations =
    [
        new("Q01_SimpleSelectWhere.cs", "ko3iko"),
        new("Q03_InnerJoin.cs", "a"),
        new("Q03_InnerJoin.cs", "b"),
        new("Q05_GroupBySingle.cs", "ko3iko"),
        new("Q10_WindowRowNumber.cs", "ko3iko", "var resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new("Q16_BinaryInterpret.cs", "f"),
        new(CrossApplySampleFileName, "a"),
        new(CrossApplySampleFileName, "b"),
        new(OuterApplySampleFileName, "a"),
        new(OuterApplySampleFileName, "b"),
        new(AsOfJoinSampleFileName, "a"),
        new(
            AsOfJoinSampleFileName,
            "b",
            "EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal>(bRows"),
        new(CteBackedAsOfJoinSampleFileName, "a"),
        new(AggregateOverHashJoinSampleFileName, "a"),
        new(AggregateOverHashJoinSampleFileName, "b"),
        new(CteBackedAggregateOverHashJoinSampleFileName, "a"),
        new(CteBackedAggregateOverHashJoinSampleFileName, "b"),
        new("Q61_ChainedApplyGroupedAggregateWindow.cs", "i"),
        new(AccessMethodApplySampleFileName, "i"),
        new(OuterAccessMethodApplySampleFileName, "i"),
        new(ChainedApplyWindowSampleFileName, "i"),
        new(ChainedApplyMixedDistinctAggregateSortSampleFileName, "i"),
        new(ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName, "i"),
        new(ChainedApplyMixedDistinctAvgAggregateSortSampleFileName, "i"),
        new(ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName, "i"),
        new(ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName, "i"),
        new(ChainedApplyQualifyWindowSampleFileName, "i"),
        new(ChainedApplyGroupedAggregateQualifyWindowSampleFileName, "i"),
        new(CompilationSimpleSelectSampleFileName, "ko3iko"),
        new(CompilationComplexGroupedSortSampleFileName, "ko3iko"),
        new(OrderBySimpleSampleFileName, "ko3iko"),
        new(OrderByMultipleKeysSampleFileName, "ko3iko"),
        new(OrderByAliasSampleFileName, "ko3iko"),
        new(OrderByHiddenComputedKeySampleFileName, "ko3iko"),
        new(RuntimeV2CseNoDuplicateRegressionSampleFileName, "ko3iko"),
        new(RuntimeV2WindowRunningSumSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2WindowQualifyRankSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2SkipTakeNoOrderSampleFileName, "ko3iko"),
        new(RuntimeV2StringFilterSampleFileName, "ko3iko"),
        new(RuntimeV2DeterministicMethodCseSampleFileName, "ko3iko"),
        new(RuntimeV2DeterministicMethodCseDisabledSampleFileName, "ko3iko"),
        new(RuntimeV2ParallelFilterProjectSampleFileName, "ko3iko"),
        new(RuntimeV2LexerManyColumnsSampleFileName, "ko3iko"),
        new(RuntimeV2DecimalConversionSampleFileName, "ko3iko"),
        new(RuntimeV2CompositeRegressionCanarySampleFileName, "ko3iko", "var resultWindowRows = new List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>();"),
        new(RuntimeV2WindowBenchmarkRowNumberNoPartitionSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2WindowBenchmarkRowNumberPartitionedSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2WindowBenchmarkRankPartitionedSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2WindowBenchmarkDenseRankPartitionedSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2WindowBenchmarkCountWholePartitionSampleFileName, "ko3iko", "resultWindowRows = EvaluationHelper.MaterializeRowsList(ko3ikoRows);"),
        new(RuntimeV2ParallelTableAddBenchmarkSampleFileName, "ko3iko"),
        new(BenchmarkCseNoDuplicateMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkCseCaseNoDuplicateMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkParallelTableAddMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkOptimizedHeavyMixedMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkOptimizedMixedColumnMethodMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkCompilationSimpleMaterializedSampleFileName, "ko3iko"),
        new(BenchmarkCompilationComplexMaterializedSampleFileName, "ko3iko")
    ];

    private static readonly string[] GroupedSetOperationSampleFileNames =
    [
        ExceptWithGroupBySidesSampleFileName,
        Union3WithGroupBySidesSampleFileName,
        UnionWithGroupBySidesSampleFileName
    ];

    private static readonly string[] SimpleOuterHashJoinSampleFileNames =
    [
        LeftJoinSampleFileName,
        RightJoinSampleFileName,
        LeftJoinWithMultipleColumnsSampleFileName,
        LeftJoinTwoSchemasSameKeySampleFileName
    ];

    private static readonly ShapeBudgetEntry[] ShapeBudgetEntries =
    [
        new(GetColumnValuePattern, static budget => budget.GetColumnValue),
        new(ConvertTableToSourcePattern, static budget => budget.ConvertTableToSource),
        new(
            ConvertTableToSourceWithDiscardedContextsPattern,
            static budget => budget.ConvertTableToSourceWithDiscardedContexts),
        new(TableRowSourcePattern, static budget => budget.TableRowSource),
        new(ObjectResolverPattern, static budget => budget.ObjectResolver),
        new(SmartForEachPattern, static budget => budget.SmartForEach),
        new(ContextsAccessPattern, static budget => budget.ContextsAccess),
        new("dynamic object dictionary reads", static budget => budget.DynamicDictionaryRead)
    ];

}
