using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Execution.Analysis;
using Musoq.Targets.TestPortable;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class StringMatchTargetContractTests
{
    [TestMethod]
    public void StringMatch_CSharpShouldAdvertiseSupportAndPortableSubsetShouldReject()
    {
        var match = new ExecutionStringMatch(
            new ExecutionLiteral("Google", typeof(string)),
            "Google%",
            "Google",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));
        var plan = new ExecutionPlan(
            "Q_StringMatchTargetContract",
            [],
            new ExecutionBlock([
                new ExecutionLet(new ExecutionVariable("matched", typeof(bool)), match)
            ]));
        var operations = ExecutionTargetOperationAnalyzer.Analyze(plan);
        var features = ExecutionTargetFeatureAnalyzer.Analyze(plan);

        Assert.AreEqual(7, plan.ExecutionIrVersion);
        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(operations).IsSupported);
        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(features).IsSupported);

        var portableOperationValidation = PortableSubsetTarget.Capabilities.Validate(operations);
        var portableFeatureValidation = PortableSubsetTarget.Capabilities.Validate(features);
        Assert.IsFalse(portableOperationValidation.IsSupported);
        Assert.AreEqual(
            "expr.string-match",
            portableOperationValidation.UnsupportedOperations.Single().Value);
        Assert.IsFalse(portableFeatureValidation.IsSupported);
        Assert.IsTrue(portableFeatureValidation.UnsupportedFeatures.Any(feature =>
            feature.Kind == ExecutionTargetFeatureKind.StringMatchKind));
        Assert.IsTrue(portableFeatureValidation.UnsupportedFeatures.Any(feature =>
            feature.Kind == ExecutionTargetFeatureKind.StringMatchComparison));
    }

    [TestMethod]
    public void LikeMatcherOperations_CSharpShouldAcceptAndPortableSubsetShouldRejectBeforeRendering()
    {
        var matcherType = ExecutionClrBindingFactory.FromClr(typeof(PreparedLikeMatcher));
        var cacheType = ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));
        var matcher = new ExecutionVariable("matcher", matcherType);
        var cache = new ExecutionVariable("cache", cacheType);
        var plan = new ExecutionPlan(
            "Q_LikeMatcherTargetContract",
            [],
            new ExecutionBlock([
                new ExecutionLet(cache, new ExecutionLikeMatcherCacheSlot(cacheType)),
                new ExecutionLet(
                    matcher,
                    new ExecutionPrepareLikeMatcher(
                        new ExecutionLiteral("a%", typeof(string)),
                        ExecutionStringMatchComparison.LikeIgnoreCase,
                        matcherType)),
                new ExecutionLet(
                    new ExecutionVariable("prepared", typeof(bool)),
                    new ExecutionPreparedLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionVariableRead(matcher),
                        ExecutionClrBindingFactory.FromClr(typeof(bool)))),
                new ExecutionLet(
                    new ExecutionVariable("dynamic", typeof(bool)),
                    new ExecutionDynamicLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionLiteral("a%", typeof(string)),
                        new ExecutionVariableRead(cache),
                        ExecutionStringMatchComparison.LikeIgnoreCase,
                        ExecutionClrBindingFactory.FromClr(typeof(bool))))
            ]));
        var operations = ExecutionTargetOperationAnalyzer.Analyze(plan);
        var features = ExecutionTargetFeatureAnalyzer.Analyze(plan);

        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(operations).IsSupported);
        Assert.IsTrue(ExecutionTargetCapabilities.CSharpClr.Validate(features).IsSupported);
        var portableOperations = PortableSubsetTarget.Capabilities.Validate(operations);
        var portableFeatures = PortableSubsetTarget.Capabilities.Validate(features);
        CollectionAssert.AreEquivalent(
            new[]
            {
                "expr.like.cache-slot",
                "expr.like.dynamic-match",
                "expr.like.prepare-matcher",
                "expr.like.prepared-match"
            },
            portableOperations.UnsupportedOperations.Select(static operation => operation.Value).ToArray());
        Assert.IsTrue(portableFeatures.UnsupportedFeatures.Any(static feature =>
            feature.Kind == ExecutionTargetFeatureKind.LikeMatcherStrategy));
        Assert.IsTrue(portableFeatures.UnsupportedFeatures.Any(static feature =>
            feature.Kind == ExecutionTargetFeatureKind.StringMatchComparison));
    }
}
