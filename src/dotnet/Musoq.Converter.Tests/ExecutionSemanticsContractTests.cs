using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ExecutionSemanticsContractTests
{
    [TestMethod]
    public void Version1_ShouldDescribeOperationSpecificCurrentClrRules()
    {
        var contract = ExecutionSemanticsContract.Version1;

        Assert.AreEqual(1, contract.Version);
        Assert.AreEqual("sql-three-valued", contract.RequireRule("null.logic"));
        Assert.AreEqual("unchecked-width-wrap", contract.RequireRule("integer.runtime.add-subtract-multiply"));
        Assert.AreEqual("checked-diagnostic", contract.RequireRule("integer.constant-folding.add-subtract-multiply"));
        Assert.AreEqual("checked-overflow", contract.RequireRule("integer.aggregate.add-subtract-multiply"));
        Assert.AreEqual("truncate-toward-zero;divide-by-zero-error", contract.RequireRule("integer.divide"));
        Assert.AreEqual("dividend-sign;divide-by-zero-error", contract.RequireRule("integer.modulo"));
        Assert.AreEqual("ieee-754-clr", contract.RequireRule("floating-point"));
        Assert.AreEqual("clr-128-bit-decimal-checked", contract.RequireRule("decimal"));
        Assert.AreEqual("ordinal", contract.RequireRule("string.equality-ordering-hashing"));
        Assert.AreEqual("invariant-culture", contract.RequireRule("strict-cast"));
        Assert.AreEqual(
            "sql-three-valued",
            contract.RequireRule(ExecutionSemanticsRuleId.NullLogic));
        Assert.HasCount(13, contract.RuleDefinitions);
        Assert.AreEqual(64, contract.Fingerprint.Length);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)contract.Rules)["integer.divide"] = "changed");
    }

    [TestMethod]
    public void ExecutionPlan_ShouldDefaultToSemanticsVersionOneAndPreserveExplicitContract()
    {
        var defaultPlan = new ExecutionPlan("Q_Default", [], new ExecutionBlock([]));
        var version2 = new ExecutionSemanticsContract(2, ExecutionSemanticsContract.Version1.Rules);
        var explicitPlan = new ExecutionPlan("Q_Explicit", [], new ExecutionBlock([]), semanticsContract: version2);

        Assert.AreSame(ExecutionSemanticsContract.Version1, defaultPlan.SemanticsContract);
        Assert.AreSame(version2, explicitPlan.SemanticsContract);
    }

    [TestMethod]
    public void CapabilitiesAndReadiness_ShouldCarryAndValidateSemanticsVersion()
    {
        var version2 = new ExecutionSemanticsContract(2, ExecutionSemanticsContract.Version1.Rules);
        var compatibility = new ExecutionTargetCompatibilityReport([]);
        var runtime = CreateEmptyRuntimeContract();

        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(ExecutionSemanticsContract.Version1).IsSupported);
        var unsupported = ExecutionTargetCapabilities.CSharpClr.Validate(version2);
        Assert.IsFalse(unsupported.IsSupported);
        CollectionAssert.AreEqual(new[] { 2 }, unsupported.UnsupportedSemanticsVersions.ToArray());

        var readiness = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(compatibility, runtime, version2);
        Assert.AreSame(version2, readiness.SemanticsContract);
    }

    [TestMethod]
    public void Capabilities_WhenKnownSemanticsVersionHasDifferentRules_ShouldRejectFingerprint()
    {
        var changedRules = new Dictionary<string, string>(ExecutionSemanticsContract.Version1.Rules)
        {
            ["integer.divide"] = "target-specific"
        };
        var changedContract = new ExecutionSemanticsContract(1, changedRules);

        var validation = ExecutionTargetCapabilities.CSharpClr.Validate(changedContract);

        Assert.IsFalse(validation.IsSupported);
        Assert.IsEmpty(validation.UnsupportedSemanticsVersions);
        CollectionAssert.AreEqual(
            new[] { changedContract.Fingerprint },
            validation.UnsupportedSemanticsFingerprints.ToArray());
    }

    private static TargetRuntimeContract CreateEmptyRuntimeContract() =>
        new(
            "Q_Semantics",
            [],
            [],
            [],
            new TargetNullBehaviorContract(false, false, false, "none"),
            new TargetCancellationContract(false, false),
            new TargetDiagnosticsContract(false, false, false),
            new TargetProfilingContract(false, false, 0, 0));
}
