using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class NonClrTargetPipelinePressureTests
{
    [TestMethod]
    public void FakeNonClrTarget_ShouldFlowThroughTargetRegistriesAndArtifactContainers()
    {
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: new FakeBackend(),
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createArtifactPackage: CreateFakePackage);

        Assert.IsNull(descriptor.ActivationPhase);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var request = CreateMinimalRequest();
        var renderResult = ExecutionTargetCatalog.Render(request);
        Assert.IsTrue(renderResult.Success);
        var renderedArtifact = renderResult.Artifact!;
        Assert.Throws<NotSupportedException>(() =>
            ExecutionTargetCatalog.ResolveActivator(TestExecutionTargetIds.TestOnlyNonClr));
        var finalizationResult = ExecutionTargetCatalog.FinalizeArtifact(
            renderedArtifact,
            TargetFinalizationOptions.Empty);
        var compilationArtifacts = CompilationBuildArtifacts.From(finalizationResult);
        var items = new BuildItems
        {
            RenderingArtifact = renderedArtifact,
            CompilationArtifacts = compilationArtifacts
        };
        var inspection = ExecutionTargetCatalog.InspectArtifact(renderedArtifact);
        var package = ExecutionTargetCatalog.CreateArtifactPackage(
            CreatePackagingContext(
                renderedArtifact,
                finalizationResult.Artifact!,
                request.RuntimeContract,
                request.CompatibilityReport));

        Assert.IsInstanceOfType<FakeRenderedQueryArtifact>(renderedArtifact);
        Assert.IsTrue(finalizationResult.Success);
        var export = Assert.IsInstanceOfType<TargetExportArtifact>(finalizationResult.Artifact);
        Assert.HasCount(1, export.SourceFiles);
        Assert.AreEqual("query.js", export.SourceFiles[0].Path);
        Assert.AreEqual("javascript", export.SourceFiles[0].Language);
        Assert.HasCount(1, export.BinaryBlobs);
        Assert.IsEmpty(export.RuntimeServices.RequiredServices);
        Assert.AreSame(renderedArtifact, items.RenderingArtifact);
        Assert.AreSame(finalizationResult.Artifact, items.ExecutableArtifact);
        Assert.IsNull(items.DllFile);
        Assert.IsNull(items.PdbFile);
        Assert.IsFalse(CSharpClrArtifactCompatibility.TryGetAssemblyExecutable(export, out _));
        Assert.Throws<InvalidOperationException>(() => _ = items.CompilationArtifacts.EmitResult);
        Assert.Throws<InvalidOperationException>(() =>
            CSharpClrArtifactCompatibility.RequireRenderedArtifact(renderedArtifact, "fake target pressure test"));
        Assert.Throws<InvalidOperationException>(() =>
            CSharpClrArtifactCompatibility.RequireFinalizationResult(finalizationResult, "fake target pressure test"));
        Assert.IsNull(inspection.GeneratedCSharpCode);
        Assert.AreEqual("fake-js", inspection.SourceMetadata["language"]);
        AssertFakeExportPackage(
            package,
            expectSourceAccessAbi: false,
            expectRowShapeAbi: false,
            expectDiagnosticsAbi: false);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CompiledQueryArtifactSupport.CreateCompiledArtifactFromPackage(
                package,
                CompiledQueryArtifactSupport.CurrentEngineVersion,
                CompiledQueryArtifact.CurrentArtifactFormatVersion,
                "options"));
        Assert.Contains("support only", exception.Message);
        Assert.Contains(ExecutionTargetIds.CSharpClr.ToString(), exception.Message);
    }

    [TestMethod]
    public void FakeNonClrTarget_ShouldReceiveRealRenderRequestFromInternalBuildPipeline()
    {
        const string query = "select d.Dummy from #system.dual() d";
        var backend = new CapturingFakeBackend();
        TargetRuntimeContract? packagedRuntimeContract = null;
        ExecutionTargetReadinessReport? packagedReadinessReport = null;
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: context =>
            {
                packagedRuntimeContract = context.RuntimeContract;
                packagedReadinessReport = context.ReadinessReport;
                return CreateFakePackage(context);
            });

        Assert.IsNull(descriptor.ActivationPhase);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            $"FakeNonClrPackage{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(result.Package);
        Assert.IsNotNull(result.Inspection);
        Assert.IsNotNull(backend.Request);
        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, backend.Request.TargetId);
        Assert.IsNotNull(backend.Request.ExecutionPlan);
        Assert.IsNotNull(backend.Request.CompatibilityReport);
        Assert.IsNotNull(backend.Request.RuntimeContract);
        Assert.AreEqual(backend.Request.SemanticsContract.Version, result.Package.SemanticsContract.Version);
        Assert.AreSame(backend.Request.CompatibilityReport, result.BuildItems.ExecutionTargetCompatibilityReport);
        Assert.AreSame(backend.Request.RuntimeContract, result.BuildItems.TargetRuntimeContract);
        Assert.AreSame(result.BuildItems.TargetRuntimeContract, packagedRuntimeContract);
        Assert.AreSame(result.BuildItems.ExecutionTargetReadinessReport, packagedReadinessReport);
        var fakeInputs = Assert.IsInstanceOfType<FakeTargetRenderInputs>(backend.Request.BackendInputs);
        Assert.AreEqual(result.BuildItems.QueryResultMode, fakeInputs.QueryResultMode);
        Assert.AreEqual(0, fakeInputs.ParameterCount);
        Assert.AreEqual(0, fakeInputs.VariableCount);
        Assert.IsTrue(fakeInputs.ReferenceNameCount > 0);
        Assert.IsNotInstanceOfType<CSharpClrRenderInputs>(backend.Request.BackendInputs);
        Assert.IsTrue(backend.Request.RuntimeContract.SourceAccess.Count > 0);
        Assert.AreEqual(backend.Request.ExecutionPlan.Identifier, backend.Request.RuntimeContract.PlanIdentifier);
        var export = Assert.IsInstanceOfType<TargetExportArtifact>(result.BuildItems.ExecutableArtifact);
        Assert.HasCount(1, export.SourceFiles);
        Assert.HasCount(1, export.BinaryBlobs);
        Assert.IsTrue(export.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
        AssertFakeExportPackage(result.Package, expectSourceAccessAbi: true, expectRowShapeAbi: true);
        Assert.AreEqual("fake-js", result.Inspection.SourceMetadata["language"]);
        Assert.AreEqual("fake-js", result.Package.Metadata["language"]);
        Assert.IsNull(result.BuildItems.DllFile);
        Assert.IsNull(result.BuildItems.PdbFile);
        Assert.IsFalse(CSharpClrArtifactCompatibility.TryGetAssemblyExecutable(export, out _));
        Assert.Throws<NotSupportedException>(() =>
            ExecutionTargetCatalog.ResolveActivator(TestExecutionTargetIds.TestOnlyNonClr));
        var exception = Assert.Throws<InvalidOperationException>(() =>
            CompiledQueryArtifactSupport.CreateCompiledArtifactFromPackage(
                result.Package,
                CompiledQueryArtifactSupport.CurrentEngineVersion,
                CompiledQueryArtifact.CurrentArtifactFormatVersion,
                "options"));
        Assert.Contains("support only", exception.Message);
    }

    [TestMethod]
    public void FakeNonClrTarget_WhenInspectionPhaseIsAbsent_ShouldStillBuildPackage()
    {
        var backend = new CapturingFakeBackend();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: CreateFakePackage);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            "select d.Dummy from #system.dual() d",
            $"FakeNonClrNoInspection{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.Package);
        Assert.IsNull(result.Inspection);
        Assert.IsNotNull(result.BuildItems);
        Assert.Throws<NotSupportedException>(() =>
            ExecutionTargetCatalog.InspectArtifact(result.BuildItems.RenderingArtifact));
    }

    [TestMethod]
    public void FakeNonClrTarget_WhenTypedResultIsRequested_ShouldBuildTypedExportPackage()
    {
        var backend = new CapturingFakeBackend();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: CreateFakePackage);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics<FakeTypedOutput>(
            "select d.Dummy as Dummy from #system.dual() d",
            $"FakeNonClrTyped{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.Package);
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(backend.Request);
        Assert.AreEqual(QueryResultMode.TypedEnumerable, result.BuildItems.QueryResultMode);
        Assert.AreEqual(typeof(FakeTypedOutput), result.BuildItems.OutputType);
        var inputs = Assert.IsInstanceOfType<FakeTargetRenderInputs>(backend.Request.BackendInputs);
        Assert.AreEqual(QueryResultMode.TypedEnumerable, inputs.QueryResultMode);
        Assert.IsTrue(result.Package.Entrypoints.Any(static entrypoint =>
            entrypoint.Kind == TargetRuntimeEntrypointKind.TypedQuery));
        Assert.IsFalse(result.Package.Entrypoints.Any(static entrypoint =>
            entrypoint.Kind == TargetRuntimeEntrypointKind.TableQuery));
        Assert.IsNull(result.Inspection);
    }

    [TestMethod]
    public void FakeNonClrTarget_WhenRuntimeServicesAreTargetProvided_ShouldPackageWithoutHostAbiImports()
    {
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: new CapturingFakeBackend(),
            finalizer: new SelfContainedFakeFinalizer(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: static context =>
            {
                var export = Assert.IsInstanceOfType<TargetExportArtifact>(context.ExecutableArtifact);
                return TargetArtifactPackage.CreatePortableExportPackage(
                    context.TargetId,
                    "FakeSelfContainedExport",
                    export,
                    context.SemanticsContract,
                    export.DiagnosticsMetadata,
                    context.ExecutionIrVersion);
            });

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            "select 1 as Value from #system.dual() d",
            $"FakeNonClrSelfContained{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.Package);
        Assert.IsEmpty(result.Package.RuntimeServices.RequiredServices);
        Assert.IsEmpty(result.Package.HostAbiInventory.Imports);
        Assert.IsTrue(result.Package.Entrypoints.Any(static entrypoint =>
            entrypoint.Kind == TargetRuntimeEntrypointKind.TableQuery));
    }

    [TestMethod]
    [DataRow(
        "select d.Dummy from #system.dual() d",
        0,
        0,
        true,
        true,
        DisplayName = "source scan")]
    [DataRow(
        "param(expected: string) select d.Dummy from #system.dual() d where d.Dummy = $expected",
        1,
        0,
        true,
        true,
        DisplayName = "parameter binding")]
    [DataRow(
        "let expected: string = 'single'; select d.Dummy from #system.dual() d where d.Dummy = $expected",
        0,
        1,
        true,
        true,
        DisplayName = "script variable binding")]
    [DataRow(
        "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d group by d.Dummy",
        0,
        0,
        true,
        true,
        DisplayName = "aggregate")]
    [DataRow(
        """
        from values {
            { Name: 'first', Score: 1 },
            { Name: 'second', Score: 2 }
        } rows
        select rows.Name, rows.Score
        """,
        0,
        0,
        false,
        true,
        DisplayName = "generated row source")]
    public void FakeNonClrTarget_ShouldReceiveRealRenderRequestsForRepresentativeQueryShapes(
        string query,
        int expectedParameterCount,
        int expectedVariableCount,
        bool expectedSourceAccessAbi,
        bool expectedRowShapeAbi)
    {
        var backend = new CapturingFakeBackend();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: CreateFakePackage);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            $"FakeNonClrShape{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.Package);
        Assert.IsNotNull(result.Inspection);
        Assert.IsNotNull(backend.Request);
        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, backend.Request.TargetId);
        Assert.IsNotNull(backend.Request.ExecutionPlan);
        Assert.IsNotNull(backend.Request.CompatibilityReport);
        Assert.IsNotNull(backend.Request.RuntimeContract);
        Assert.IsNotInstanceOfType<CSharpClrRenderInputs>(backend.Request.BackendInputs);
        Assert.HasCount(expectedParameterCount, backend.Request.ScriptBinding.ParameterNames);
        Assert.HasCount(expectedVariableCount, backend.Request.ScriptBinding.VariableNames);
        Assert.IsTrue(backend.Request.RuntimeContract.RowShapes.Count > 0);
        Assert.IsNotNull(result.BuildItems);
        Assert.AreSame(backend.Request.CompatibilityReport, result.BuildItems.ExecutionTargetCompatibilityReport);
        Assert.AreSame(backend.Request.RuntimeContract, result.BuildItems.TargetRuntimeContract);
        Assert.IsNotNull(result.BuildItems.ExecutionTargetReadinessReport);
        Assert.IsInstanceOfType<TargetExportArtifact>(result.BuildItems.ExecutableArtifact);
        AssertFakeExportPackage(
            result.Package,
            expectedSourceAccessAbi,
            expectedRowShapeAbi);
        Assert.AreEqual("fake-js", result.Inspection.SourceMetadata["language"]);
    }

    [TestMethod]
    public void FakeNonClrTarget_WhenQueryCallsPluginMethod_ShouldPackagePluginAbi()
    {
        const string query = "select ToUpper(d.Dummy) as UpperDummy from #system.dual() d";
        var backend = new CapturingFakeBackend();
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createRenderInputs: static context => new FakeTargetRenderInputs(
                TestExecutionTargetIds.TestOnlyNonClr,
                context.QueryResultMode,
                context.ScriptBinding.ParameterNames.Count,
                context.ScriptBinding.VariableNames.Count,
                context.References.ReferenceNames.Count),
            createArtifactPackage: CreateFakePackage);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            query,
            $"FakeNonClrCallable{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics));
        Assert.IsNotNull(result.Package);
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(backend.Request);
        var methodRequirement = backend.Request.CompatibilityReport.Requirements.First(requirement =>
            requirement.Kind == ExecutionTargetRequirementKind.MethodInfoCall &&
            requirement.CallableSymbol?.MethodName == nameof(LibraryBase.ToUpper));
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, methodRequirement.CallableSymbol!.Portability);
        Assert.IsTrue(backend.Request.RuntimeContract.PluginInvocations.Any(invocation =>
            invocation.Callable.MethodName == nameof(LibraryBase.ToUpper) &&
            invocation.Callable.Portability == ExecutionPortableSymbolPortability.HostImport));

        var pluginImport = result.Package.HostAbiInventory.Imports.First(import =>
            import.Kind == TargetHostAbiImportKind.PluginInvocation &&
            import.Attributes["methodName"] == nameof(LibraryBase.ToUpper));
        Assert.AreEqual("plugin-invocation-v2", pluginImport.Contract);
        Assert.AreEqual(2, pluginImport.ContractVersion);
        var pluginDetails = Assert.IsInstanceOfType<TargetPluginInvocationAbiDetails>(pluginImport.Details);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), pluginDetails.MethodName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, pluginDetails.CallablePortability);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), pluginImport.Attributes["methodName"]);
        Assert.AreEqual("HostImport", pluginImport.Attributes["callablePortability"]);

        Assert.IsNotNull(result.BuildItems.ExecutionTargetReadinessReport);
        Assert.IsTrue(result.BuildItems.ExecutionTargetReadinessReport.Issues.Any(issue =>
            issue.Category == ExecutionTargetReadinessCategory.PluginInvocation &&
            issue.Requirement.Detail.Contains(nameof(LibraryBase.ToUpper), StringComparison.Ordinal)));
        Assert.IsFalse(result.BuildItems.ExecutionTargetReadinessReport.Issues.Any(issue =>
            issue.Requirement.CallableSymbol?.Portability == ExecutionPortableSymbolPortability.ClrOnly));
    }

    [TestMethod]
    public void CSharpClrBackend_WhenRenderInputsAreNotCSharpClrInputs_ShouldReject()
    {
        var request = CreateMinimalRequest() with
        {
            TargetId = ExecutionTargetIds.CSharpClr,
            BackendInputs = new EmptyTargetBackendRenderInputs(ExecutionTargetIds.CSharpClr)
        };

        var exception = Assert.Throws<NotSupportedException>(
            () => new CSharpClrExecutionBackend().Render(request));

        Assert.Contains(nameof(CSharpClrRenderInputs), exception.Message);
    }

    [TestMethod]
    public void Render_WhenTargetCapabilitiesRejectRequest_ShouldFailBeforeBackendRender()
    {
        var backend = new CapturingFakeBackend(ExecutionTargetCapabilities.Create());
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createArtifactPackage: CreateFakePackage);
        var plan = new ExecutionPlan("Q_UnsupportedFakeNonClr", [], new ExecutionBlock([]));
        var compatibilityReport = new ExecutionTargetCompatibilityReport(
        [
            new ExecutionTargetRequirement(
                ExecutionTargetRequirementKind.MethodInfoCall,
                "Sample.Plugin.Method")
        ]);
        var request = CreateMinimalRequest() with
        {
            ExecutionPlan = plan,
            CompatibilityReport = compatibilityReport,
            RuntimeContract = CreateNoRuntimeRequirementsContract(plan)
        };

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = ExecutionTargetCatalog.Render(request);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedRequirement &&
            diagnostic.Message.Contains("MethodInfoCall: Sample.Plugin.Method", StringComparison.Ordinal)));
        Assert.IsNull(backend.Request, "Backend render must not run when target capabilities reject the request.");
    }

    [TestMethod]
    public void Render_WhenTargetCapabilitiesRejectOperation_ShouldFailBeforeBackendRender()
    {
        var backend = new CapturingFakeBackend(ExecutionTargetCapabilities.Create());
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(backend: backend);
        var plan = new ExecutionPlan(
            "Q_UnsupportedOperation",
            [],
            new ExecutionBlock([
                new ExecutionLet(
                    new ExecutionVariable("value", typeof(int)),
                    new ExecutionLiteral(1, typeof(int)))
            ]));
        var request = CreateMinimalRequest() with
        {
            ExecutionPlan = plan,
            OperationReport = ExecutionTargetOperationAnalyzer.Analyze(plan),
            RuntimeContract = CreateNoRuntimeRequirementsContract(plan)
        };

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = ExecutionTargetCatalog.Render(request);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedOperation &&
            diagnostic.Message.Contains("expr.literal", StringComparison.Ordinal)));
        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedOperation &&
            diagnostic.Message.Contains("variable.let", StringComparison.Ordinal)));
        Assert.IsNull(backend.Request, "Backend render must not run when target operations are unsupported.");
    }

    [TestMethod]
    public void Render_WhenTargetCapabilitiesRejectFeature_ShouldFailBeforeBackendRender()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            [],
            [],
            Enum.GetValues<ExecutionPortableSymbolPortability>(),
            Enum.GetValues<ExecutionPortableSymbolPortability>(),
            ExecutionOperationCatalog.AllOperationIds,
            [ExecutionSemanticsContract.Version1.Version],
            [
                ExecutionTargetFeatureKind.ConstantKind,
                ExecutionTargetFeatureKind.TypePortability
            ]);
        var backend = new CapturingFakeBackend(capabilities);
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(backend: backend);
        var plan = new ExecutionPlan(
            "Q_UnsupportedFeature",
            [],
            new ExecutionBlock(
            [
                new ExecutionLet(
                    new ExecutionVariable("value", typeof(int)),
                    new ExecutionBinary(
                        BinaryOpKind.Add,
                        new ExecutionLiteral(1, typeof(int)),
                        new ExecutionLiteral(2, typeof(int)),
                        typeof(int)))
            ]));
        var request = CreateMinimalRequest() with
        {
            ExecutionPlan = plan,
            OperationReport = ExecutionTargetOperationAnalyzer.Analyze(plan),
            FeatureReport = ExecutionTargetFeatureAnalyzer.Analyze(plan),
            RuntimeContract = CreateNoRuntimeRequirementsContract(plan)
        };

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = ExecutionTargetCatalog.Render(request);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedRequirement &&
            diagnostic.Message.Contains("feature 'binary:add'", StringComparison.Ordinal)));
        Assert.IsNull(backend.Request, "Backend render must not run when target features are unsupported.");
    }

    [TestMethod]
    public void Render_WhenTargetCapabilitiesRejectSemantics_ShouldFailBeforeBackendRender()
    {
        var backend = new CapturingFakeBackend(
            ExecutionTargetCapabilities.CreateForSemantics([ExecutionSemanticsContract.Version1.Version]));
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(backend: backend);
        var version2 = new ExecutionSemanticsContract(2, ExecutionSemanticsContract.Version1.Rules);
        var plan = new ExecutionPlan("Q_UnsupportedSemantics", [], new ExecutionBlock([]), semanticsContract: version2);
        var request = CreateMinimalRequest() with
        {
            ExecutionPlan = plan,
            SemanticsContract = version2,
            RuntimeContract = CreateNoRuntimeRequirementsContract(plan)
        };

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = ExecutionTargetCatalog.Render(request);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == TargetDiagnosticCodes.UnsupportedSemantics &&
            diagnostic.Message.Contains("version 2", StringComparison.Ordinal)));
        Assert.IsNull(backend.Request, "Backend render must not run when execution semantics are unsupported.");
    }

    [TestMethod]
    public void FakeNonClrTarget_WhenRealQueryUsesUnsupportedCallablePortability_ShouldFailBeforeRender()
    {
        var capabilities = ExecutionTargetCapabilities.Create(
            ExecutionTargetCapabilities.CSharpClr.SupportedRequirementKinds,
            ExecutionTargetCapabilities.CSharpClr.SupportedRuntimeRequirementKinds,
            Enum.GetValues<ExecutionPortableSymbolPortability>(),
            [ExecutionPortableSymbolPortability.Portable],
            ExecutionOperationCatalog.AllOperationIds,
            [ExecutionSemanticsContract.Version1.Version]);
        var backend = new CapturingFakeBackend(capabilities);
        var descriptor = TestOnlyExecutionTarget.CreateDescriptor(
            backend: backend,
            finalizer: new FakeFinalizer(),
            inspector: new FakeInspector(),
            createArtifactPackage: CreateFakePackage);

        using var registration = ExecutionTargetCatalog.UseTemporaryDescriptor(descriptor);

        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            "select ToUpper(d.Dummy) as UpperDummy from #system.dual() d",
            $"FakeNonClrRejectedCallable{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            TestExecutionTargetIds.TestOnlyNonClr);

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(backend.Request, "Backend render must not run for unsupported callable portability.");
        var diagnosticText = FormatDiagnostics(result.Diagnostics);
        StringAssert.Contains(diagnosticText, nameof(LibraryBase.ToUpper));
        StringAssert.Contains(diagnosticText, "[HostImport]");
    }

    private static TargetRenderRequest CreateMinimalRequest()
    {
        var plan = new ExecutionPlan("Q_FakeNonClr", [], new ExecutionBlock([]));
        var compatibilityReport = new ExecutionTargetCompatibilityReport([]);
        return new TargetRenderRequest
        {
            TargetId = TestExecutionTargetIds.TestOnlyNonClr,
            Purpose = TargetRenderPurpose.Execution,
            Profile = TargetRenderProfile.ExecutionFast,
            Identity = new TargetRenderIdentity("FakeNonClr"),
            Options = TargetRenderOptions.Empty,
            ScriptBinding = TargetScriptBindingContract.Empty,
            References = TargetReferenceInventory.Empty,
            ExecutionPlan = plan,
            ExecutionIrVersion = plan.ExecutionIrVersion,
            SemanticsContract = plan.SemanticsContract,
            OperationReport = ExecutionTargetOperationReport.Empty,
            FeatureReport = ExecutionTargetFeatureReport.Empty,
            CompatibilityReport = compatibilityReport,
            RuntimeContract = CreateNoRuntimeRequirementsContract(plan),
            HostAbiVersion = TargetContractVersions.HostAbi,
            BackendInputs = new EmptyTargetBackendRenderInputs(TestExecutionTargetIds.TestOnlyNonClr)
        };
    }

    private static TargetArtifactPackagingContext CreatePackagingContext(
        RenderedQueryArtifact renderedArtifact,
        ExecutableQueryArtifact executableArtifact,
        TargetRuntimeContract? runtimeContract = null,
        ExecutionTargetCompatibilityReport? compatibilityReport = null)
    {
        return new TargetArtifactPackagingContext(
            TestExecutionTargetIds.TestOnlyNonClr,
            "FakeNonClrPackage",
            "select d.Dummy from #system.dual() d",
            "options",
            renderedArtifact,
            executableArtifact,
            TargetArtifactSemanticFacts.Empty,
            ExecutionSemanticsContract.Version1,
            runtimeContract,
            runtimeContract is null || compatibilityReport is null
                ? null
                : ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(
                    compatibilityReport,
                    runtimeContract));
    }

    private static TargetRuntimeContract CreateNoRuntimeRequirementsContract(ExecutionPlan plan)
    {
        return new TargetRuntimeContract(
            plan.Identifier,
            [],
            [],
            [],
            new TargetNullBehaviorContract(
                UsesNullableValueTypes: false,
                UsesObjectNulls: false,
                UsesFieldNullabilityMetadata: false,
                Semantics: "none"),
            new TargetCancellationContract(
                RequiresCancellationToken: false,
                RequiresParallelCancellation: false),
            new TargetDiagnosticsContract(
                RequiresBuildDiagnostics: false,
                RequiresSourceDiagnostics: false,
                RequiresRuntimeExceptionDiagnostics: false),
            new TargetProfilingContract(
                SupportsSourceBoundaryProfiling: false,
                SupportsOperatorProfiling: false,
                SourceBoundaryCount: 0,
                OperatorCount: 0));
    }

    private static TargetArtifactPackage CreateFakePackage(TargetArtifactPackagingContext context)
    {
        var export = context.ExecutableArtifact as TargetExportArtifact ??
                     throw new InvalidOperationException("Fake target package creation requires a portable export artifact.");

        return TargetArtifactPackage.CreatePortableExportPackage(
            context.TargetId,
            "FakeNonClrExport",
            export,
            context.SemanticsContract,
            export.DiagnosticsMetadata,
            context.ExecutionIrVersion);
    }

    private static string FormatDiagnostics(IEnumerable<global::Musoq.Parser.Diagnostics.Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }

    private static void AssertFakeExportPackage(
        TargetArtifactPackage package,
        bool expectSourceAccessAbi,
        bool expectRowShapeAbi,
        bool expectDiagnosticsAbi = true)
    {
        Assert.AreEqual(TestExecutionTargetIds.TestOnlyNonClr, package.TargetId);
        Assert.AreEqual("FakeNonClrExport", package.ArtifactKind);
        Assert.AreEqual(nameof(TargetExportArtifact), package.ExecutableArtifactKind);
        Assert.AreEqual("fake-js", package.Metadata["language"]);
        Assert.HasCount(1, package.SourceFiles);
        Assert.HasCount(1, package.BinaryBlobs);
        Assert.HasCount(1, package.Entrypoints);
        Assert.AreEqual(
            expectSourceAccessAbi,
            package.RuntimeServices.Requires(TargetRuntimeServiceRequirementKind.SourceAccess));
        if (expectSourceAccessAbi)
        {
            Assert.IsTrue(package.HostAbiInventory.Requires(TargetHostAbiImportKind.SourceAccess));
            var sourceImport = package.HostAbiInventory.Imports.Single(import =>
                import.Kind == TargetHostAbiImportKind.SourceAccess);
            Assert.AreEqual(2, sourceImport.ContractVersion);
            Assert.AreEqual("source-access-v2", sourceImport.Contract);
            var sourceDetails = Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(sourceImport.Details);
            Assert.IsFalse(string.IsNullOrWhiteSpace(sourceDetails.SourceContextId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(sourceDetails.SchemaName));
            Assert.IsTrue(sourceImport.Attributes.ContainsKey("sourceContextId"));
            Assert.IsTrue(sourceImport.Attributes.ContainsKey("schemaName"));
        }

        if (expectRowShapeAbi)
        {
            Assert.IsTrue(package.HostAbiInventory.Requires(TargetHostAbiImportKind.RowShapeTransfer));
            var rowImport = package.HostAbiInventory.Imports.First(import =>
                import.Kind == TargetHostAbiImportKind.RowShapeTransfer);
            Assert.AreEqual(1, rowImport.ContractVersion);
            Assert.AreEqual("row-shape-transfer-v1", rowImport.Contract);
            var rowDetails = Assert.IsInstanceOfType<TargetRowShapeTransferAbiDetails>(rowImport.Details);
            Assert.IsTrue(rowDetails.FieldCount > 0);
            Assert.IsTrue(rowImport.Attributes.ContainsKey("fieldCount"));
        }

        if (expectDiagnosticsAbi)
        {
            Assert.IsTrue(package.HostAbiInventory.Requires(TargetHostAbiImportKind.Diagnostics));
            var diagnosticsImport = package.HostAbiInventory.Imports.Single(import =>
                import.Kind == TargetHostAbiImportKind.Diagnostics);
            var diagnosticsDetails = Assert.IsInstanceOfType<TargetDiagnosticsAbiDetails>(diagnosticsImport.Details);
            Assert.IsTrue(diagnosticsDetails.RequiresBuildDiagnostics);
            Assert.IsTrue(diagnosticsImport.Attributes.ContainsKey("requiresBuildDiagnostics"));
            Assert.IsTrue(diagnosticsImport.Attributes.ContainsKey("requiresSourceDiagnostics"));
            Assert.IsTrue(diagnosticsImport.Attributes.ContainsKey("requiresRuntimeExceptionDiagnostics"));
        }
    }

    private sealed record FakeRenderedQueryArtifact(
        string Source,
        QueryResultMode ResultMode,
        TargetHostAbiInventory HostAbiInventory)
        : RenderedQueryArtifact(TestExecutionTargetIds.TestOnlyNonClr);

    private sealed record FakeTargetRenderInputs(
        ExecutionTargetId TargetId,
        QueryResultMode QueryResultMode,
        int ParameterCount,
        int VariableCount,
        int ReferenceNameCount) : TargetBackendRenderInputs(TargetId);

    private sealed class FakeBackend : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } = ExecutionTargetCapabilities.CSharpClr;

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            return TargetRenderResult.Succeeded(
                new FakeRenderedQueryArtifact(
                    "export function run() { return []; }",
                    ResolveResultMode(request),
                    TargetHostAbiInventoryBuilder.Build(request.RuntimeContract)));
        }
    }

    private sealed class CapturingFakeBackend : IQueryExecutionBackend
    {
        public CapturingFakeBackend()
            : this(ExecutionTargetCapabilities.CSharpClr)
        {
        }

        public CapturingFakeBackend(ExecutionTargetCapabilities capabilities)
        {
            Capabilities = capabilities;
        }

        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; }

        public TargetRenderRequest? Request { get; private set; }

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            Request = request;
            return TargetRenderResult.Succeeded(
                new FakeRenderedQueryArtifact(
                    "export function run() { return []; }",
                    ResolveResultMode(request),
                    TargetHostAbiInventoryBuilder.Build(request.RuntimeContract)));
        }
    }

    private sealed class FakeFinalizer : IRenderedQueryFinalizer
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
        {
            var rendered = (FakeRenderedQueryArtifact)artifact;
            var export = TargetExportArtifact.Create(
                TargetId,
                sourceFiles:
                [
                    new TargetExportSourceFile("query.js", "javascript", rendered.Source)
                ],
                binaryBlobs:
                [
                    new TargetExportBinaryBlob("query.vm", [1, 2, 3], "application/octet-stream")
                ],
                entrypoints:
                [
                    new TargetRuntimeEntrypoint(
                        "run",
                        rendered.ResultMode == QueryResultMode.TypedEnumerable
                            ? TargetRuntimeEntrypointKind.TypedQuery
                            : TargetRuntimeEntrypointKind.TableQuery,
                        "run")
                ],
                runtimeServices: rendered.HostAbiInventory.CreateServiceRequirements(
                    TargetRuntimeServiceFulfillmentKind.HostImport),
                hostAbiInventory: rendered.HostAbiInventory,
                diagnosticsMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["language"] = "fake-js"
                });

            return new FakeFinalizationResult(export);
        }
    }

    private sealed class SelfContainedFakeFinalizer : IRenderedQueryFinalizer
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
        {
            var rendered = (FakeRenderedQueryArtifact)artifact;
            var export = TargetExportArtifact.Create(
                TargetId,
                sourceFiles:
                [
                    new TargetExportSourceFile("query.js", "javascript", rendered.Source)
                ],
                entrypoints:
                [
                    new TargetRuntimeEntrypoint("run", TargetRuntimeEntrypointKind.TableQuery, "run")
                ],
                runtimeServices: TargetRuntimeServiceRequirements.Empty,
                diagnosticsMetadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["language"] = "fake-js"
                });

            return new FakeFinalizationResult(export);
        }
    }

    private sealed record FakeFinalizationResult(ExecutableQueryArtifact Executable)
        : TargetFinalizationResult(
            TestExecutionTargetIds.TestOnlyNonClr,
            true,
            [],
            Executable);

    private sealed class FakeInspector : IRenderedQueryInspector
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
        {
            return new RenderedQueryInspection(
                TargetId,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["language"] = "fake-js"
                });
        }
    }

    private static QueryResultMode ResolveResultMode(TargetRenderRequest request)
    {
        return request.BackendInputs is FakeTargetRenderInputs inputs
            ? inputs.QueryResultMode
            : QueryResultMode.Table;
    }

    private sealed class FakeTypedOutput
    {
        public string? Dummy { get; init; }
    }
}
