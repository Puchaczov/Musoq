using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CompilationOptionsTests
{
    [TestMethod]
    public void Constructor_Defaults_ShouldUseCloudCtePosture()
    {
        var options = new CompilationOptions();

        Assert.AreEqual(ParallelizationMode.Full, options.ParallelizationMode);
        Assert.IsTrue(options.UseHashJoin);
        Assert.IsTrue(options.UseSortMergeJoin);
        Assert.IsTrue(options.UseCommonSubexpressionElimination);
        Assert.IsTrue(options.UseConstantFolding);
        Assert.IsTrue(options.UsePrimitiveTypeValidation);
        Assert.IsTrue(options.UseCteParallelization);
        Assert.IsTrue(options.UseCteSidecarIndexes);
        Assert.AreEqual(1_000, options.RecursiveCteLimits.MaxIterations);
        Assert.AreEqual(10_000_000, options.RecursiveCteLimits.MaxRows);
        Assert.AreEqual(10_000_000, options.RecursiveCteLimits.MaxSnapshotRows);
    }

    [TestMethod]
    public void WithInstrumentationMode_WhenTableResultMaterializationIsForced_ShouldPreserveFlag()
    {
        var options = new CompilationOptions()
            .WithTableResultMaterialization()
            .WithInstrumentationMode(QueryInstrumentationMode.Full);

        Assert.IsTrue(options.ForceTableResultMaterialization);
        Assert.AreEqual(QueryInstrumentationMode.Full, options.InstrumentationMode);
        Assert.IsTrue(options.UseCteParallelization);
        Assert.IsTrue(options.UseCteSidecarIndexes);
    }

    [TestMethod]
    public void WithMethods_ShouldPreserveRecursiveCteLimits()
    {
        var limits = new RecursiveCteExecutionLimits(17, 123, 456);
        var options = new CompilationOptions()
            .WithRecursiveCteLimits(limits)
            .WithInstrumentationMode(QueryInstrumentationMode.Full)
            .WithTableResultMaterialization();

        Assert.AreSame(limits, options.RecursiveCteLimits);
        Assert.AreEqual(17, options.RecursiveCteLimits.MaxIterations);
        Assert.AreEqual(123, options.RecursiveCteLimits.MaxRows);
        Assert.AreEqual(456, options.RecursiveCteLimits.MaxSnapshotRows);
    }

    [TestMethod]
    public void WithMethods_ShouldPreserveEveryUnchangedCompilationOption()
    {
        var original = new CompilationOptions(
                ParallelizationMode.None,
                useHashJoin: false,
                useSortMergeJoin: false,
                useCommonSubexpressionElimination: false,
                useConstantFolding: false,
                usePrimitiveTypeValidation: false,
                useCteParallelization: false,
                useCteSidecarIndexes: false,
                instrumentationMode: QueryInstrumentationMode.SourceBoundaries,
                maxDegreeOfParallelismOverride: 3,
                forceTableResultMaterialization: false)
            .WithRecursiveCteLimits(new(17, 123));

        var clone = original
            .WithInstrumentationMode(QueryInstrumentationMode.Full)
            .WithTableResultMaterialization();

        Assert.AreEqual(original.ParallelizationMode, clone.ParallelizationMode);
        Assert.AreEqual(original.UseHashJoin, clone.UseHashJoin);
        Assert.AreEqual(original.UseSortMergeJoin, clone.UseSortMergeJoin);
        Assert.AreEqual(original.UseCommonSubexpressionElimination, clone.UseCommonSubexpressionElimination);
        Assert.AreEqual(original.UseConstantFolding, clone.UseConstantFolding);
        Assert.AreEqual(original.UsePrimitiveTypeValidation, clone.UsePrimitiveTypeValidation);
        Assert.AreEqual(original.UseCteParallelization, clone.UseCteParallelization);
        Assert.AreEqual(original.UseCteSidecarIndexes, clone.UseCteSidecarIndexes);
        Assert.AreSame(original.SourceRuntimeSettingsResolver, clone.SourceRuntimeSettingsResolver);
        Assert.AreEqual(original.MaxDegreeOfParallelismOverride, clone.MaxDegreeOfParallelismOverride);
        Assert.AreSame(original.RecursiveCteLimits, clone.RecursiveCteLimits);
        Assert.AreEqual(QueryInstrumentationMode.Full, clone.InstrumentationMode);
        Assert.IsTrue(clone.ForceTableResultMaterialization);
    }

    [TestMethod]
    public void CompilationOptionsFingerprint_ShouldBeStableAndSeparateBehavioralOptions()
    {
        var baseline = new CompilationOptions();
        var equivalent = baseline.WithTableResultMaterialization(false);
        var instrumented = baseline.WithInstrumentationMode(QueryInstrumentationMode.Full);
        var limited = baseline.WithRecursiveCteLimits(new(2, 3));
        var snapshotLimited = baseline.WithRecursiveCteLimits(new(1_000, 10_000_000, 3));

        Assert.AreEqual(
            CompilationOptionsFingerprint.Compute(baseline),
            CompilationOptionsFingerprint.Compute(equivalent));
        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(baseline),
            CompilationOptionsFingerprint.Compute(instrumented));
        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(baseline),
            CompilationOptionsFingerprint.Compute(limited));
        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(baseline),
            CompilationOptionsFingerprint.Compute(snapshotLimited));
        StringAssert.StartsWith(
            CompilationOptionsFingerprint.Compute(baseline),
            "compilation-options-v1:");
    }

    [TestMethod]
    public void RecursiveCteLimits_WhenNotPositive_ShouldRejectValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecursiveCteExecutionLimits(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecursiveCteExecutionLimits(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecursiveCteExecutionLimits(1, 1, 0));
    }
}
