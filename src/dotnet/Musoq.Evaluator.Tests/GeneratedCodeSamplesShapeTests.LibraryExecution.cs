using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void PermissionSamples_ShouldExposeAuthorizedTypedExecutionAndSerialDefaults()
    {
        var count = ReadSample("K33_AuthorizedTypedCountMerge.cs").Content;
        var customSerial = ReadSample("K34_CustomCountSerialDefault.cs").Content;
        var checkedSum = ReadSample("K35_CheckedSumWithoutMergeEquivalence.cs").Content;
        var scalar = ReadSample("K36_AuthorizedScalarProjection.cs").Content;
        var sourceSerial = ReadSample("K37_SerialDatasourceProjection.cs").Content;
        Assert.Contains("ParallelSingleKeyAggregateChunkWorker_", count);
        Assert.Contains("CountNullableAggregateKernel<int>.Set(ref", count);
        Assert.Contains("CountNullableAggregateKernel<int>.Merge(ref", count);
        Assert.DoesNotContain("ParallelSingleKeyAggregateChunkWorker_", customSerial);
        Assert.DoesNotContain("ParallelSingleKeyAggregateChunkWorker_", checkedSum);
        Assert.Contains("checked(", checkedSum);
        Assert.Contains("ProjectChunkedRowsParallel<", scalar);
        Assert.DoesNotContain("ProjectChunkedRowsParallel<", sourceSerial);
        Assert.DoesNotContain("MethodInfo.Invoke", count);
    }

    [TestMethod]
    public void AggregateSamples_WhenInputAndResultDiffer_ShouldUseClosedKernelsAndResetOwnedState()
    {
        var average = ReadSample("K17_IntegralDecimalAverage.cs").Content;
        var custom = ReadSample("K18_CustomKernelReset.cs").Content;
        var generic = ReadSample("K19_ClosedGenericCustomAggregate.cs").Content;

        Assert.Contains("AverageKernel<int, decimal>.Set", average);
        Assert.Contains("AverageDistinctKernel<int, decimal>.Get", average);
        Assert.Contains("AverageKernel<int, decimal>.Reset", average);
        Assert.Contains("SeededTotalKernel.Reset", custom);
        Assert.Contains("GenericTotalKernel<int>.Set", generic);
        Assert.Contains("GenericTotalKernel<decimal>.Set", generic);
        Assert.DoesNotContain("MethodInfo.Invoke", generic);
    }
    [TestMethod]
    public void LibrarySamples_WhenNumericTypesDiffer_ShouldUseConcreteAggregateKernels()
    {
        var content = ReadSample("K01_LibraryMixedNumericGroups.cs").Content;

        Assert.Contains("SumAggregateKernel<decimal>", content);
        Assert.Contains("SumAggregateKernel<double>", content);
        Assert.Contains("SumAggregateKernel<int>", content);
    }

    [TestMethod]
    public void LibrarySamples_WhenCustomOperationsAreBound_ShouldEmitDirectKernelCalls()
    {
        var content = ReadSample("K02_LibraryCustomMultiArgumentGroups.cs").Content;

        Assert.Contains("PositiveTotalKernel.Set", content);
        Assert.Contains("ScaledTotalKernel.Set", content);
        Assert.DoesNotContain("AggregateFunction.NotInvoked", content);
        Assert.DoesNotContain("MethodInfo.Invoke", content);
    }

    [TestMethod]
    public void BoundarySamples_WhenStructuredValuesAreDecoded_ShouldReadConcreteJsonValuesAndVisitTypedColumns()
    {
        var content = ReadSample("K09_TypedNestedJsonParameter.cs").Content;

        Assert.Contains("value.GetInt32()", content);
        Assert.Contains("value.GetBoolean()", content);
        Assert.Contains("TypedColumnVisit.Apply", content);
        Assert.Contains("TypedParameters.GetRequired<System.Text.Json.JsonElement>", content);
        Assert.DoesNotContain("Deserialize<object", content);
        Assert.DoesNotContain("Deserialize<StructuralValue", content);
        Assert.DoesNotContain("MethodInfo.Invoke", content);
    }
    [TestMethod]
    public void RelationalSamples_WhenSourceContextsAreNullable_ShouldStoreConcreteContextFields()
    {
        var content = ReadSample("K14_TypedValueSourceContexts.cs").Content;

        Assert.Contains("LibraryExecutionValueEntity __leftContext", content);
        Assert.Contains("LibraryExecutionValueEntity? __rightContext", content);
        Assert.DoesNotContain("readonly object __", content);
        Assert.DoesNotContain("object __leftContext", content);
        Assert.DoesNotContain("(object)a", content);
        Assert.DoesNotContain("(object)b", content);
    }

    [TestMethod]
    public void ScalarSamples_WhenNumericValuesArePassed_ShouldBindClosedTypedDeclarations()
    {
        var content = ReadSample("K15_TypedNumericScalarTypeName.cs").Content;

        Assert.Contains("GetTypeName<int>(position)", content);
        Assert.Contains("GetTypeName<int>((ko3iko.Integral ?? 0))", content);
        Assert.DoesNotContain("GetTypeName<int?>", content);
    }

    [TestMethod]
    public void IndexedSamples_WhenCollectionsAreConcrete_ShouldCallTypedOperations()
    {
        var content = ReadSample("K16_TypedIndexedValues.cs").Content;

        Assert.Contains("GetTypedReadOnlyListElement<int[], int>", content);
        Assert.Contains("GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>", content);
        Assert.DoesNotContain("GetIndexedElement(", content);
        Assert.DoesNotContain("MethodInfo.Invoke", content);
    }

    [TestMethod]
    public void RangeJoinSamples_WithWidePartitions_ShouldUseNestedTypedTuples()
    {
        var content = ReadSample("K52_TypedWideRangeJoinPartition.cs").Content;
        Assert.Contains("ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?", content);
        Assert.Contains("CreateRangeJoinIndex<", content);
        Assert.DoesNotContain("CompositeKey(", content);
        Assert.DoesNotContain("CreateNullableHashJoinKey(", content);
    }

    [TestMethod]
    public void WindowPartitionSamples_WhenKeysAreWideOrNullable_ShouldUseConcreteBuilders()
    {
        var wide = ReadSample("K53_TypedWideWindowPartition.cs").Content;
        var nullable = ReadSample("K54_TypedNullableWindowPartition.cs").Content;
        Assert.Contains("WindowPartitionBuilder<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>>", wide);
        Assert.Contains("WindowPartitionBuilder<int?>", nullable);
        Assert.DoesNotContain("CompositeKey(", wide);
        Assert.DoesNotContain("WindowPartitionBuilder<object>", nullable);
    }

    [TestMethod]
    public void PartialPayloadSamples_ShouldCloseScalarCallsOverTheConcreteGeneratedPayload()
    {
        var content = ReadSample("K55_TypedPartialPayloadJson.cs").Content;
        Assert.Contains("ToJson<Musoq.Generated.Interpreters.Packet>", content);
        Assert.Contains("JsonPayloadWriter.Write(writer, in fieldValue, options)", content);
        Assert.Contains("IsFieldComplete(\"Currency\")", content);
        Assert.DoesNotContain("ToJson<object>", content);
        Assert.DoesNotContain("GetValue(\"Currency\")", content);
    }

    [TestMethod]
    public void ConcatSamples_ShouldUseFixedConcreteArgumentsWithoutInvocationArrays()
    {
        var content = ReadSample("K56_TypedFixedArityConcat.cs").Content;
        Assert.Contains("Concat<string, int?>(label, value)", content);
        Assert.Contains("Concat<string, string, int?, string>(label, \":\", value, \"!\")", content);
        Assert.DoesNotContain("new object[]", content);
        Assert.DoesNotContain("Concat<object>", content);
    }

    [TestMethod]
    public void NullableNestedMemberSamples_ShouldReadTheParentOnceAndPreserveNull()
    {
        var content = ReadSample("K57_TypedNullableNestedMember.cs").Content;
        Assert.Contains("Child?.Value", content);
        Assert.DoesNotContain("GetNestedValue", content);
        Assert.DoesNotContain("GetIndexedElement", content);
    }

    [TestMethod]
    public void MixedNumericKeySamples_ShouldRetainConcreteNullableOperands()
    {
        var content = ReadSample("K58_TypedMixedNumericHashKeys.cs").Content;
        Assert.Contains("int? intKey", content);
        Assert.Contains("long? longKey", content);
        Assert.DoesNotContain("CreateNullableHashJoinKey", content);
        Assert.DoesNotContain("new object[]", content);
    }

    [TestMethod]
    public void AsOfSamples_ShouldUseNativeCompositeKeysWithoutErasedSelectors()
    {
        var content = ReadSample("K59_TypedNullableCompositeAsOf.cs").Content;
        Assert.Contains("NativeEqualityKey<ValueTuple<int, string>>", content);
        Assert.DoesNotContain("CreateAsOfEqualityKey", content);
        Assert.DoesNotContain("(object)", content);
        Assert.DoesNotContain("new object[]", content);
    }

    [TestMethod]
    public void NativeRowSamples_ShouldExposeTypedConsumptionAndNullablePayloadAccess()
    {
        var joined = ReadSample("K60_NativeResultCompositeJoin.cs").Content;
        var payload = ReadSample("K61_NativeSourceNullablePayload.cs").Content;
        foreach (var content in new[] { joined, payload })
        {
            Assert.Contains("public override T GetValue<T>(int columnIndex)", content);
            Assert.Contains("public override void VisitColumns<TVisitor>", content);
            Assert.DoesNotContain("public override object this[", content);
            Assert.DoesNotContain("GetValue<object>", content);
        }
        Assert.Contains("p.Field0?.Value", payload);
        Assert.Contains("reader.Read<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload>", payload);
    }
}
