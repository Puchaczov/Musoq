using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly string[] StructuredInputSampleFileNames =
    [
        "Q330_StructuredPrimitiveArray.cs",
        "Q331_StructuredRecordArray.cs",
        "Q332_StructuredNestedRecord.cs",
        "Q333_StructuredNestedArrays.cs",
        "Q334_StructuredReorderedDefaults.cs",
        "Q335_StructuredInferredLetPresence.cs",
        "Q336_StructuredDeclaredLetDefaults.cs",
        "Q337_StructuredParameterDefaults.cs",
        "Q338_StructuredPrimitiveCteArgument.cs",
        "Q339_StructuredRecordCteArgument.cs",
        "Q340_StructuredSharedCteArgument.cs",
        "Q341_StructuredSortedCteArgument.cs",
        "Q342_StructuredDistinctCteArgument.cs",
        "Q343_StructuredGroupedCteArgument.cs",
        "Q344_StructuredSetCteArgument.cs",
        "Q345_StructuredPagedCteArgument.cs",
        "Q346_StructuredParallelCteArguments.cs",
        "Q347_StructuredCorrelatedInvocation.cs",
        "Q348_StructuredEmptyAndSuppressedDemand.cs",
        "Q349_StructuredCoupledSource.cs",
        "Q350_StructuredArgumentsDescription.cs",
        "Q351_StructuredMetadataOnlyDescription.cs",
        "Q352_StructuredWidePresence.cs",
        "Q353_StructuredValuesGroupedExpression.cs",
        "Q354_StructuredNumericOverload.cs",
        "Q355_StructuredStructuralOverload.cs",
        "Q356_StructuredDeclarationDefaultConflict.cs",
        "Q357_StructuredExplicitNullDescription.cs",
        "Q358_StructuredContextLifecycle.cs",
        "Q359_StructuredMutableOwnership.cs",
        "Q360_StructuredTypedParameterSnapshot.cs",
        "Q361_StructuredRecursiveCteArgument.cs",
        "Q362_StructuredParallelDirectCte.cs",
        "Q363_StructuredStrictReceiverDemand.cs",
        "Q364_StructuredFilteredCteArgument.cs",
        "Q365_StructuredValuesAndArrayInOneScript.cs",
        "Q366_StructuredOverloadInventory.cs",
        "Q367_StructuredEmptyCteArgument.cs",
        "Q368_StructuredPrimitiveEmptyContext.cs"
    ];

    [TestMethod]
    public void StructuredInputSampleCatalog_WhenCheckedIn_ShouldCoverEveryRequiredFamily()
    {
        var catalog = GeneratedCodeSamplesCatalog.Samples
            .Where(static sample => sample.FileName.StartsWith("Q3", StringComparison.Ordinal))
            .Select(static sample => sample.FileName)
            .Where(static fileName => StructuredInputSampleFileNames.Contains(fileName, StringComparer.Ordinal))
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(StructuredInputSampleFileNames, catalog);
    }

    [TestMethod]
    public void StructuredInputSamples_WhenGenerated_ShouldRetainInspectionSections()
    {
        foreach (var fileName in StructuredInputSampleFileNames)
        {
            var content = ReadSample(fileName).Content;
            var parsed = content.IndexOf("// === Parsed Query ===", StringComparison.Ordinal);
            var logical = content.IndexOf("// === Logical Plan ===", StringComparison.Ordinal);
            var physical = content.IndexOf("// === Physical Plan ===", StringComparison.Ordinal);
            var execution = content.IndexOf("// === Execution Plan ===", StringComparison.Ordinal);
            var generated = content.IndexOf("// === Generated C# ===", StringComparison.Ordinal);

            Assert.IsGreaterThanOrEqualTo(0, parsed, fileName);
            Assert.IsLessThan(logical, parsed, fileName);
            Assert.IsLessThan(physical, logical, fileName);
            Assert.IsLessThan(execution, physical, fileName);
            Assert.IsLessThan(generated, execution, fileName);
        }
    }

    [TestMethod]
    public void StructuredInputSamples_WhenGenerated_ShouldUseTypedConstructionAndCtePreparation()
    {
        var primitive = ReadSample("Q330_StructuredPrimitiveArray.cs").Content;
        Assert.Contains("NumbersSource", primitive);
        Assert.Contains("IReadOnlyList<int> __musoqStructural_n_0", primitive);
        Assert.Contains("new int[]", primitive);

        var records = ReadSample("Q331_StructuredRecordArray.cs").Content;
        Assert.Contains("PatternInput[]", records);
        Assert.Contains("new Musoq.Examples.DataSources.StructuredInputs.PatternInput", records);

        var nested = ReadSample("Q332_StructuredNestedRecord.cs").Content;
        Assert.Contains("new Musoq.Examples.DataSources.StructuredInputs.OptionsInput", nested);
        Assert.Contains("new Musoq.Examples.DataSources.StructuredInputs.WindowInput", nested);

        var matrix = ReadSample("Q333_StructuredNestedArrays.cs").Content;
        Assert.Contains("new IReadOnlyList<int>[]", matrix);
        Assert.Contains("new int[]", matrix);

        var reordered = ReadSample("Q334_StructuredReorderedDefaults.cs").Content;
        Assert.Contains("WeightedInput[]", reordered);
        Assert.Contains("int __musoqStructural_w_4 = 20", reordered);
        Assert.Contains("decimal __musoqStructural_w_3 = 2.5m", reordered);
        Assert.Contains(
            "WeightedInput(__musoqStructural_w_4, __musoqStructural_w_3, __musoqStructural_w_5)",
            reordered);

        var inferred = ReadSample("Q335_StructuredInferredLetPresence.cs").Content;
        Assert.Contains("__musoqStructuralCarrier_", inferred);
        Assert.Contains("__musoqStructuralAdapt_", inferred);
        Assert.DoesNotContain("StructuralValue.FromRecord", inferred);
        Assert.DoesNotContain("StructuralInputRuntime", inferred);

        var declared = ReadSample("Q336_StructuredDeclaredLetDefaults.cs").Content;
        Assert.Contains("__musoqStructuralCarrier_", declared);
        Assert.Contains("__musoqStructuralAdapt_", declared);
        Assert.Contains(", \"regex\",", declared);
        Assert.DoesNotContain("StructuralInputRuntime", declared);

        var parameter = ReadSample("Q337_StructuredParameterDefaults.cs").Content;
        Assert.Contains("ScriptParameterContract(\"patterns\"", parameter);
        Assert.Contains("CaptureParameterSnapshot", parameter);
        Assert.Contains("__musoqCaptureStructuralParameter_", parameter);
        Assert.Contains("ReserveMetrics(new StructuralInputMetrics", parameter);
        Assert.Contains("EnsureCollectionLowerBound", parameter);
        Assert.Contains("GetCollectionCount<", parameter);
        Assert.Contains("ReadTypedScalar", parameter);
        Assert.Contains("IReadOnlyList<object?> referenceList", parameter);
        Assert.DoesNotContain("GetCollectionElement(", parameter);
        Assert.DoesNotContain("Array.GetValue", parameter);
        Assert.DoesNotContain("PropertyInfo.GetValue", parameter);
        Assert.DoesNotContain("StructuralInputRuntime", parameter);

        var primitiveCte = ReadSample("Q338_StructuredPrimitiveCteArgument.cs").Content;
        Assert.Contains("__musoqPrepareCte_", primitiveCte);
        Assert.Contains("for (var index = 0; index < rows.Count; index++)", primitiveCte);
        Assert.Contains("__musoqStructuralNodes", primitiveCte);
        Assert.DoesNotContain("__musoqCheckStructural_", primitiveCte);

        var recordCte = ReadSample("Q339_StructuredRecordCteArgument.cs").Content;
        Assert.Contains("__musoqPrepareCte_", recordCte);
        Assert.Contains("ownership=ConstructFresh", recordCte);
        Assert.Contains("__musoqStructuralNodes", recordCte);
        Assert.DoesNotContain("__musoqCheckStructural_", recordCte);

        var sharedCte = ReadSample("Q340_StructuredSharedCteArgument.cs").Content;
        Assert.Contains("__musoqPrepareCte_", sharedCte);
        Assert.DoesNotContain("PrepareRows", sharedCte);
        Assert.DoesNotContain("row[0]", sharedCte);
        Assert.DoesNotContain("row[1]", sharedCte);

        var parallel = ReadSample("Q346_StructuredParallelCteArguments.cs").Content;
        Assert.Contains("Parallel.Invoke", parallel);
        Assert.Contains("CteLevel0Runner", parallel);
        Assert.Contains("__musoqPrepareCte_", parallel);
        Assert.Contains("ownership=ConstructFresh", parallel);

        foreach (var fileName in StructuredInputSampleFileNames.Where(static fileName =>
                     fileName.StartsWith("Q338_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q339_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q340_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q341_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q342_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q343_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q344_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q345_", StringComparison.Ordinal) ||
                     fileName.StartsWith("Q346_", StringComparison.Ordinal)))
        {
            var content = ReadSample(fileName).Content;
            Assert.DoesNotContain("StructuralInputRuntime", content, fileName);
            Assert.DoesNotContain("PrepareRows", content, fileName);
            Assert.DoesNotContain("Func<", content, fileName);
            Assert.DoesNotContain("Array.GetValue", content, fileName);
            Assert.DoesNotContain("=> row[", content, fileName);
        }

        foreach (var fileName in StructuredInputSampleFileNames.Where(static fileName =>
                     !fileName.StartsWith("Q35", StringComparison.Ordinal)))
        {
            var content = ReadSample(fileName).Content;
            Assert.DoesNotContain("GetRawConstructors", content, fileName);
            Assert.DoesNotContain("new object[]", content, fileName);
        }
    }

    [TestMethod]
    public void StructuredInputDescriptionAndWideSamples_WhenGenerated_ShouldStayMetadataOnlyAndReferenceable()
    {
        var description = ReadSample("Q350_StructuredArgumentsDescription.cs").Content;
        Assert.Contains("IMetadataOnlyRunnable", description);
        Assert.Contains("GetStructuralArgumentDescriptions", description);
        Assert.DoesNotContain("OpenTypedRowSource<", description);
        Assert.DoesNotContain("foreach", description);

        var queryDescription = ReadSample("Q351_StructuredMetadataOnlyDescription.cs").Content;
        Assert.Contains("GetQueryDescription", queryDescription);
        Assert.DoesNotContain("OpenTypedRowSource<", queryDescription);

        var wide = ReadSample("Q352_StructuredWidePresence.cs").Content;
        Assert.Contains("__musoqStructuralCarrier_", wide);
        Assert.Contains("__musoqStructuralAdapt_", wide);
        Assert.Contains("public readonly ulong P0", wide);
        Assert.Contains("public readonly ulong P1", wide);
        Assert.Contains("StructuredWideInput(", wide);
        Assert.Contains("source[index].P1", wide);
        Assert.DoesNotContain("StructuralValue.FromRecord", wide);
        var values = ReadSample("Q353_StructuredValuesGroupedExpression.cs").Content;
        Assert.Contains("(1 + 2) * 3", values);
        Assert.Contains("Label: ')'", values);
        Assert.Contains("Values", values);

        var numericOverload = ReadSample("Q354_StructuredNumericOverload.cs").Content;
        Assert.Contains("OverloadSource", numericOverload);
        Assert.Contains("42.5m", numericOverload);
        Assert.DoesNotContain("new Musoq.Examples.DataSources.StructuredInputs.OverloadSource(42,", numericOverload);

        var structuralOverload = ReadSample("Q355_StructuredStructuralOverload.cs").Content;
        Assert.Contains("OverloadSource", structuralOverload);
        Assert.Contains("PatternInput", structuralOverload);

        var defaultConflict = ReadSample("Q356_StructuredDeclarationDefaultConflict.cs").Content;
        Assert.Contains("DefaultConflictSource", defaultConflict);
        Assert.Contains("\"regex\"", defaultConflict);

        var explicitNull = ReadSample("Q357_StructuredExplicitNullDescription.cs").Content;
        Assert.Contains("GetStructuralArgumentDescriptions", explicitNull);
        Assert.Contains("null", explicitNull);

        var context = ReadSample("Q358_StructuredContextLifecycle.cs").Content;
        Assert.Contains("ContextProbeSource", context);
        Assert.Contains("SourceExecutionContext", context);

        var mutable = ReadSample("Q359_StructuredMutableOwnership.cs").Content;
        Assert.Contains("MutableProbeSource", mutable);
        Assert.Contains("new Musoq.Examples.DataSources.StructuredInputs.MutableInput", mutable);

        var parameterSnapshot = ReadSample("Q360_StructuredTypedParameterSnapshot.cs").Content;
        Assert.Contains("CaptureParameterSnapshot", parameterSnapshot);
        Assert.Contains("__musoqCaptureStructuralParameter_", parameterSnapshot);
        Assert.Contains("GetCollectionCount<", parameterSnapshot);
        Assert.DoesNotContain("GetCollectionElement(", parameterSnapshot);
        Assert.DoesNotContain("Array.GetValue", parameterSnapshot);

        var recursive = ReadSample("Q361_StructuredRecursiveCteArgument.cs").Content;
        Assert.Contains("RecursiveCte", recursive);
        Assert.Contains("__musoqPrepareCte_", recursive);

        var directParallel = ReadSample("Q362_StructuredParallelDirectCte.cs").Content;
        Assert.Contains("Parallel.Invoke", directParallel);
        Assert.Contains("MatchSource", directParallel);

        var strict = ReadSample("Q363_StructuredStrictReceiverDemand.cs").Content;
        Assert.Contains("StrictLimitSource", strict);
        Assert.Contains("IReadOnlyList<int>", strict);

        var cteOrder = ReadSample("Q364_StructuredFilteredCteArgument.cs").Content;
        Assert.Contains("__musoqPrepareCte_", cteOrder);
        Assert.Contains("p.Value > 1", cteOrder);

        var mixedValues = ReadSample("Q365_StructuredValuesAndArrayInOneScript.cs").Content;
        Assert.Contains("new int[]", mixedValues);
        Assert.Contains("Values", mixedValues);

        var inventory = ReadSample("Q366_StructuredOverloadInventory.cs").Content;
        Assert.Contains("GetStructuralArgumentDescriptions", inventory);
        Assert.Contains("Overload", inventory);

        var emptyCte = ReadSample("Q367_StructuredEmptyCteArgument.cs").Content;
        Assert.Contains("__musoqPrepareCte_", emptyCte);
        Assert.Contains("Array.Empty", emptyCte);

        var emptyDescription = ReadSample("Q368_StructuredPrimitiveEmptyContext.cs").Content;
        Assert.Contains("GetStructuralArgumentDescriptions", emptyDescription);
    }
}
