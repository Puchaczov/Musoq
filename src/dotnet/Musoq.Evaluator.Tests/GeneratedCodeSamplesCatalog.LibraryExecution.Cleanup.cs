using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateLibraryExecutionCleanupSamples() =>
    [
        new GeneratedCodeSample
        {
            Name = "K69_CatalogOrdinalQualify", FileName = "K69_CatalogOrdinalQualify.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, RowNumber() over (order by Value) as Ordinal from #schema.first() qualify Ordinal <= 0 order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionWindowQueryTests.NumericRow<int>, LibraryExecutionWindowQueryTests.ShiftedOrdinalLibrary>(
                [new(1, 1), new(2, 2), new(3, 3)])
        },
        new GeneratedCodeSample
        {
            Name = "K70_CatalogFusedRanking", FileName = "K70_CatalogFusedRanking.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, RowNumber() over (order by Value) as Ordinal, Rank() over (order by Value) as Rank from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionWindowQueryTests.NumericRow<int>, LibraryExecutionWindowQueryTests.ShiftedOrdinalLibrary>(
                [new(1, 1), new(2, 2), new(3, 2)])
        },
        new GeneratedCodeSample
        {
            Name = "K68_NativeGeneratedGenericResult", FileName = "K68_NativeGeneratedGenericResult.cs",
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false).WithStabilityAwareScalarReuse(),
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "param(items: (Value: int)[]) select NativeEcho($items) as Echo from #schema.first()",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionNativeGenericQueryTests.Input, LibraryExecutionNativeGenericQueryTests.GenericLibrary>([new(1)])
        },
        new GeneratedCodeSample
        {
            Name = "K67_NativeNestedGeneratedGenericCalls", FileName = "K67_NativeNestedGeneratedGenericCalls.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "param(items: (Value: int)[]) select NativeLength(NativeEcho($items)) as Length from #schema.first()",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionNativeGenericQueryTests.Input, LibraryExecutionNativeGenericQueryTests.GenericLibrary>([new(1)])
        },
        new GeneratedCodeSample
        {
            Name = "K66_NativeNumericConversions", FileName = "K66_NativeNumericConversions.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeConversionQueryTests.Query,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                new LibraryExecutionNativeConversionQueryTests.Input[]
                    { new(1, 1.5m), new(2, 2.5m), new(3, -1.5m), new(4, null) })
        },
        new GeneratedCodeSample
        {
            Name = "K65_NativeSpanVariadicCalls", FileName = "K65_NativeSpanVariadicCalls.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeVariadicQueryTests.Query,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<
                LibraryExecutionNativeVariadicQueryTests.Input, LibraryExecutionNativeVariadicQueryTests.SpanLibrary>(
                [new(1, 10), new(2, -20)])
        },
        new GeneratedCodeSample
        {
            Name = "K64_NativeApplyArgumentCarriers", FileName = "K64_NativeApplyArgumentCarriers.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeSourceArgumentQueryTests.ApplyQuery,
            CreateSchemaProvider = LibraryExecutionNativeSourceArgumentQueryTests.CreateProvider
        },
        new GeneratedCodeSample
        {
            Name = "K63_NativeTypedGroupedDto", FileName = "K63_NativeTypedGroupedDto.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeRowQueryTests.NativeTypedGroupingQuery,
            OutputType = typeof(LibraryExecutionNativeRowQueryTests.NativeSubtotal),
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(LibraryExecutionNativeRowQueryTests.Rows())
        },
        new GeneratedCodeSample
        {
            Name = "K62_NativeDictionaryNamedMembers", FileName = "K62_NativeDictionaryNamedMembers.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeRowQueryTests.NativeSignalQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionNativeRowQueryTests.NativeSignalRows())
        },
        new GeneratedCodeSample
        {
            Name = "K61_NativeSourceNullablePayload", FileName = "K61_NativeSourceNullablePayload.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeRowQueryTests.NativePayloadQuery,
            CreateSchemaProvider = LibraryExecutionNativeRowQueryTests.PayloadProvider
        },
        new GeneratedCodeSample
        {
            Name = "K60_NativeResultCompositeJoin", FileName = "K60_NativeResultCompositeJoin.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionNativeRowQueryTests.CompositeJoinQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionNativeRowQueryTests.Rows())
        },
        new GeneratedCodeSample
        {
            Name = "K59_TypedNullableCompositeAsOf", FileName = "K59_TypedNullableCompositeAsOf.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionAsOfQueryTests.CompositeQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionAsOfQueryTests.CreateRows())
        },
        new GeneratedCodeSample
        {
            Name = "K58_TypedMixedNumericHashKeys", FileName = "K58_TypedMixedNumericHashKeys.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = LibraryExecutionMixedKeyQueryTests.MixedKeyQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionMixedKeyQueryTests.CreateRows())
        },
        new GeneratedCodeSample
        {
            Name = "K57_TypedNullableNestedMember", FileName = "K57_TypedNullableNestedMember.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Child.Value as Value from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<Runtime.EvaluationHelperNestedValueTests.OuterEntity>(
                [new(1, new("resolved")), new(2, null)])
        },
        new GeneratedCodeSample
        {
            Name = "K56_TypedFixedArityConcat", FileName = "K56_TypedFixedArityConcat.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Concat(Label, Value) as Pair, Concat(Label, ':', Value) as Triple, " +
                "Concat(Label, ':', Value, '!') as Four from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionConcatQueryTests.ConcatInput>(
                [new(1, "A", 42), new(2, "B", null), new(3, null, 7)])
        },
        new GeneratedCodeSample
        {
            Name = "K55_TypedPartialPayloadJson", FileName = "K55_TypedPartialPayloadJson.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "binary Packet { Amount: int le, Currency: byte }; " +
                "select s.Position as Position, ToJson(p.ParsedFields) as Json from #schema.first() s " +
                "cross apply PartialInterpret<Packet>(s.Data) p order by s.Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionProviderQueryTests.PartialBytes>(
                [new(1, [42, 0, 0, 0, 7]), new(2, [42, 0, 0, 0])])
        },
        new GeneratedCodeSample
        {
            Name = "K53_TypedWideWindowPartition", FileName = "K53_TypedWideWindowPartition.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Sum(Amount) over " +
                "(partition by K1, K2, K3, K4, K5, K6, K7, K8, K9) as Total " +
                "from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionWideRangeKeyQueryTests.CreateRows())
        },
        new GeneratedCodeSample
        {
            Name = "K54_TypedNullableWindowPartition", FileName = "K54_TypedNullableWindowPartition.cs",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, Sum(Key) over (partition by NullableKey) as Total " +
                "from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionNullableSidecarKeyQueryTests.NullableKeyInput>(
                [new(1, 10, 10, "A"), new(2, 5, null, "A"), new(3, 20, 10, "B"), new(4, 7, null, "B")])
        },
        new GeneratedCodeSample
        {
            Name = "K52_TypedWideRangeJoinPartition",
            FileName = "K52_TypedWideRangeJoinPartition.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CompilationOptions = new CompilationOptions(useHashJoin: false, useSortMergeJoin: true),
            Query = LibraryExecutionWideRangeKeyQueryTests.WideRangeQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
                LibraryExecutionWideRangeKeyQueryTests.CreateRows())
        },
        new GeneratedCodeSample
        {
            Name = "K51_TypedNativeRecordMembersThroughCte",
            FileName = "K51_TypedNativeRecordMembersThroughCte.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
            Query = "with Records as (select (Position: Position, Detail: (Value: Value)) as Payload from #schema.first()) " +
                "select Payload.Position as Position, Payload.Detail.Value as Value from Records order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionBoundaryQueryTests.NativeRecordInput>(
                [new(1, 10), new(2, null)])
        },
        new GeneratedCodeSample
        {
            Name = "K50_TypedNativeRecordProjection",
            FileName = "K50_TypedNativeRecordProjection.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
            Query = LibraryExecutionBoundaryQueryTests.NativeRecordQuery,
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionBoundaryQueryTests.NativeRecordInput>(
                [new(1, 10), new(2, null)])
        },
        new GeneratedCodeSample
        {
            Name = "K49_TypedVolatileReceiverLifetime",
            FileName = "K49_TypedVolatileReceiverLifetime.cs",
            Category = "LibraryExecution",
            Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            Query = "select Position, NextSerialValue() as Tick from #schema.first() order by Position",
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider<LibraryExecutionWindowQueryTests.NumericRow<int>, LibraryExecutionConcurrencyQueryTests.ReceiverLifetimeLibrary>(
                [new(1, 10), new(2, 20), new(3, 30)])
        }
    ];
}
