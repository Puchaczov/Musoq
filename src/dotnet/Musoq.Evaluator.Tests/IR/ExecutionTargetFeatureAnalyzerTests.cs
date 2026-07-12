using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Targets.Abstractions;
using Musoq.Targets.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionTargetFeatureAnalyzerTests
{
    [TestMethod]
    public void Analyze_ShouldInventoryPortableFeaturesDeterministically()
    {
        var field = new FieldBinding(
            "Value",
            "s.Value",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new ClrPropertyAccess(nameof(FeatureSource.Value)),
            readModifiers: new System.Collections.Generic.Dictionary<string, string>
            {
                ["json"] = "$.value"
            });
        var sourceShape = new SourceEntityShape("s", typeof(FeatureSource), [field]);
        var source = new ExecutionSourceScan(
            new ExecutionVariable("s", typeof(FeatureSource)),
            new ExecutionVariable("rows", typeof(object)),
            new ExecutionSourceBinding("sample", "rows", "s:0", 0, [], [field], SourceType: ExecutionTypeRef.FromClr(typeof(FeatureSource))));
        var binary = new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
        var unary = new ExecutionUnary(UnaryOpKind.Negate, binary, typeof(int));
        var cast = new ExecutionStrictCast(unary, "int", typeof(int));
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var methodCall = new ExecutionMethodCall(
            method,
            [new ExecutionLiteral("value", typeof(string))],
            "upper",
            typeof(string));
        var plan = new ExecutionPlan(
            "Q_Features",
            [sourceShape],
            new ExecutionBlock(
            [
                source,
                new ExecutionLet(new ExecutionVariable("result", typeof(int)), cast),
                new ExecutionLet(new ExecutionVariable("upper", typeof(string)), methodCall)
            ]));

        var report = ExecutionTargetFeatureAnalyzer.Analyze(plan);

        AssertFeature(report, ExecutionTargetFeatureKind.SourceKind, "source:scan");
        AssertFeature(report, ExecutionTargetFeatureKind.ReadModifier, "read-modifier:json");
        AssertFeature(report, ExecutionTargetFeatureKind.ConstantKind, "constant:signed-integer");
        AssertFeature(report, ExecutionTargetFeatureKind.ConstantKind, "constant:string");
        AssertFeature(report, ExecutionTargetFeatureKind.BinaryOperation, "binary:add");
        AssertFeature(report, ExecutionTargetFeatureKind.UnaryOperation, "unary:negate");
        AssertFeature(report, ExecutionTargetFeatureKind.StrictCastTarget, "strict-cast:primitive:int32");
        AssertFeature(report, ExecutionTargetFeatureKind.CallableKind, "callable-kind:clr-method");
        Assert.IsTrue(report.Features.Any(feature =>
            feature.Kind == ExecutionTargetFeatureKind.Callable &&
            feature.StableId.Contains("string", StringComparison.Ordinal)));
        AssertFeature(report, ExecutionTargetFeatureKind.TypePortability, "type-portability:portable");
        AssertFeature(report, ExecutionTargetFeatureKind.DynamicValue, "dynamic:host-opaque");

        var ordered = report.Features
            .OrderBy(static feature => feature.Kind)
            .ThenBy(static feature => feature.StableId, StringComparer.Ordinal)
            .ToArray();
        CollectionAssert.AreEqual(ordered, report.Features.ToArray());
    }

    [TestMethod]
    public void Analyze_WhenPlanUsesPortableContainer_ShouldReportContainerRequirement()
    {
        var listType = ExecutionTypeRef.FromClr(typeof(System.Collections.Generic.List<int>));
        var plan = new ExecutionPlan(
            "Q_ContainerFeature",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("values", listType),
                    new ExecutionLiteral((object?)null, listType))
            ]));

        var report = ExecutionTargetFeatureAnalyzer.Analyze(plan);

        AssertFeature(report, ExecutionTargetFeatureKind.Container, "container:list");
    }

    [TestMethod]
    public void Capabilities_WhenFeatureKindIsUnsupported_ShouldReportFeatureDeterministically()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            [],
            [],
            Enum.GetValues<ExecutionPortableSymbolPortability>(),
            Enum.GetValues<ExecutionPortableSymbolPortability>(),
            [],
            [ExecutionSemanticsContract.Version1.Version],
            [ExecutionTargetFeatureKind.ConstantKind]);
        var report = new ExecutionTargetFeatureReport(
        [
            new ExecutionTargetFeature(
                ExecutionTargetFeatureKind.BinaryOperation,
                "binary:add",
                "Add")
        ]);

        var validation = capabilities.Validate(report);

        Assert.IsFalse(validation.IsSupported);
        Assert.HasCount(1, validation.UnsupportedFeatures);
        Assert.AreEqual("binary:add", validation.UnsupportedFeatures[0].StableId);
        Assert.AreEqual(
            "Execution target 'PortableSubset' does not support: feature: binary:add",
            validation.FormatUnsupportedRequirements("PortableSubset"));
    }

    private static void AssertFeature(
        ExecutionTargetFeatureReport report,
        ExecutionTargetFeatureKind kind,
        string stableId)
    {
        Assert.IsTrue(
            report.Features.Any(feature => feature.Kind == kind && feature.StableId == stableId),
            $"Expected target feature '{kind}:{stableId}'.");
    }

    private sealed class FeatureSource
    {
        public int Value { get; init; }
    }
}
