using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
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
        Assert.AreEqual("string-only;null-propagating;left-to-right;eager-operands",
            contract.RequireRule(ExecutionSemanticsRuleId.SqlStringConcatenation));
        Assert.HasCount(14, contract.RuleDefinitions);
        Assert.AreEqual(64, contract.Fingerprint.Length);
        Assert.AreEqual("8A2F2DDB7040FE616FBAA27166D9BABC348A1310FB233A5A6C8CFAAB7525F4DB", contract.Fingerprint);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)contract.Rules)["integer.divide"] = "changed");
    }

    [TestMethod]
    public void Version2_ShouldDeclareLazyThreeValuedLogicWithoutChangingVersion1()
    {
        Assert.AreEqual(2, ExecutionSemanticsContract.Current.Version);
        Assert.AreEqual("sql-three-valued;left-to-right;short-circuit-and-or",
            ExecutionSemanticsContract.Version2.RequireRule(ExecutionSemanticsRuleId.NullLogic));
        Assert.AreNotEqual(ExecutionSemanticsContract.Version1.Fingerprint, ExecutionSemanticsContract.Version2.Fingerprint);
    }

    [TestMethod]
    public void CurrentContract_ShouldInvalidateArtifactsWithoutInterpretationExpressionSemantics()
    {
        var current = ExecutionSemanticsContract.Current;
        var previous = new ExecutionSemanticsContract(current.Version,
            current.RuleDefinitions.Where(rule => rule.Id != ExecutionSemanticsRuleId.InterpretationExpressions));
        Assert.AreNotEqual(previous.Fingerprint, current.Fingerprint);
        Assert.AreEqual("lossless-coalesce;lazy-three-valued-logic;checked-int32-cursor-domain;typed-parse-recovery;precise-partial-cursor;recursive-binary-values;direct-text-switch-values;payload-json-converters",
            current.RequireRule(ExecutionSemanticsRuleId.InterpretationExpressions));
    }

    [TestMethod]
    public void ExecutionPlan_ShouldDefaultToCurrentSemanticsAndPreserveExplicitContract()
    {
        var defaultPlan = new ExecutionPlan("Q_Default", [], new ExecutionBlock([]));
        var legacyContract = ExecutionSemanticsContract.Version1;
        var explicitPlan = new ExecutionPlan("Q_Explicit", [], new ExecutionBlock([]), semanticsContract: legacyContract);

        Assert.AreSame(ExecutionSemanticsContract.Version2, defaultPlan.SemanticsContract);
        Assert.AreSame(legacyContract, explicitPlan.SemanticsContract);
    }

    [TestMethod]
    public void CapabilitiesAndReadiness_ShouldCarryAndValidateSemanticsVersion()
    {
        var unsupportedContract = new ExecutionSemanticsContract(3, ExecutionSemanticsContract.Version2.Rules);
        var compatibility = new ExecutionTargetCompatibilityReport([]);
        var runtime = CreateEmptyRuntimeContract();

        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(ExecutionSemanticsContract.Version2).IsSupported);
        Assert.IsFalse(ExecutionTargetCapabilities.CSharpClr.Validate(ExecutionSemanticsContract.Version1).IsSupported);
        var unsupported = ExecutionTargetCapabilities.CSharpClr.Validate(unsupportedContract);
        Assert.IsFalse(unsupported.IsSupported);
        CollectionAssert.AreEqual(new[] { 3 }, unsupported.UnsupportedSemanticsVersions.ToArray());

        var readiness = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(compatibility, runtime, unsupportedContract);
        Assert.AreSame(unsupportedContract, readiness.SemanticsContract);
    }

    [TestMethod]
    public void Capabilities_WhenKnownSemanticsVersionHasDifferentRules_ShouldRejectFingerprint()
    {
        var changedRules = new Dictionary<string, string>(ExecutionSemanticsContract.Version2.Rules)
        {
            ["integer.divide"] = "target-specific"
        };
        var changedContract = new ExecutionSemanticsContract(2, changedRules);

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
