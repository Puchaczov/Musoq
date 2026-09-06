using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class CompileTimeCancellationTargetTests
{
    [TestMethod]
    public async Task Render_WhenBackendWaits_ShouldReceiveAndPropagateTheInvocationToken()
    {
        using var gate = new CooperativeTargetGate();
        var backend = new CooperativeBackend(gate);
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: backend));

        var token = await AssertCancelledAtGateAsync(
            gate,
            cancellationToken => ExecutionTargetCatalog.Render(CreateMinimalRequest(cancellationToken)));

        Assert.AreEqual(token, backend.ObservedToken);
    }

    [TestMethod]
    public async Task FinalizeArtifact_WhenFinalizerWaits_ShouldReceiveAndPropagateTheInvocationToken()
    {
        using var gate = new CooperativeTargetGate();
        var finalizer = new CooperativeFinalizer(gate);
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(
                finalizer: finalizer,
                createFinalizationOptions: static context =>
                    new CooperativeFinalizationOptions(context.CancellationToken)));

        var token = await AssertCancelledAtGateAsync(
            gate,
            cancellationToken =>
            {
                var options = ExecutionTargetCatalog.CreateFinalizationOptions(
                    TestExecutionTargetIds.TestOnlyNonClr,
                    new TargetFinalizationOptionsContext(false, CancellationToken: cancellationToken));
                return ExecutionTargetCatalog.FinalizeArtifact(
                    new CooperativeRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr),
                    options);
            });

        Assert.AreEqual(token, finalizer.ObservedToken);
    }

    [TestMethod]
    public async Task CreateArtifactPackage_WhenFactoryWaits_ShouldReceiveAndPropagateTheInvocationToken()
    {
        using var gate = new CooperativeTargetGate();
        CancellationToken? observedToken = null;
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(
                createArtifactPackage: context =>
                {
                    observedToken = context.CancellationToken;
                    gate.Wait(context.CancellationToken);
                    return TargetArtifactPackage.CreateValidated(
                        context.TargetId,
                        "cancellation-test",
                        nameof(TargetExportArtifact),
                        context.SemanticsContract,
                        executionIrVersion: context.ExecutionIrVersion);
                }));

        var token = await AssertCancelledAtGateAsync(
            gate,
            cancellationToken => ExecutionTargetCatalog.CreateArtifactPackage(
                new TargetArtifactPackagingContext(
                    cancellationToken,
                    TestExecutionTargetIds.TestOnlyNonClr,
                    "CancellationPackage",
                    "select 1",
                    "options",
                    new CooperativeRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr),
                    new CooperativeExecutableArtifact(TestExecutionTargetIds.TestOnlyNonClr),
                    TargetArtifactSemanticFacts.Empty,
                    ExecutionSemanticsContract.Version1)));

        Assert.AreEqual(token, observedToken);
    }

    [TestMethod]
    public async Task InspectArtifact_WhenInspectorWaits_ShouldReceiveAndPropagateTheInvocationToken()
    {
        using var gate = new CooperativeTargetGate();
        var inspector = new CooperativeInspector(gate);
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(inspector: inspector));

        var token = await AssertCancelledAtGateAsync(
            gate,
            cancellationToken => ExecutionTargetCatalog.InspectArtifact(
                new CooperativeRenderedArtifact(TestExecutionTargetIds.TestOnlyNonClr),
                cancellationToken));

        Assert.AreEqual(token, inspector.ObservedToken);
    }

    private static TargetRenderRequest CreateMinimalRequest(CancellationToken cancellationToken)
    {
        var plan = new ExecutionPlan("Q_CancellationTarget", [], new ExecutionBlock([]));
        return new TargetRenderRequest
        {
            CancellationToken = cancellationToken,
            TargetId = TestExecutionTargetIds.TestOnlyNonClr,
            Purpose = TargetRenderPurpose.Execution,
            Profile = TargetRenderProfile.ExecutionFast,
            Identity = new TargetRenderIdentity("CancellationTarget"),
            Options = TargetRenderOptions.Empty,
            ScriptBinding = TargetScriptBindingContract.Empty,
            References = TargetReferenceInventory.Empty,
            ExecutionPlan = plan,
            ExecutionIrVersion = plan.ExecutionIrVersion,
            SemanticsContract = plan.SemanticsContract,
            OperationReport = ExecutionTargetOperationReport.Empty,
            FeatureReport = ExecutionTargetFeatureReport.Empty,
            CompatibilityReport = new ExecutionTargetCompatibilityReport([]),
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

    private static async Task<CancellationToken> AssertCancelledAtGateAsync<T>(
        CooperativeTargetGate gate,
        Func<CancellationToken, T> operation)
    {
        using var cancellation = new CancellationTokenSource();
        var operationTask = Task.Run(() => operation(cancellation.Token));

        try
        {
            var firstCompleted = await Task.WhenAny(gate.Entered, operationTask);
            Assert.AreSame(
                gate.Entered,
                firstCompleted,
                $"The target operation completed before entering its cancellation gate. Status={operationTask.Status}; Exception={operationTask.Exception}");
            Assert.IsFalse(operationTask.IsCompleted);

            var token = cancellation.Token;
            cancellation.Cancel();

            try
            {
                _ = await operationTask;
                Assert.Fail("The cancelled target operation returned normally.");
            }
            catch (OperationCanceledException exception)
            {
                Assert.AreEqual(token, exception.CancellationToken);
            }

            return token;
        }
        finally
        {
            cancellation.Cancel();
            gate.Release();
            if (!operationTask.IsCompleted)
            {
                try
                {
                    _ = await operationTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    private sealed class CooperativeTargetGate : IDisposable
    {
        private readonly TaskCompletionSource<bool> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public void Wait(CancellationToken cancellationToken)
        {
            _entered.TrySetResult(true);
            _release.Task.WaitAsync(cancellationToken).GetAwaiter().GetResult();
        }

        public void Release() => _release.TrySetResult(true);

        public void Dispose() => Release();
    }

    private sealed class CooperativeBackend(CooperativeTargetGate gate) : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } =
            ExecutionTargetCapabilities.CreateForSemantics([ExecutionSemanticsContract.Version1.Version]);

        public CancellationToken? ObservedToken { get; private set; }

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            ObservedToken = request.CancellationToken;
            gate.Wait(request.CancellationToken);
            return TargetRenderResult.Succeeded(new CooperativeRenderedArtifact(TargetId));
        }
    }

    private sealed class CooperativeFinalizer(CooperativeTargetGate gate) : IRenderedQueryFinalizer
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public CancellationToken? ObservedToken { get; private set; }

        public TargetFinalizationResult Finalize(
            RenderedQueryArtifact artifact,
            TargetFinalizationOptions options)
        {
            ObservedToken = options.CancellationToken;
            gate.Wait(options.CancellationToken);
            return new CooperativeFinalizationResult(
                new CooperativeExecutableArtifact(TargetId));
        }
    }

    private sealed class CooperativeInspector(CooperativeTargetGate gate)
        : IRenderedQueryInspector, ICancellableRenderedQueryInspector
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public CancellationToken? ObservedToken { get; private set; }

        public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
        {
            return new RenderedQueryInspection(TargetId, null, new Dictionary<string, string>());
        }

        public RenderedQueryInspection Inspect(
            RenderedQueryArtifact artifact,
            CancellationToken cancellationToken)
        {
            ObservedToken = cancellationToken;
            gate.Wait(cancellationToken);
            return new RenderedQueryInspection(TargetId, null, new Dictionary<string, string>());
        }
    }

    private sealed record CooperativeRenderedArtifact(ExecutionTargetId TargetId)
        : RenderedQueryArtifact(TargetId);

    private sealed record CooperativeExecutableArtifact(ExecutionTargetId TargetId)
        : ExecutableQueryArtifact(TargetId);

    private sealed record CooperativeFinalizationResult(ExecutableQueryArtifact Executable)
        : TargetFinalizationResult(
            TestExecutionTargetIds.TestOnlyNonClr,
            true,
            [],
            Executable);

    private sealed record CooperativeFinalizationOptions(CancellationToken Token)
        : TargetFinalizationOptions
    {
        public override CancellationToken CancellationToken => Token;
    }
}
