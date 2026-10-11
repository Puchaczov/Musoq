using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeProfiledSamplesCatalog
{
    private const string SimpleSelectWhereQuery =
        "select Name, Population from #A.entities() where Population > 0";

    public static IReadOnlyList<GeneratedCodeSample> Samples { get; } =
    [
        ProfiledBasic(
            "P01_SimpleSelectWhere_Disabled",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.Disabled),
        ProfiledBasic(
            "P02_SimpleSelectWhere_SourceBoundaries",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.SourceBoundaries),
        ProfiledBasic(
            "P03_SimpleSelectWhere_Full",
            SimpleSelectWhereQuery,
            QueryInstrumentationMode.Full),
        ProfiledFromExisting("P04_InnerJoin_Full", "Q03_InnerJoin.cs"),
        ProfiledFromExisting("P05_GroupBySingle_Full", "Q05_GroupBySingle.cs"),
        ProfiledFromExisting("P06_OrderBySkipTake_Full", "Q22_OrderBySkipTake.cs"),
        ProfiledFromExisting("P07_ParallelCte_Full", "Q82_ParallelIndependentCtes.cs"),
        ProfiledFromExisting(
            "P08_RecursiveUnionAll_Disabled",
            "Q188_RecursiveUnionAllCounter.cs",
            QueryInstrumentationMode.Disabled),
        ProfiledFromExisting(
            "P09_RecursiveUnionAll_SourceBoundaries",
            "Q188_RecursiveUnionAllCounter.cs",
            QueryInstrumentationMode.SourceBoundaries),
        ProfiledFromExisting("P10_RecursiveUnionAll_Full", "Q188_RecursiveUnionAllCounter.cs"),
        ProfiledFromExisting("P11_RecursiveKeyedUnion_Full", "Q193_RecursiveUnionSingleKeyCycle.cs"),
        ProfiledFromExisting("P12_RecursiveInvariantIndexedEdges_Full", "Q203_RecursiveInvariantHashLookup.cs"),
        ProfiledFromExisting(
            "P13_RecursiveTypedInvariantDirectIndex_Full",
            "Q224_RecursiveCompositeInvariantSubplan.cs"),
        ProfiledFromExisting("P14_StableFilterProjectionReuse_Full", "Q252_StableFilterProjectionReuse.cs"),
        ProfiledFromExisting("P15_VolatileFilterProjectionReuse_Full", "Q253_VolatileFilterProjectionReuse.cs"),
        ProfiledFromExisting("P16_SharedStableWindowInputs_Full", "Q254_SharedStableWindowInputs.cs"),
        ProfiledFromExisting("P17_ParallelAggregateSharedArguments_Full", "Q256_ParallelAggregateSharedArguments.cs"),
        ProfiledFromExisting("P18_PivotPredicateDispatch_Full", "Q257_PivotPredicateDispatch.cs"),
        ProfiledFromExisting("P19_RecursiveStableScalarInvariant_Full", "Q267_RecursiveStableScalarInvariant.cs"),
        ProfiledFromExisting("P20_LibraryMixedNumericGroups_Full", "K01_LibraryMixedNumericGroups.cs"),
        ProfiledFromExisting("P21_LibraryCustomMultiArgumentGroups_Full", "K02_LibraryCustomMultiArgumentGroups.cs"),
        ProfiledFromExisting("P22_LibraryRunningWindow_Full", "K03_LibraryRunningWindow.cs"),
        ProfiledFromExisting("P23_TypedScalarParameters_Full", "K08_TypedScalarParameterSlots.cs"),
        ProfiledFromExisting("P24_TypedNestedJson_Full", "K09_TypedNestedJsonParameter.cs"),
        ProfiledFromExisting("P25_TypedStructuredDefaults_Full", "K12_TypedStructuredDeclarationDefaults.cs"),
        ProfiledFromExisting("P26_TypedJoinedSourceContexts_Full", "K13_TypedJoinedSourceContexts.cs"),
        ProfiledFromExisting("P27_TypedValueSourceContexts_Full", "K14_TypedValueSourceContexts.cs"),
        ProfiledFromExisting("P28_TypedIndexedValues_Full", "K16_TypedIndexedValues.cs"),
        ProfiledFromExisting("P29_IntegralDecimalAverage_Full", "K17_IntegralDecimalAverage.cs"),
        ProfiledFromExisting("P30_CustomKernelReset_Full", "K18_CustomKernelReset.cs"),
        ProfiledFromExisting("P31_ClosedGenericCustomAggregate_Full", "K19_ClosedGenericCustomAggregate.cs"),
        ProfiledFromExisting("P32_FilteredDecimalAverage_Full", "K20_FilteredDecimalAverage.cs"),
        ProfiledFromExisting("P33_TypedReferenceKernelState_Full", "K21_TypedReferenceKernelState.cs"),
        ProfiledFromExisting("P34_CustomScalarSubqueryEmptyReset_Full", "K22_CustomScalarSubqueryEmptyReset.cs"),
        ProfiledFromExisting("P35_TypedNumericWindows_Full", "K23_TypedNumericWindows.cs"),
        ProfiledFromExisting("P36_CustomWindowFrameArguments_Full", "K24_CustomWindowFrameArguments.cs"),
        ProfiledFromExisting("P37_TypedEmptyWindowFrames_Full", "K25_TypedEmptyWindowFrames.cs"),
        ProfiledFromExisting("P38_TypedDistinctWindowFrames_Full", "K26_TypedDistinctWindowFrames.cs"),
        ProfiledFromExisting("P39_TypedRangeWindowPeers_Full", "K27_TypedRangeWindowPeers.cs"),
        ProfiledFromExisting("P40_TypedWindowValueContext_Full", "K28_TypedWindowValueContext.cs"),
        ProfiledFromExisting("P41_TypedWindowRankingContext_Full", "K29_TypedWindowRankingContext.cs"),
        ProfiledFromExisting("P42_TypedWindowOutputTableProfile_Full", "K30_TypedWindowOutputValueCarrier.cs"),
        ProfiledFromExisting("P43_TypedWindowCapturedVolatileInputs_Full", "K31_TypedWindowCapturedVolatileInputs.cs"),
        ProfiledFromExisting("P44_TypedWindowExtremaBatch_Full", "K32_TypedWindowExtremaBatch.cs")
            with { OutputType = null },
        ProfiledFromExisting("P45_AuthorizedTypedCountMerge_Full", "K33_AuthorizedTypedCountMerge.cs"),
        ProfiledFromExisting("P46_CustomCountSerialDefault_Full", "K34_CustomCountSerialDefault.cs"),
        ProfiledFromExisting("P47_CheckedSumWithoutMergeEquivalence_Full", "K35_CheckedSumWithoutMergeEquivalence.cs"),
        ProfiledFromExisting("P48_AuthorizedScalarProjection_Full", "K36_AuthorizedScalarProjection.cs"),
        ProfiledFromExisting("P49_SerialDatasourceProjection_Full", "K37_SerialDatasourceProjection.cs"),
        ProfiledFromExisting("P50_TypedProviderAggregate_Full", "K38_TypedProviderAggregate.cs"),
        ProfiledFromExisting("P51_TypedProviderJoinedWindow_Full", "K39_TypedProviderJoinedWindow.cs"),
        ProfiledFromExisting("P52_TypedOrderKeyContext_Full", "K40_TypedOrderKeyContext.cs"),
        ProfiledFromExisting("P53_TypedCompositeOrderKeyContext_Full", "K41_TypedCompositeOrderKeyContext.cs"),
        ProfiledFromExisting("P54_InvoiceLibraryGrouped_Full", "K42_InvoiceLibraryGrouped.cs"),
        ProfiledFromExisting("P55_InvoiceLibraryWindowed_Full", "K43_InvoiceLibraryWindowed.cs"),
        ProfiledFromExisting("P56_TypedStateRangeOrderCapture_Full", "K44_TypedStateRangeOrderCapture.cs"),
        ProfiledFromExisting("P57_TypedTextSwitchResult_Full", "K45_TypedTextSwitchResult.cs"),
        ProfiledFromExisting("P58_TypedPartialPayload_Full", "K46_TypedPartialPayload.cs"),
        ProfiledFromExisting("P59_TypedIndexedMembers_Full", "K47_TypedIndexedMembers.cs"),
        ProfiledFromExisting("P60_TypedNullableArithmeticFilter_Full", "K48_TypedNullableArithmeticFilter.cs"),
        ProfiledFromExisting("P61_TypedVolatileReceiverLifetime_Full", "K49_TypedVolatileReceiverLifetime.cs"),
        ProfiledFromExisting("P62_TypedNativeRecordProjection_Full", "K50_TypedNativeRecordProjection.cs"),
        ProfiledFromExisting("P63_TypedNativeRecordMembersThroughCte_Full", "K51_TypedNativeRecordMembersThroughCte.cs"),
        ProfiledFromExisting("P64_TypedWideRangeJoinPartition_Full", "K52_TypedWideRangeJoinPartition.cs"),
        ProfiledFromExisting("P65_TypedWideWindowPartition_Full", "K53_TypedWideWindowPartition.cs"),
        ProfiledFromExisting("P66_TypedNullableWindowPartition_Full", "K54_TypedNullableWindowPartition.cs"),
        ProfiledFromExisting("P67_TypedPartialPayloadJson_Full", "K55_TypedPartialPayloadJson.cs"),
        ProfiledFromExisting("P68_TypedFixedArityConcat_Full", "K56_TypedFixedArityConcat.cs"),
        ProfiledFromExisting("P69_TypedNullableNestedMember_Full", "K57_TypedNullableNestedMember.cs"),
        ProfiledFromExisting("P70_TypedMixedNumericHashKeys_Full", "K58_TypedMixedNumericHashKeys.cs"),
        ProfiledFromExisting("P71_TypedNullableCompositeAsOf_Full", "K59_TypedNullableCompositeAsOf.cs"),
        ProfiledFromExisting("P72_NativeResultCompositeJoin_Full", "K60_NativeResultCompositeJoin.cs"),
        ProfiledFromExisting("P73_NativeSourceNullablePayload_Full", "K61_NativeSourceNullablePayload.cs"),
        ProfiledFromExisting("P74_NativeDictionaryNamedMembers_Full", "K62_NativeDictionaryNamedMembers.cs"),
        ProfiledFromExisting("P75_NativeGroupedResult_Full", "K63_NativeTypedGroupedDto.cs"),
        ProfiledFromExisting("P76_NativeApplyArgumentCarriers_Full", "K64_NativeApplyArgumentCarriers.cs"),
        ProfiledFromExisting("P77_NativeSpanVariadicCalls_Full", "K65_NativeSpanVariadicCalls.cs"),
        ProfiledFromExisting("P78_NativeNumericConversions_Full", "K66_NativeNumericConversions.cs"),
        ProfiledFromExisting("P79_NativeNestedGeneratedGenericCalls_Full", "K67_NativeNestedGeneratedGenericCalls.cs"),
        ProfiledFromExisting("P80_NativeGeneratedGenericResult_Full", "K68_NativeGeneratedGenericResult.cs"),
        ProfiledFromExisting("P81_CatalogOrdinalQualify_Full", "K69_CatalogOrdinalQualify.cs"),
        ProfiledFromExisting("P82_CatalogFusedRanking_Full", "K70_CatalogFusedRanking.cs"),
        ProfiledFromExisting("P83_NativeStructGenericArguments_Full", "K71_NativeStructGenericArguments.cs"),
        ProfiledFromExisting("P84_NativeStructGenericResult_Full", "K72_NativeStructGenericResult.cs"),
        ProfiledFromExisting("P85_NativeTupleResults_Full", "K73_NativeTupleResults.cs"),
        ProfiledFromExisting("P86_NativeInlineRecordArgument_Full", "K74_NativeInlineRecordArgument.cs"),
        ProfiledFromExisting("P87_NativeGenericResultDescription_Full", "K75_NativeGenericResultDescription.cs"),
        ProfiledFromExisting("P88_NativeJoinedFramesQualify_Full", "K76_NativeJoinedFramesQualify.cs"),
        ProfiledFromExisting("P89_NativeRecursiveFrames_Full", "K77_NativeRecursiveFrames.cs"),
        ProfiledFromExisting("P90_NativeSetAggregateWindow_Full", "K78_NativeSetAggregateWindow.cs"),
        ProfiledFromExisting("P91_NativeNumericRangeOffsets_Full", "K79_NativeNumericRangeOffsets.cs"),
        ProfiledFromExisting("P92_NativeFloatingDecimalComparison_Full", "K80_NativeFloatingDecimalComparison.cs"),
        ProfiledFromExisting("P93_NativeDynamicSqlMembership_Full", "K81_NativeDynamicSqlMembership.cs"),
        ProfiledFromExisting("P94_NativeDynamicLiteralContains_Full", "K82_NativeDynamicLiteralContains.cs"),
        ProfiledFromExisting("P95_NativeCaseDistinctCteGrouping_Full", "K83_NativeCaseDistinctCteGrouping.cs"),
        ProfiledFromExisting("P96_NativeOuterJoinConstantMembership_Full", "K84_NativeOuterJoinConstantMembership.cs"),
        ProfiledFromExisting("P97_NativeFilteredNullMembership_Full", "K85_NativeFilteredNullMembership.cs"),
        ProfiledFromExisting("P98_NativeAggregateMembershipInput_Full", "K86_NativeAggregateMembershipInput.cs"),
        ProfiledFromExisting("P99_NativeByteKernelState_Full", "K87_NativeByteKernelState.cs"),
        ProfiledFromExisting("P100_NativeSByteKernelState_Full", "K88_NativeSByteKernelState.cs"),
        ProfiledFromExisting("P101_NativeInt16KernelState_Full", "K89_NativeInt16KernelState.cs"),
        ProfiledFromExisting("P102_NativeUInt16KernelState_Full", "K90_NativeUInt16KernelState.cs"),
        ProfiledFromExisting("P103_NativeUInt32KernelState_Full", "K91_NativeUInt32KernelState.cs"),
        ProfiledFromExisting("P104_NativeInt64KernelState_Full", "K92_NativeInt64KernelState.cs"),
        ProfiledFromExisting("P105_NativeUInt64KernelState_Full", "K93_NativeUInt64KernelState.cs"),
        ProfiledFromExisting("P106_NativeSingleKernelState_Full", "K94_NativeSingleKernelState.cs")
    ];

    public static GeneratedCodeSample GetByFileName(string fileName)
    {
        return Samples.Single(sample => sample.FileName == fileName);
    }

    private static GeneratedCodeSample ProfiledBasic(
        string name,
        string query,
        QueryInstrumentationMode instrumentationMode)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "Profiled",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CreateSchemaProvider = GeneratedCodeSamplesCatalog.CreateBasicSchemaProvider,
            CompilationOptions = new CompilationOptions(instrumentationMode: instrumentationMode)
        };
    }

    private static GeneratedCodeSample ProfiledFromExisting(
        string name,
        string sourceFileName,
        QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Full)
    {
        var source = GeneratedCodeSamplesCatalog.GetByFileName(sourceFileName);

        return source with
        {
            Name = name,
            FileName = $"{name}.cs",
            Category = "Profiled",
            OutputType = null,
            CompilationOptions = source.CompilationOptions.WithInstrumentationMode(instrumentationMode)
        };
    }
}
