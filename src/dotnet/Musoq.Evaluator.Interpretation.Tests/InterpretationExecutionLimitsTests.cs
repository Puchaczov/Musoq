using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretationExecutionLimitsTests
{
    [TestMethod]
    public void DefaultLimits_ShouldRetainTenThousandIterations()
    {
        Assert.AreEqual(10_000, new InterpretationExecutionLimits().MaxRepeatIterations);
        Assert.AreEqual(10_000, new CompilationOptions().InterpretationLimits.MaxRepeatIterations);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void NonPositiveLimits_ShouldFailAtConfigurationTime(int maximum)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new InterpretationExecutionLimits(maximum));

        Assert.AreEqual("maxRepeatIterations", error.ParamName);
    }

    [TestMethod]
    public void InterpretationLimits_ShouldAcceptTheFullPositiveIntRange()
    {
        Assert.AreEqual(int.MaxValue, new InterpretationExecutionLimits(int.MaxValue).MaxRepeatIterations);
    }

    [TestMethod]
    public void EveryOptionClone_ShouldPreserveTheImmutableInterpretationLimits()
    {
        var limits = new InterpretationExecutionLimits(17);
        var original = new CompilationOptions().WithInterpretationLimits(limits);
        var clone = original.WithInstrumentationMode(QueryInstrumentationMode.Disabled)
            .WithTableResultMaterialization().WithLoopInvariantCodeMotion(false)
            .WithStabilityAwareScalarReuse(false).WithRecursiveCteLimits(new(7, 11, 13))
            .WithRegexMatchTimeout(TimeSpan.FromSeconds(2));
        var replacement = clone.WithInterpretationLimits(new(19));

        Assert.AreSame(limits, clone.InterpretationLimits);
        Assert.AreEqual(17, original.InterpretationLimits.MaxRepeatIterations);
        Assert.AreEqual(19, replacement.InterpretationLimits.MaxRepeatIterations);
        Assert.AreEqual(7, replacement.RecursiveCteLimits.MaxIterations);
    }

    [TestMethod]
    public void NullLimits_ShouldFailAtThePublicOptionsBoundary()
    {
        Assert.Throws<ArgumentNullException>(() => new CompilationOptions().WithInterpretationLimits(null!));
    }

    [TestMethod]
    public void ConfiguredLimits_ShouldParticipateInCompilationIdentityByValue()
    {
        var first = new CompilationOptions().WithInterpretationLimits(new(17));
        var equivalent = new CompilationOptions().WithInterpretationLimits(new(17));
        var different = new CompilationOptions().WithInterpretationLimits(new(19));

        Assert.AreEqual(CompilationOptionsFingerprint.Compute(first), CompilationOptionsFingerprint.Compute(equivalent));
        Assert.AreNotEqual(CompilationOptionsFingerprint.Compute(first), CompilationOptionsFingerprint.Compute(different));
    }
}
