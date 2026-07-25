using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void ExecutionNodeTypes_ShouldHaveExplicitCoverageInventory()
    {
        string[] expectedNodeTypes =
        [
            "ExecutionAdaptExpando",
            "ExecutionAggregateCapturedValueSet",
            "ExecutionAggregateSet",
            "ExecutionAppendExistingRow",
            "ExecutionAppendRecord",
            "ExecutionAppendRow", "ExecutionArrayAssign",
            "ExecutionAsOfProbe",
            "ExecutionAssign",
            "ExecutionBreak",
            "ExecutionComputeOffsetWindow",
            "ExecutionComputePluginWindow",
            "ExecutionComputeRankingWindow",
            "ExecutionContinue",
            "ExecutionContinueIf",
            "ExecutionCreateAggregateContext",
            "ExecutionCreateAggregateLibrary",
            "ExecutionCreateAsOfIndex", "ExecutionCreateBooleanArray",
            "ExecutionCreateBoundedRecordList",
            "ExecutionCreateGeneratedRow", "ExecutionCreateHash", "ExecutionCreateHashPayload", "ExecutionCreateKeySet",
            "ExecutionCreateObject",
            "ExecutionCreateRangeIndex",
            "ExecutionCreateRecordList",
            "ExecutionCreateSingleKeyAggregateContext",
            "ExecutionCreateTable",
            "ExecutionCreateValueTupleAggregateContext",
            "ExecutionCreateValuesRows", "ExecutionCteFusedProducerCandidate", "ExecutionCteIndexOnlyStorageCandidate", "ExecutionCteReadOnceFusionCandidate", "ExecutionCteSidecarAppendRewriteCandidate", "ExecutionCteSidecarIndexBuildCandidate", "ExecutionCteSidecarIndexLoadCandidate", "ExecutionCteSidecarIndexStoreCandidate",
            "ExecutionDistinctTable",
            "ExecutionEnsureAggregateGroup",
            "ExecutionEnsureTableCapacity",
            "ExecutionEnumerableSource",
            "ExecutionForEach",
            "ExecutionForEachIndexed", "ExecutionForEachWithOrdinality", "ExecutionFusedCteProducer",
            "ExecutionGetOrAddSingleKeyAggregateGroup",
            "ExecutionGetOrAddValueTupleAggregateGroup",
            "ExecutionHashAdd",
            "ExecutionHashProbe",
            "ExecutionHoistCandidateLet", "ExecutionIf",
            "ExecutionInterpretSource", "ExecutionKeySetAdd", "ExecutionKeySetProbe",
            "ExecutionLet",
            "ExecutionLoadCteIndex",
            "ExecutionMaterializeExpandoList",
            "ExecutionMaterializeFilteredList",
            "ExecutionMaterializeList",
            "ExecutionMaterializeRecordListToTable",
            "ExecutionMethodTargetDeclarationCandidate",
            "ExecutionOrderRecordList",
            "ExecutionParallelBlock",
            "ExecutionParallelFilterProjectLoop",
            "ExecutionParallelSingleKeyAggregateLoop",
            "ExecutionProjectTable",
            "ExecutionRangeProbe",
            "ExecutionRecursiveCte",
            "ExecutionRecursiveCteAppend",
            "ExecutionRecursiveCteSnapshotRowGuard",
            "ExecutionRelatedCtePhase",
            "ExecutionReturnDesc",
            "ExecutionReturnTable",
            "ExecutionScopedBlock", "ExecutionSetOperation", "ExecutionSingleUsePipelineFusionCandidate", "ExecutionSkipTable",
            "ExecutionSliceTable",
            "ExecutionSortTable",
            "ExecutionSourceScan",
            "ExecutionStoreCteIndex",
            "ExecutionStoreTable",
            "ExecutionTakeTable",
            "ExecutionTopNTable",
            "ExecutionTopOffsetTable",
            "ExecutionWindowAggregateKernel",
            "ExecutionWindowKernelPlan"
        ];

        var actualNodeTypes = typeof(ExecutionNode).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(ExecutionNode)))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(
            expectedNodeTypes.SequenceEqual(actualNodeTypes, StringComparer.Ordinal),
            "ExecutionNode inventory changed. Expected: " +
            string.Join(", ", expectedNodeTypes) +
            ". Actual: " +
            string.Join(", ", actualNodeTypes));
    }

    [TestMethod]
    public void DistinctAggregateOverloads_ShouldKeepRuntimeV2AggregateAttributes()
    {
        var expectedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [nameof(LibraryBase.CountDistinct)] = 15,
            [nameof(LibraryBase.SumDistinct)] = 11,
            [nameof(LibraryBase.AvgDistinct)] = 11,
            [nameof(LibraryBase.MinDistinct)] = 11,
            [nameof(LibraryBase.MaxDistinct)] = 11
        };

        var methods = typeof(LibraryBase)
            .GetMethods()
            .Where(method => expectedCounts.ContainsKey(method.Name))
            .GroupBy(method => method.Name)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var (name, expectedCount) in expectedCounts)
        {
            Assert.IsTrue(methods.TryGetValue(name, out var overloads), $"Missing distinct aggregate family {name}.");
            Assert.IsNotNull(overloads);
            Assert.HasCount(expectedCount, overloads, $"Unexpected distinct aggregate overload count for {name}.");

            foreach (var overload in overloads)
            {
                Assert.IsNotNull(
                    overload.GetCustomAttributes(typeof(AggregateFunctionAttribute), inherit: false).SingleOrDefault(),
                    $"Missing AggregateFunctionAttribute on {overload}.");

                var parentParameter = overload.GetParameters().SingleOrDefault(parameter => parameter.Name == "parent");
                Assert.IsNotNull(parentParameter, $"Missing parent parameter on {overload}.");
                Assert.IsTrue(
                    parentParameter.GetCustomAttributes(typeof(AggregateParentAttribute), inherit: false).Length > 0,
                    $"Missing AggregateParentAttribute on {overload}.");
            }
        }
    }

    [TestMethod]
    public void GenericAndMathHelpers_ShouldKeepSelectedPublicOverloadSignatures()
    {
        AssertGenericLibraryMethod(nameof(LibraryBase.Skip), MethodCategories.Utility, 2);
        AssertGenericLibraryMethod(nameof(LibraryBase.Take), MethodCategories.Utility, 2);
        AssertGenericLibraryMethod(nameof(LibraryBase.NthFromEndOrDefault), MethodCategories.Utility, 2);
        AssertGenericLibraryMethod(nameof(LibraryBase.IfNull), MethodCategories.Utility, 2);
        AssertConcreteLibraryMethod(nameof(LibraryBase.Match), typeof(bool?), MethodCategories.Utility, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Coalesce), typeof(byte?), MethodCategories.Utility, typeof(byte?[]));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Coalesce), typeof(decimal?), MethodCategories.Utility, typeof(decimal?[]));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Abs), typeof(decimal?), MethodCategories.Math, typeof(decimal?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Abs), typeof(long?), MethodCategories.Math, typeof(long?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Abs), typeof(int?), MethodCategories.Math, typeof(int?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Clamp), typeof(int?), MethodCategories.Math, typeof(int?), typeof(int?), typeof(int?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Clamp), typeof(decimal?), MethodCategories.Math, typeof(decimal?), typeof(decimal?), typeof(decimal?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.IsBetweenExclusive), typeof(bool?), MethodCategories.Math, typeof(decimal?), typeof(decimal?), typeof(decimal?));
        AssertConcreteLibraryMethodSignature(nameof(LibraryBase.Sin), typeof(float?), typeof(float?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.LogBase), typeof(double?), MethodCategories.Math, typeof(double?), typeof(double?));

        var rand = AssertConcreteLibraryMethod(nameof(LibraryBase.Rand), typeof(int), MethodCategories.Math);
        var rangedRand = AssertConcreteLibraryMethod(nameof(LibraryBase.Rand), typeof(int?), MethodCategories.Math, typeof(int?), typeof(int?));
        Assert.IsNotNull(rand.GetCustomAttributes(typeof(NonDeterministicAttribute), inherit: false).SingleOrDefault());
        Assert.IsNotNull(rangedRand.GetCustomAttributes(typeof(NonDeterministicAttribute), inherit: false).SingleOrDefault());
    }

    [TestMethod]
    public void CompressionHelpers_ShouldKeepSelectedPublicOverloadSignatures()
    {
        foreach (var codec in new[] { "ZLib", "GZip", "Deflate", "Brotli" })
        {
            AssertConcreteLibraryMethod($"Decompress{codec}", typeof(string), MethodCategories.Compression, typeof(byte[]));
            AssertConcreteLibraryMethod($"Decompress{codec}", typeof(string), MethodCategories.Compression, typeof(byte[]), typeof(string));
            AssertConcreteLibraryMethod($"Decompress{codec}ToBytes", typeof(byte[]), MethodCategories.Compression, typeof(byte[]));
            AssertConcreteLibraryMethod($"Decompress{codec}FromBase64", typeof(string), MethodCategories.Compression, typeof(string));
            AssertConcreteLibraryMethod($"Decompress{codec}FromBase64", typeof(string), MethodCategories.Compression, typeof(string), typeof(string));
            AssertConcreteLibraryMethod($"Compress{codec}", typeof(byte[]), MethodCategories.Compression, typeof(string));
            AssertConcreteLibraryMethod($"Compress{codec}", typeof(byte[]), MethodCategories.Compression, typeof(byte[]));
            AssertConcreteLibraryMethod($"Compress{codec}ToBase64", typeof(string), MethodCategories.Compression, typeof(string));
        }
    }

    [TestMethod]
    public void StrictConversionHelpers_ShouldKeepSelectedPublicOverloadSignatures()
    {
        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt32Strict), typeof(int?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt64Strict), typeof(long?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToDecimalStrict), typeof(decimal?), typeof(object));

        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt32Comparison), typeof(int?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt64Comparison), typeof(long?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToDecimalComparison), typeof(decimal?), typeof(object));

        AssertBindableMethod(nameof(LibraryBase.TryConvertNumericOnly), typeof(decimal?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt32NumericOnly), typeof(int?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToInt64NumericOnly), typeof(long?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToDecimalNumericOnly), typeof(decimal?), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.TryConvertToDoubleNumericOnly), typeof(double?), typeof(object));

        AssertBindableMethod(nameof(LibraryBase.InternalApplyAddOperator), typeof(object), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalApplySubtractOperator), typeof(object), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalApplyMultiplyOperator), typeof(object), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalApplyDivideOperator), typeof(object), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalApplyModuloOperator), typeof(object), typeof(object), typeof(object));

        AssertBindableMethod(nameof(LibraryBase.InternalGreaterThanOperator), typeof(bool?), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalLessThanOperator), typeof(bool?), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalGreaterThanOrEqualOperator), typeof(bool?), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalLessThanOrEqualOperator), typeof(bool?), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalEqualOperator), typeof(bool?), typeof(object), typeof(object));
        AssertBindableMethod(nameof(LibraryBase.InternalNotEqualOperator), typeof(bool?), typeof(object), typeof(object));
    }

    [TestMethod]
    public void StringAndNetworkHelpers_ShouldKeepSelectedPublicOverloadSignatures()
    {
        AssertConcreteLibraryMethod(nameof(LibraryBase.Contains), typeof(bool?), MethodCategories.String, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.NthIndexOf), typeof(int?), MethodCategories.String, typeof(string), typeof(string), typeof(int));
        AssertConcreteLibraryMethod(nameof(LibraryBase.Soundex), typeof(string), MethodCategories.String, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.HasFuzzyMatchedWord), typeof(bool), MethodCategories.String, typeof(string), typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.LevenshteinDistance), typeof(int?), MethodCategories.String, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.StartsWith), typeof(bool?), MethodCategories.String, typeof(string), typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.IsAlphaNumeric), typeof(bool?), MethodCategories.String, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.CountOccurrences), typeof(int?), MethodCategories.String, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.UrlEncode), typeof(string), MethodCategories.String, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.RegexExtract), typeof(string), MethodCategories.String, typeof(string), typeof(string), typeof(int));
        AssertConcreteLibraryMethod(nameof(LibraryBase.SplitByNewLines), typeof(string[]), MethodCategories.String, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.ToUnicodeEscape), typeof(string), MethodCategories.String, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.FromBinaryString), typeof(string), MethodCategories.String, typeof(string));

        AssertConcreteLibraryMethod(nameof(LibraryBase.IsPrivateIp), typeof(bool?), MethodCategories.Network, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.IpToLong), typeof(long?), MethodCategories.Network, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.LongToIp), typeof(string), MethodCategories.Network, typeof(long?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.IsInSubnet), typeof(bool?), MethodCategories.Network, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.FormatMac), typeof(string), MethodCategories.Network, typeof(string), typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.NewGuid), typeof(string), MethodCategories.Network);
        AssertConcreteLibraryMethod(nameof(LibraryBase.ConvertBase), typeof(string), MethodCategories.Network, typeof(string), typeof(int), typeof(int));
        AssertConcreteLibraryMethod(nameof(LibraryBase.UnixToDateTime), typeof(DateTime?), MethodCategories.Network, typeof(long?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.DateTimeOffsetToUnixMillis), typeof(long?), MethodCategories.Network, typeof(DateTimeOffset?));
        AssertConcreteLibraryMethod(nameof(LibraryBase.ToSlug), typeof(string), MethodCategories.Network, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.EscapeRegex), typeof(string), MethodCategories.Network, typeof(string));
        AssertConcreteLibraryMethod(nameof(LibraryBase.ExtractUrls), typeof(string), MethodCategories.Network, typeof(string));
    }

    [TestMethod]
    public void MethodDeclarationHelper_ShouldKeepPublicStaticHelperInventory()
    {
        string[] expectedSignatures =
        [
            "CreateContextRunMethodWithBody(BlockSyntax):MethodDeclarationSyntax",
            "CreateDataSourceProgressEvent():EventFieldDeclarationSyntax",
            "CreateOnDataSourceProgressMethod():MethodDeclarationSyntax",
            "CreateOnPhaseChangedMethod():MethodDeclarationSyntax",
            "CreatePhaseChangedEvent():EventFieldDeclarationSyntax",
            "CreatePublicProperty(String,String):PropertyDeclarationSyntax",
            "CreateRunMethod(String):MethodDeclarationSyntax",
            "CreateRunMethodWithBody(BlockSyntax):MethodDeclarationSyntax",
            "CreateSourceExecutionPlansProperty():PropertyDeclarationSyntax",
            "CreateSourceRuntimeSettingDescriptionsBySourceContextIdProperty():PropertyDeclarationSyntax",
            "CreateSourceRuntimeSettingsBySourceContextIdProperty():PropertyDeclarationSyntax",
            "CreateStandardParameterList():ParameterListSyntax",
            "CreateStandardPrivateMethod(String,BlockSyntax):MethodDeclarationSyntax"
        ];

        var actualSignatures = typeof(MethodDeclarationHelper)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(static method =>
            {
                var parameterList = string.Join(
                    ",",
                    method.GetParameters().Select(static parameter => parameter.ParameterType.Name));

                return $"{method.Name}({parameterList}):{method.ReturnType.Name}";
            })
            .OrderBy(static signature => signature, StringComparer.Ordinal)
            .ToArray();

        Assert.IsTrue(
            expectedSignatures.SequenceEqual(actualSignatures, StringComparer.Ordinal),
            "MethodDeclarationHelper public helper inventory changed. Expected: " +
            string.Join(", ", expectedSignatures) +
            ". Actual: " +
            string.Join(", ", actualSignatures));
    }
}
