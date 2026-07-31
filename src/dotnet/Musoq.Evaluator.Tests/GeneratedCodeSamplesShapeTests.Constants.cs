using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private const int ExpectedSampleFileCount = 233;
    private const int InlineInArrayAllocationBudget = 0;
    private const string CrossApplySampleFileName = "Q19_CrossApply.cs";
    private const string OuterApplySampleFileName = "Q20_OuterApply.cs";
    private const string AccessMethodApplySampleFileName = "Q62_AccessMethodApply.cs";
    private const string OuterAccessMethodApplySampleFileName = "Q63_OuterAccessMethodApply.cs";
    private const string CteBackedAsOfJoinSampleFileName = "Q64_CteBackedAsOfJoin.cs";
    private const string AggregateOverHashJoinSampleFileName = "Q65_AggregateOverHashJoin.cs";
    private const string CteBackedAggregateOverHashJoinSampleFileName = "Q66_CteBackedAggregateOverHashJoin.cs";
    private const string DynamicCteBackedAsOfJoinSampleFileName = "Q67_DynamicCteBackedAsOfJoin.cs";
    private const string CteWithJoinSampleFileName = "Q15_CteWithJoin.cs";
    private const string ChainedApplyGroupedAggregateWindowSampleFileName =
        "Q61_ChainedApplyGroupedAggregateWindow.cs";
    private const string ChainedApplyWindowSampleFileName = "Q68_ChainedApplyWindow.cs";
    private const string ChainedApplyMixedDistinctAggregateSortSampleFileName =
        "Q69_ChainedApplyMixedDistinctAggregateSort.cs";
    private const string ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName =
        "Q70_ChainedApplyMixedDistinctMinMaxAggregateSort.cs";
    private const string ChainedApplyMixedDistinctAvgAggregateSortSampleFileName =
        "Q71_ChainedApplyMixedDistinctAvgAggregateSort.cs";
    private const string ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName =
        "Q72_ChainedApplyMixedDistinctMinMaxAggregateWindow.cs";
    private const string ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName =
        "Q73_ChainedApplyMixedDistinctAvgAggregateWindow.cs";
    private const string ChainedApplyQualifyWindowSampleFileName =
        "Q74_ChainedApplyQualifyWindow.cs";
    private const string ChainedApplyGroupedAggregateQualifyWindowSampleFileName =
        "Q75_ChainedApplyGroupedAggregateQualifyWindow.cs";
    private const string ApplyWithOrdinalitySampleFileName = "Q173_ApplyWithOrdinality.cs";
    private const string CteDistinctJoinByCountrySampleFileName = "Q50_CteDistinctJoinByCountry.cs";
    private const string CteJoinFrameQualifySampleFileName = "Q47_CteJoinFrameQualify.cs";
    private const string InSubqueryBasicSampleFileName = "Q99_InSubqueryBasic.cs";
    private const string UnionSampleFileName = "Q09_Union.cs";
    private const string ExceptSampleFileName = "Q14_Except.cs";
    private const string UnionAllSampleFileName = "Q25_UnionAll.cs";
    private const string IntersectSampleFileName = "Q29_Intersect.cs";
    private const string GroupBySingleSampleFileName = "Q05_GroupBySingle.cs";
    private const string GroupByHavingOrderBySampleFileName = "Q07_GroupByHavingOrderBy.cs";
    private const string CteDownstreamSampleFileName = "Q02_CteDownstream.cs";
    private const string MultipleCteChainedSampleFileName = "Q28_MultipleCteChained.cs";
    private const string MultipleWindowsSampleFileName = "Q13_MultipleWindows.cs";
    private const string WindowLagSampleFileName = "Q12_WindowLag.cs";
    private const string WindowLeadSampleFileName = "Q27_WindowLead.cs";
    private const string WindowRankDenseRankSampleFileName = "Q26_WindowRankDenseRank.cs";
    private const string GroupBySkipTakeSampleFileName = "Q30_GroupBySkipTake.cs";
    private const string OrderBySkipTakeSampleFileName = "Q22_OrderBySkipTake.cs";
    private const string OrderByTopOffsetHiddenKeySampleFileName = "Q110_OrderByTopOffsetHiddenKey.cs";
    private const string LargeInClauseSampleFileName = "Q42_InClauseLarge20Values.cs";
    private const string WindowSumWholePartitionDecimalSampleFileName = "Q78_WindowSumWholePartitionDecimal.cs";
    private const string WindowSumRunningDecimalSampleFileName = "Q79_WindowSumRunningDecimal.cs";
    private const string WindowAvgRunningDecimalSampleFileName = "Q80_WindowAvgRunningDecimal.cs";
    private const string WindowRunningProductPluginSampleFileName = "Q81_WindowRunningProductPlugin.cs";
    private const string ParallelIndependentCtesSampleFileName = "Q82_ParallelIndependentCtes.cs";
    private const string CompositeHashJoinSampleFileName = "Q83_CompositeHashJoin.cs";
    private const string RepeatedCteSelfJoinSampleFileName = "Q84_RepeatedCteSelfJoin.cs";
    private const string OrderByTakeSampleFileName = "Q85_OrderByTake.cs";
    private const string WindowRunningProductFramedPluginSampleFileName =
        "Q86_WindowRunningProductFramedPlugin.cs";
    private const string IsDistinctFromNullSafeComparisonSampleFileName =
        "Q168_IsDistinctFromNullSafeComparison.cs";
    private const string NullsFirstLastOrderingSampleFileName =
        "Q169_NullsFirstLastOrdering.cs";
    private const string SelectStarRenameSampleFileName =
        "Q170_SelectStarRename.cs";
    private const string DescQuerySampleFileName = "Q175_DescQuery.cs";
    private const string CompilationSimpleSelectSampleFileName = "Q87_CompilationSimpleSelect.cs";
    private const string CompilationComplexGroupedSortSampleFileName = "Q88_CompilationComplexGroupedSort.cs";
    private const string CompilationCseDisabledSampleFileName = "Q89_CompilationCseDisabled.cs";
    private const string CompilationCseEnabledSampleFileName = "Q90_CompilationCseEnabled.cs";
    private const string OrderBySimpleSampleFileName = "Q91_OrderBySimple.cs";
    private const string OrderByMultipleKeysSampleFileName = "Q92_OrderByMultipleKeys.cs";
    private const string OrderByAliasSampleFileName = "Q93_OrderByAlias.cs";
    private const string OrderByHiddenComputedKeySampleFileName = "Q94_OrderByHiddenComputedKey.cs";
    private const string RuntimeV2CseNoDuplicateRegressionSampleFileName =
        "Q100_RuntimeV2CseNoDuplicateRegression.cs";
    private const string RuntimeV2WindowRunningSumSampleFileName =
        "Q101_RuntimeV2WindowRunningSum.cs";
    private const string RuntimeV2WindowQualifyRankSampleFileName =
        "Q102_RuntimeV2WindowQualifyRank.cs";
    private const string RuntimeV2SkipTakeNoOrderSampleFileName =
        "Q103_RuntimeV2SkipTakeNoOrder.cs";
    private const string RuntimeV2StringFilterSampleFileName =
        "Q104_RuntimeV2StringFilter.cs";
    private const string RuntimeV2DeterministicMethodCseSampleFileName =
        "Q105_RuntimeV2DeterministicMethodCse.cs";
    private const string RuntimeV2DeterministicMethodCseDisabledSampleFileName =
        "Q105_RuntimeV2DeterministicMethodCseDisabled.cs";
    private const string RuntimeV2ParallelFilterProjectSampleFileName =
        "Q106_RuntimeV2ParallelFilterProject.cs";
    private const string RuntimeV2LexerManyColumnsSampleFileName =
        "Q107_RuntimeV2LexerManyColumns.cs";
    private const string RuntimeV2DecimalConversionSampleFileName =
        "Q108_RuntimeV2DecimalConversion.cs";
    private const string RuntimeV2CompositeRegressionCanarySampleFileName =
        "Q109_RuntimeV2CompositeRegressionCanary.cs";
    private const string RuntimeV2WindowBenchmarkRowNumberNoPartitionSampleFileName =
        "Q111_RuntimeV2WindowBenchmarkRowNumberNoPartition.cs";
    private const string RuntimeV2WindowBenchmarkRowNumberPartitionedSampleFileName =
        "Q112_RuntimeV2WindowBenchmarkRowNumberPartitioned.cs";
    private const string RuntimeV2WindowBenchmarkRankPartitionedSampleFileName =
        "Q113_RuntimeV2WindowBenchmarkRankPartitioned.cs";
    private const string RuntimeV2WindowBenchmarkDenseRankPartitionedSampleFileName =
        "Q114_RuntimeV2WindowBenchmarkDenseRankPartitioned.cs";
    private const string RuntimeV2WindowBenchmarkCountWholePartitionSampleFileName =
        "Q115_RuntimeV2WindowBenchmarkCountWholePartition.cs";
    private const string RuntimeV2ParallelTableAddBenchmarkSampleFileName =
        "Q116_RuntimeV2ParallelTableAddBenchmark.cs";
    private const string BenchmarkCseNoDuplicateMaterializedSampleFileName =
        "Q176_BenchmarkCseNoDuplicateMaterialized.cs";
    private const string BenchmarkCseCaseNoDuplicateMaterializedSampleFileName =
        "Q177_BenchmarkCseCaseNoDuplicateMaterialized.cs";
    private const string BenchmarkParallelTableAddMaterializedSampleFileName =
        "Q178_BenchmarkParallelTableAddMaterialized.cs";
    private const string BenchmarkOptimizedHeavyMixedMaterializedSampleFileName =
        "Q179_BenchmarkOptimizedHeavyMixedMaterialized.cs";
    private const string BenchmarkOptimizedMixedColumnMethodMaterializedSampleFileName =
        "Q180_BenchmarkOptimizedMixedColumnMethodMaterialized.cs";
    private const string BenchmarkCompilationSimpleMaterializedSampleFileName =
        "Q181_BenchmarkCompilationSimpleMaterialized.cs";
    private const string BenchmarkCompilationComplexMaterializedSampleFileName =
        "Q182_BenchmarkCompilationComplexMaterialized.cs";
    private const string BenchmarkInterpretationMultipleFilesMaterializedSampleFileName =
        "Q183_BenchmarkInterpretationMultipleFilesMaterialized.cs";
    private const string BenchmarkInterpretationHighThroughputMaterializedSampleFileName =
        "Q184_BenchmarkInterpretationHighThroughputMaterialized.cs";
    private const string ValuesRowLiteralsSampleFileName = "Q117_ValuesRowLiterals.cs";
    private const string ValuesCteReuseSampleFileName = "Q118_ValuesCteReuse.cs";
    private const string ValuesNumericLiteralsSampleFileName = "Q119_ValuesNumericLiterals.cs";
    private const string ValuesStaticParametersAndLetsSampleFileName =
        "Q171_ValuesStaticParametersAndLets.cs";
    private const string CollectionParameterInMembershipSampleFileName =
        "Q172_CollectionParameterInMembership.cs";
    private const string ScriptParametersWhereSelectSampleFileName = "Q120_ScriptParametersWhereSelect.cs";
    private const string ScriptParameterPrimitiveDefaultsSampleFileName = "Q121_ScriptParameterPrimitiveDefaults.cs";
    private const string ScriptParameterSourceArgumentSampleFileName = "Q122_ScriptParameterSourceArgument.cs";
    private const string ScriptParameterGroupByHelperCaptureSampleFileName =
        "Q123_ScriptParameterGroupByHelperCapture.cs";
    private const string ScriptParameterJoinHelperCaptureSampleFileName =
        "Q124_ScriptParameterJoinHelperCapture.cs";
    private const string ScriptParameterCteHelperCaptureSampleFileName =
        "Q125_ScriptParameterCteHelperCapture.cs";
    private const string ScriptParameterWindowHelperCaptureSampleFileName =
        "Q126_ScriptParameterWindowHelperCapture.cs";
    private const string ScriptParameterParallelHelperCaptureSampleFileName =
        "Q127_ScriptParameterParallelHelperCapture.cs";
    private const string ScriptParameterTypedComparisonSampleFileName =
        "Q128_ScriptParameterTypedComparison.cs";
    private const string ScriptParameterNumericWideningComparisonSampleFileName =
        "Q129_ScriptParameterNumericWideningComparison.cs";
    private const string CorrelatedInSubquerySampleFileName = "Q138_CorrelatedInSubquery.cs";
    private const string CorrelatedNotExistsSubquerySampleFileName = "Q139_CorrelatedNotExistsSubquery.cs";
    private const string CorrelatedScalarAggregateSubquerySampleFileName =
        "Q140_CorrelatedScalarAggregateSubquery.cs";
    private const string ScalarSubqueryJoinOnSampleFileName = "Q141_ScalarSubqueryJoinOn.cs";
    private const string CorrelatedAllSubquerySampleFileName = "Q142_CorrelatedAllSubquery.cs";
    private const string CorrelatedApplyDerivedTableSampleFileName = "Q143_CorrelatedApplyDerivedTable.cs";
    private const string CorrelatedCompositeValueTypeSubquerySampleFileName =
        "Q144_CorrelatedCompositeValueTypeSubquery.cs";
    private const string CorrelatedApplySelectiveDerivedTableSampleFileName =
        "Q145_CorrelatedApplySelectiveDerivedTable.cs";
    private const string CorrelatedCompositeRangeMarkSampleFileName =
        "Q186_CorrelatedCompositeRangeMark.cs";
    private const string CteSidecarHashJoinSampleFileName = "Q146_CteSidecarHashJoin.cs";
    private const string CteSidecarKeySetSemiJoinSampleFileName = "Q147_CteSidecarKeySetSemiJoin.cs";
    private const string CteSidecarFanoutThreeHashesSampleFileName = "Q148_CteSidecarFanoutThreeHashes.cs";
    private const string CteSidecarStagedGraphMixedSampleFileName = "Q149_CteSidecarStagedGraphMixed.cs";
    private const string RuntimeV2CastProjectionSampleFileName =
        "Q150_RuntimeV2CastProjection.cs";
    private const string RuntimeV2CastExpressionsSampleFileName =
        "Q151_RuntimeV2CastExpressions.cs";
    private const string RuntimeV2CastAggregateGroupingSampleFileName =
        "Q152_RuntimeV2CastAggregateGrouping.cs";
    private const string RuntimeV2GroupByOrdinalSampleFileName =
        "Q153_RuntimeV2GroupByOrdinal.cs";
    private const string RuntimeV2GroupByAllCastsSampleFileName =
        "Q154_RuntimeV2GroupByAllCasts.cs";
    private const string RuntimeV2AliasWhereGroupBySampleFileName =
        "Q155_RuntimeV2AliasWhereGroupBy.cs";
    private const string RuntimeV2HavingAggregateAliasSampleFileName =
        "Q156_RuntimeV2HavingAggregateAlias.cs";
    private const string RuntimeV2AliasSourceConflictSampleFileName =
        "Q157_RuntimeV2AliasSourceConflict.cs";
    private const string RuntimeV2CombinedGroupingSampleFileName =
        "Q158_RuntimeV2CombinedGrouping.cs";
    private const string UnpivotBasicStreamingSampleFileName = "Q159_UnpivotBasicStreaming.cs";
    private const string UnpivotCteNullableOrderingSampleFileName = "Q160_UnpivotCteNullableOrdering.cs";
    private const string UnpivotSetOperatorSampleFileName = "Q161_UnpivotSetOperator.cs";
    private const string PivotGroupedSingleMeasureSampleFileName = "Q162_PivotGroupedSingleMeasure.cs";
    private const string PivotMultipleMeasuresSampleFileName = "Q163_PivotMultipleMeasures.cs";
    private const string PivotCteNoGroupBySampleFileName = "Q164_PivotCteNoGroupBy.cs";
    private const string RuntimeV2WeatherSingleAggregateSampleFileName =
        "Q185_RuntimeV2WeatherSingleAggregate.cs";
    private const string ExceptWithGroupBySidesSampleFileName = "Q99_ExceptWithGroupBySides.cs";
    private const string Union3WithGroupBySidesSampleFileName = "Q99_Union3WithGroupBySides.cs";
    private const string UnionWithGroupBySidesSampleFileName = "Q99_UnionWithGroupBySides.cs";
    private const string InnerJoinSampleFileName = "Q03_InnerJoin.cs";
    private const string AsOfJoinSampleFileName = "Q33_AsOfJoin.cs";
    private const string AsOfTieBreakSampleFileName = "Q174_AsOfTieBreak.cs";
    private const string LeftJoinSampleFileName = "Q04_LeftJoin.cs";
    private const string RightJoinSampleFileName = "Q21_RightJoin.cs";
    private const string LeftJoinWithMultipleColumnsSampleFileName = "Q35_LeftJoinWithMultipleColumns.cs";
    private const string LeftJoinTwoSchemasSameKeySampleFileName = "Q49_LeftJoinTwoSchemasSameKey.cs";
    private static readonly string[] DirectInterpretationProjectionSampleFileNames =
    [
        "Q16_BinaryInterpret.cs",
        "Q17_TextParse.cs",
        "Q51_BinaryConditionalInterpret.cs",
        "Q52_BinaryStringInterpret.cs",
        "Q53_BinaryComputedInterpret.cs",
        "Q54_BinaryNestedInterpret.cs"
    ];
    private static readonly string[] NestedInterpretationExpansionSampleFileNames =
    [
        "Q55_BinaryInlineArrayInterpret.cs",
        "Q56_BinaryStringRepeatUntilInterpret.cs",
        "Q57_BinaryInlineRepeatUntilInterpret.cs",
        "Q58_BinaryGenericInterpret.cs",
        "Q59_BinaryNestedGenericInterpret.cs",
        "Q60_BinaryBitsRepeatUntilInterpret.cs"
    ];
    private static readonly string[] BenchmarkInterpretationMaterializedSampleFileNames =
    [
        BenchmarkInterpretationMultipleFilesMaterializedSampleFileName,
        BenchmarkInterpretationHighThroughputMaterializedSampleFileName
    ];
    private static readonly string[] BenchmarkMaterializedSampleFileNames =
    [
        BenchmarkCseNoDuplicateMaterializedSampleFileName,
        BenchmarkCseCaseNoDuplicateMaterializedSampleFileName,
        BenchmarkParallelTableAddMaterializedSampleFileName,
        BenchmarkOptimizedHeavyMixedMaterializedSampleFileName,
        BenchmarkOptimizedMixedColumnMethodMaterializedSampleFileName,
        BenchmarkCompilationSimpleMaterializedSampleFileName,
        BenchmarkCompilationComplexMaterializedSampleFileName
    ];
    private static readonly string[] RuntimeV2CastGroupingFeatureSampleFileNames =
    [
        RuntimeV2CastProjectionSampleFileName,
        RuntimeV2CastExpressionsSampleFileName,
        RuntimeV2CastAggregateGroupingSampleFileName,
        RuntimeV2GroupByOrdinalSampleFileName,
        RuntimeV2GroupByAllCastsSampleFileName,
        RuntimeV2AliasWhereGroupBySampleFileName,
        RuntimeV2HavingAggregateAliasSampleFileName,
        RuntimeV2AliasSourceConflictSampleFileName,
        RuntimeV2CombinedGroupingSampleFileName
    ];
    private static readonly string[] UnpivotSampleFileNames =
    [
        UnpivotBasicStreamingSampleFileName,
        UnpivotCteNullableOrderingSampleFileName,
        UnpivotSetOperatorSampleFileName
    ];
    private static readonly string[] PivotSampleFileNames =
    [
        PivotGroupedSingleMeasureSampleFileName,
        PivotMultipleMeasuresSampleFileName,
        PivotCteNoGroupBySampleFileName
    ];

    private const string GetColumnValuePattern = "EvaluationHelper.GetColumnValue(";
    private const string ConvertTableToSourcePattern = "EvaluationHelper.ConvertTableToSource(";
    private const string ConvertTableToSourceWithDiscardedContextsPattern = "EvaluationHelper.ConvertTableToSourceWithDiscardedContexts(";
    private const string TableRowSourcePattern = "TableRowSource";
    private const string ObjectResolverPattern = "IObjectResolver";
    private const string SmartForEachPattern = "EvaluationHelper.SmartForEach(";
    private const string ToDistinctTablePattern = "EvaluationHelper.ToDistinctTable(";
    private const string ContextsAccessPattern = ".Contexts";
    private const string AddDirectPattern = ".AddDirect(";
    private const string EnsureCapacityPattern = ".EnsureCapacity(";
    private const string HashSetPattern = "HashSet<";
    private const string StaticColumnMetadataPattern = "private static readonly Column[]";
    private const string StaticSchemaMetadataPattern = "private static readonly IReadOnlyCollection<ISchemaColumn>";
    private static readonly Regex MutableDynamicDictionaryPattern =
        new(@"\b(?:I)?Dictionary<string, object>");
    private const string ParallelFilterProjectLoopPattern = "ParallelFilterProjectLoop [";
    private const string ParallelProjectionRowsPattern = "EvaluationHelper.GetParallelProjectionRowsOrEmpty<";
    private const string ParallelProjectRowsPattern = "TypedProjectionRows.Project";
    private const string TableParallelProjectRowsPattern = "EvaluationHelper.ProjectRowsParallel<";
    private const string AddRowsDirectPattern = "QueryRows.FromRowShards(";
    private const string GeneratedCodeSectionMarker = "// === SyntaxTree:";
    private const string NullableHashJoinKeyPattern = "CreateNullableHashJoinKey(";
    private const string ObjectHashJoinBucketPattern = "Dictionary<object, HashJoinBucket<";
    private const string ObjectHashJoinKeyLocalPattern = "object key =";
    private const string WindowCompositeKeyPattern = "WindowFunctionHelpers.CompositeKey(";
    private const string HashJoinSingleLookupAddPattern =
        "System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault";
    private const string HashJoinDoubleLookupAddPattern = ".Add(key, new HashJoinBucket<";
    private static readonly Regex GeneratedLocalFunctionPattern =
        new(@"^\s{12}(?:void|[A-Za-z_][A-Za-z0-9_.<>?]*(?:\s*\[\])?)\s+(?:BuildCte|Populate|Finalize|RunCte)[A-Za-z0-9_]*\s*\(",
            RegexOptions.Multiline);
    private static readonly Regex GeneratedRowCastPattern =
        new(@"\(\((?<rowType>[A-Za-z_][A-Za-z0-9_]*Row\d+)\)(?<sourceName>[A-Za-z_][A-Za-z0-9_]*)\)");

    private const string ObjectsRowValueArrayCreationPattern = "new ObjectsRow(new object[]";
    private const int RetiredGeneratedCodePatternBudget = 0;

}
