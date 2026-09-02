using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CompilationOptionsLoopInvariantTests
{
    [TestMethod]
    public void DefaultOptionsEnableLoopInvariantCodeMotion()
    {
        Assert.IsTrue(new CompilationOptions().UseLoopInvariantCodeMotion);
    }

    [TestMethod]
    public void WithLoopInvariantCodeMotionIsIndependentFromCse()
    {
        var options = new CompilationOptions(useCommonSubexpressionElimination: false)
            .WithLoopInvariantCodeMotion();

        Assert.IsTrue(options.UseLoopInvariantCodeMotion);
        Assert.IsFalse(options.UseCommonSubexpressionElimination);
    }

    [TestMethod]
    public void LoopInvariantCodeMotionParticipatesInCompilationFingerprint()
    {
        var enabled = new CompilationOptions();
        var disabled = enabled.WithLoopInvariantCodeMotion(false);

        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(disabled),
            CompilationOptionsFingerprint.Compute(enabled));
        Assert.IsFalse(disabled.UseLoopInvariantCodeMotion);
    }

    [TestMethod]
    public void StabilityAwareScalarReuseIsEnabledByDefaultAndIndependentFromLoopInvariantCodeMotion()
    {
        var defaults = new CompilationOptions();
        var disabled = defaults
            .WithStabilityAwareScalarReuse(false)
            .WithLoopInvariantCodeMotion(false);

        Assert.IsTrue(defaults.UseStabilityAwareScalarReuse);
        Assert.IsFalse(disabled.UseStabilityAwareScalarReuse);
        Assert.IsFalse(disabled.UseLoopInvariantCodeMotion);
        Assert.IsTrue(disabled.UseCommonSubexpressionElimination);
        Assert.AreNotEqual(
            CompilationOptionsFingerprint.Compute(defaults),
            CompilationOptionsFingerprint.Compute(disabled));
    }

    [TestMethod]
    public void ScalarReuseFingerprintSeparatesEveryOptimizationToggleCombination()
    {
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);

        foreach (var loopInvariant in new[] { false, true })
        foreach (var scalarReuse in new[] { false, true })
        foreach (var cse in new[] { false, true })
        {
            var options = new CompilationOptions(useCommonSubexpressionElimination: cse)
                .WithLoopInvariantCodeMotion(loopInvariant)
                .WithStabilityAwareScalarReuse(scalarReuse);

            Assert.IsTrue(fingerprints.Add(CompilationOptionsFingerprint.Compute(options)));
        }
    }
}
