using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ExecutionTargetPhaseDispatchTests
{
    [TestMethod]
    public void Render_WhenBackendReturnsArtifactForDifferentTarget_ShouldReject()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: new WrongTargetBackend()));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionTargetCatalog.Render(CreateMinimalRequest()));

        Assert.Contains("render result", exception.Message);
        Assert.Contains(ExecutionTargetIds.CSharpClr.ToString(), exception.Message);
    }

    [TestMethod]
    public void Render_WhenBackendInputsTargetDoesNotMatchRequest_ShouldRejectBeforeBackend()
    {
        var backend = new CapturingBackend();
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: backend));
        var request = CreateMinimalRequest() with
        {
            BackendInputs = new EmptyTargetBackendRenderInputs(ExecutionTargetIds.CSharpClr)
        };

        var exception = Assert.Throws<InvalidOperationException>(() => ExecutionTargetCatalog.Render(request));

        Assert.Contains("backend inputs", exception.Message);
        Assert.IsFalse(backend.WasCalled);
    }

    [TestMethod]
    public void Render_WhenBackendReportsExpectedLimitation_ShouldReturnDiagnostics()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: new FailingBackend()));

        var result = ExecutionTargetCatalog.Render(CreateMinimalRequest());

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Artifact);
        Assert.AreEqual(TargetDiagnosticCodes.UnsupportedLowering, result.Diagnostics.Single().Code);
    }

    [TestMethod]
    public void RenderResult_WhenSuccessOrFailureInvariantsAreViolated_ShouldReject()
    {
        var artifact = new TestRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr);
        var error = TargetDiagnostic.Error("MT-ERROR", "failed");
        var warning = new TargetDiagnostic("MT-WARNING", TargetDiagnosticSeverity.Warning, "warning");

        Assert.Throws<ArgumentException>(() => TargetRenderResult.Succeeded(artifact, [error]));
        Assert.Throws<ArgumentException>(() => TargetRenderResult.Failed(
            TestExecutionTargetIds.TestOnlyNonClr,
            [warning]));
    }

    [TestMethod]
    public void FinalizeArtifact_WhenFinalizerReturnsResultForDifferentTarget_ShouldReject()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(finalizer: new WrongTargetFinalizer()));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionTargetCatalog.FinalizeArtifact(
                new TestRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr),
                TargetFinalizationOptions.Empty));

        Assert.Contains("finalization result", exception.Message);
        Assert.Contains(ExecutionTargetIds.CSharpClr.ToString(), exception.Message);
    }

    [TestMethod]
    public void InspectArtifact_WhenInspectorReturnsResultForDifferentTarget_ShouldReject()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(inspector: new WrongTargetInspector()));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionTargetCatalog.InspectArtifact(
                new TestRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr)));

        Assert.Contains("inspection result", exception.Message);
        Assert.Contains(ExecutionTargetIds.CSharpClr.ToString(), exception.Message);
    }

    [TestMethod]
    public void FinalizationResult_WhenSuccessHasNoArtifact_ShouldReject()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TestFinalizationResult(
                TestExecutionTargetIds.TestOnlyNonClr,
                success: true,
                [],
                artifact: null));

        Assert.Contains("must produce an executable artifact", exception.Message);
    }

    [TestMethod]
    public void FinalizationResult_WhenArtifactTargetDoesNotMatchResult_ShouldReject()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TestFinalizationResult(
                TestExecutionTargetIds.TestOnlyNonClr,
                success: true,
                [],
                new TestExecutableArtifact(ExecutionTargetIds.CSharpClr)));

        Assert.Contains("cannot contain an executable artifact", exception.Message);
    }

    private static TargetRenderRequest CreateMinimalRequest()
    {
        var plan = new ExecutionPlan("Q_PhaseDispatch", [], new ExecutionBlock([]));
        var compatibility = new ExecutionTargetCompatibilityReport([]);
        return new TargetRenderRequest
        {
            TargetId = TestExecutionTargetIds.TestOnlyNonClr,
            Identity = new TargetRenderIdentity("PhaseDispatch"),
            Options = TargetRenderOptions.Empty,
            ScriptBinding = TargetScriptBindingContract.Empty,
            References = TargetReferenceInventory.Empty,
            ExecutionPlan = plan,
            ExecutionIrVersion = plan.ExecutionIrVersion,
            SemanticsContract = plan.SemanticsContract,
            OperationReport = ExecutionTargetOperationReport.Empty,
            FeatureReport = ExecutionTargetFeatureReport.Empty,
            CompatibilityReport = compatibility,
            RuntimeContract = new TargetRuntimeContract(
                plan.Identifier,
                [],
                [],
                [],
                new TargetNullBehaviorContract(false, false, false, "none"),
                new TargetCancellationContract(false, false),
                new TargetDiagnosticsContract(false, false, false),
                new TargetProfilingContract(false, false, 0, 0)),
            HostAbiVersion = TargetContractVersions.HostAbi,
            BackendInputs = new EmptyTargetBackendRenderInputs(TestExecutionTargetIds.TestOnlyNonClr)
        };
    }

    private sealed record TestRenderedArtifact(ExecutionTargetId TargetId) : RenderedQueryArtifact(TargetId);

    private sealed record TestExecutableArtifact(ExecutionTargetId TargetId) : ExecutableQueryArtifact(TargetId);

    private sealed class WrongTargetBackend : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } =
            ExecutionTargetCapabilities.CreateForSemantics([ExecutionSemanticsContract.Version1.Version]);

        public TargetRenderResult Render(TargetRenderRequest request) =>
            TargetRenderResult.Succeeded(new TestRenderedArtifact(ExecutionTargetIds.CSharpClr));
    }

    private sealed class CapturingBackend : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } =
            ExecutionTargetCapabilities.CreateForSemantics([ExecutionSemanticsContract.Version1.Version]);

        public bool WasCalled { get; private set; }

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            WasCalled = true;
            return TargetRenderResult.Succeeded(new TestRenderedArtifact(TargetId));
        }
    }

    private sealed class FailingBackend : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } =
            ExecutionTargetCapabilities.CreateForSemantics([ExecutionSemanticsContract.Version1.Version]);

        public TargetRenderResult Render(TargetRenderRequest request) =>
            TargetRenderResult.Failed(
                TargetId,
                [TargetDiagnostic.Error(TargetDiagnosticCodes.UnsupportedLowering, "test lowering is unavailable")]);
    }

    private sealed class WrongTargetFinalizer : IRenderedQueryFinalizer
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options) =>
            new TestFinalizationResult(
                ExecutionTargetIds.CSharpClr,
                success: true,
                [],
                new TestExecutableArtifact(ExecutionTargetIds.CSharpClr));
    }

    private sealed class WrongTargetInspector : IRenderedQueryInspector
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact) =>
            new(ExecutionTargetIds.CSharpClr, null, new Dictionary<string, string>());
    }

    private sealed record TestFinalizationResult : TargetFinalizationResult
    {
        public TestFinalizationResult(
            ExecutionTargetId targetId,
            bool success,
            IReadOnlyList<TargetDiagnostic> diagnostics,
            ExecutableQueryArtifact? artifact)
            : base(targetId, success, diagnostics, artifact)
        {
        }
    }
}
