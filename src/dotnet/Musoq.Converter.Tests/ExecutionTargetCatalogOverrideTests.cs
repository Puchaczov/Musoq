using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class ExecutionTargetCatalogOverrideTests
{
    [TestMethod]
    public void ExecutionTargetCatalog_WhenResolvingCSharpClr_ShouldReturnAllCSharpPhases()
    {
        Assert.IsInstanceOfType<CSharpClrExecutionBackend>(
            ExecutionTargetCatalog.ResolveBackend(ExecutionTargetIds.CSharpClr));
        Assert.IsInstanceOfType<CSharpClrRenderedQueryFinalizer>(
            ExecutionTargetCatalog.ResolveFinalizer(ExecutionTargetIds.CSharpClr));
        Assert.IsInstanceOfType<ClrAssemblyExecutableActivator>(
            ExecutionTargetCatalog.ResolveActivator(ExecutionTargetIds.CSharpClr));
        Assert.IsInstanceOfType<CSharpRenderedQueryInspector>(
            ExecutionTargetCatalog.ResolveInspector(ExecutionTargetIds.CSharpClr));
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenTestOnlyTargetHasNoRenderPhase_ShouldReject()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));

        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenTemporaryDescriptorIsActive_ShouldResolveAllRegisteredPhases()
    {
        var backend = new TestOnlyExecutionTarget.Backend();
        var finalizer = new TestOnlyExecutionTarget.Finalizer();
        var activator = new TestOnlyExecutionTarget.Activator();
        var inspector = new TestOnlyExecutionTarget.Inspector();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend,
            finalizer,
            activator,
            inspector);

        using (ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor))
        {
            Assert.AreSame(
                backend,
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
            Assert.AreSame(
                finalizer,
                ExecutionTargetCatalog.ResolveFinalizer(TestExecutionTargetIds.TestOnlyNonClr));
            Assert.AreSame(
                activator,
                ExecutionTargetCatalog.ResolveActivator(TestExecutionTargetIds.TestOnlyNonClr));
            Assert.AreSame(
                inspector,
                ExecutionTargetCatalog.ResolveInspector(TestExecutionTargetIds.TestOnlyNonClr));
        }

        Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
        Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveFinalizer(TestExecutionTargetIds.TestOnlyNonClr));
        Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveActivator(TestExecutionTargetIds.TestOnlyNonClr));
        Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveInspector(TestExecutionTargetIds.TestOnlyNonClr));
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenOverridesAreNested_ShouldRestoreOuterDescriptor()
    {
        var outerBackend = new TestOnlyExecutionTarget.Backend();
        var innerBackend = new TestOnlyExecutionTarget.Backend();
        var outer = TestOnlyExecutionTarget.CreateDescriptor(backend: outerBackend);
        var inner = TestOnlyExecutionTarget.CreateDescriptor(backend: innerBackend);

        using (ExecutionTargetCatalog.UseTemporaryDescriptor(outer))
        {
            Assert.AreSame(
                outerBackend,
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));

            using (ExecutionTargetCatalog.UseTemporaryDescriptor(inner))
            {
                Assert.AreSame(
                    innerBackend,
                    ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
            }

            Assert.AreSame(
                outerBackend,
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
        }

        Assert.Throws<NotSupportedException>(() =>
            ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenOverridesAreDisposedOutOfOrder_ShouldNotResurrectDisposedDescriptor()
    {
        var outerBackend = new TestOnlyExecutionTarget.Backend();
        var innerBackend = new TestOnlyExecutionTarget.Backend();
        var outer = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: outerBackend));
        var inner = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: innerBackend));

        try
        {
            outer.Dispose();
            Assert.AreSame(
                innerBackend,
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));

            inner.Dispose();
            Assert.Throws<NotSupportedException>(() =>
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
        }
        finally
        {
            inner.Dispose();
            outer.Dispose();
        }
    }

    [TestMethod]
    public async Task ExecutionTargetCatalog_WhenCapturedAsyncContextOutlivesRegistration_ShouldNotResolveDisposedDescriptor()
    {
        var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(backend: new TestOnlyExecutionTarget.Backend()));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var child = Task.Run(async () =>
        {
            started.SetResult();
            await release.Task;
            Assert.Throws<NotSupportedException>(() =>
                ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
        });

        await started.Task;
        registration.Dispose();
        release.SetResult();
        await child;
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenTemporaryDescriptorHasNoActivationPhase_ShouldRejectActivationOnly()
    {
        var backend = new TestOnlyExecutionTarget.Backend();
        var finalizer = new TestOnlyExecutionTarget.Finalizer();
        var inspector = new TestOnlyExecutionTarget.Inspector();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: finalizer,
            inspector: inspector);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        Assert.AreSame(
            backend,
            ExecutionTargetCatalog.ResolveBackend(TestExecutionTargetIds.TestOnlyNonClr));
        Assert.AreSame(
            finalizer,
            ExecutionTargetCatalog.ResolveFinalizer(TestExecutionTargetIds.TestOnlyNonClr));
        Assert.AreSame(
            inspector,
            ExecutionTargetCatalog.ResolveInspector(TestExecutionTargetIds.TestOnlyNonClr));

        var exception = Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveActivator(TestExecutionTargetIds.TestOnlyNonClr));

        Assert.Contains("does not support activation", exception.Message);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenDescriptorHasNoRenderContributionFactory_ShouldReturnEmptyContribution()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor());

        var contribution = ExecutionTargetCatalog.CreateRenderBuildContribution(
            new TestOnlyRenderedArtifact());

        Assert.AreEqual(QueryMethodRenderMetadata.Unknown, contribution.QueryMethodRenderMetadata);
        Assert.IsNull(contribution.OptimizationTrace);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenDescriptorHasNoPackageFactory_ShouldRejectArtifactPackaging()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor());

        var exception = Assert.Throws<NotSupportedException>(() => ExecutionTargetCatalog.CreateArtifactPackage(
            new TargetArtifactPackagingContext(
                TestExecutionTargetIds.TestOnlyNonClr,
                "TestOnlyPackage",
                "select 1",
                "options",
                new TestOnlyRenderedArtifact(),
                TargetExportArtifact.Create(TestExecutionTargetIds.TestOnlyNonClr),
                TargetArtifactSemanticFacts.Empty,
                ExecutionSemanticsContract.Version1)));

        Assert.Contains("does not support artifact packaging", exception.Message);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenDescriptorUsesPortableExportPackageFactory_ShouldPackagePortableExportArtifact()
    {
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(
            TestOnlyExecutionTarget.CreateDescriptor(createArtifactPackage: CreateExportPackage));
        var executableArtifact = TargetExportArtifact.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            sourceFiles:
            [
                new TargetExportSourceFile("query.js", "javascript", "export function run() {}")
            ],
            binaryBlobs:
            [
                new TargetExportBinaryBlob("query.bin", [1, 2, 3], "application/octet-stream")
            ],
            entrypoints:
            [
                new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")
            ],
            runtimeServices: TargetRuntimeServiceRequirements.CreateTargetProvided(
                TargetRuntimeServiceRequirementKind.SourceAccess));

        var package = ExecutionTargetCatalog.CreateArtifactPackage(
            new TargetArtifactPackagingContext(
                TestExecutionTargetIds.TestOnlyNonClr,
                "TestOnlyPackage",
                "select 1",
                "options",
                new TestOnlyRenderedArtifact(),
                executableArtifact,
                TargetArtifactSemanticFacts.Empty,
                ExecutionSemanticsContract.Version1));

        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, package.TargetId);
        Assert.AreEqual("TargetExport", package.ArtifactKind);
        Assert.AreEqual(nameof(TargetExportArtifact), package.ExecutableArtifactKind);
        Assert.HasCount(1, package.SourceFiles);
        Assert.HasCount(1, package.BinaryBlobs);
        Assert.HasCount(1, package.Entrypoints);
        Assert.IsTrue(package.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenPackageFactoryReturnsDifferentTarget_ShouldReject()
    {
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            createArtifactPackage: static _ => TargetArtifactPackage.CreateValidated(
                ExecutionTargetIds.CSharpClr,
                "wrong",
                "wrong",
                ExecutionSemanticsContract.Version1));
        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExecutionTargetCatalog.CreateArtifactPackage(
                new TargetArtifactPackagingContext(
                    TestExecutionTargetIds.TestOnlyNonClr,
                    "TestOnlyPackage",
                    "select 1",
                    "options",
                    new TestOnlyRenderedArtifact(),
                    TargetExportArtifact.Create(TestExecutionTargetIds.TestOnlyNonClr),
                    TargetArtifactSemanticFacts.Empty,
                    ExecutionSemanticsContract.Version1)));

        Assert.Contains("produced a package for target", exception.Message);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_WhenTestOnlyTargetHasNoFinalizationPhase_ShouldReject()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => ExecutionTargetCatalog.ResolveFinalizer(TestExecutionTargetIds.TestOnlyNonClr));

        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    [TestMethod]
    public void ExecutionTargetDescriptor_WhenComponentTargetDoesNotMatch_ShouldReject()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ExecutionTargetDescriptor.Create(
                ExecutionTargetIds.CSharpClr,
                renderPhase: new TestOnlyExecutionTarget.Backend(),
                finalizationPhase: new CSharpClrRenderedQueryFinalizer(),
                activationPhase: new ClrAssemblyExecutableActivator(),
                inspectionPhase: new CSharpRenderedQueryInspector()));

        Assert.Contains("CSharpClr", exception.Message);
        Assert.Contains("TestOnlyNonClr", exception.Message);
    }

    private sealed record TestOnlyRenderedArtifact()
        : RenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private static TargetArtifactPackage CreateExportPackage(TargetArtifactPackagingContext context)
    {
        var exportArtifact = Assert.IsInstanceOfType<TargetExportArtifact>(context.ExecutableArtifact);
        return TargetArtifactPackage.CreatePortableExportPackage(
            context.TargetId,
            "TargetExport",
            exportArtifact,
            context.SemanticsContract);
    }
}
