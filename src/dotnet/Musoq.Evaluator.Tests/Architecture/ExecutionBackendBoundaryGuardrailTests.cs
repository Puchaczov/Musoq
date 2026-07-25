using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ExecutionBackendBoundaryGuardrailTests
{
    [TestMethod]
    public void TransformTree_ShouldDispatchExecutionRenderingThroughBackendRegistry()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var transformTreeExecutionIr = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Build",
            "TransformTree.ExecutionIr.cs");
        var text = File.ReadAllText(transformTreeExecutionIr);

        StringAssert.Contains(text, "ExecutionTargetCatalog.Render(renderRequest)");
        StringAssert.Contains(text, "ExecutionTargetCatalog.CreateRenderInputs(");
        StringAssert.Contains(text, "ExecutionTargetCatalog.CreateRenderBuildContribution(");
        StringAssert.Contains(text, "CreateTargetRenderRequest(");
        Assert.IsFalse(
            text.Contains("ResolveBackend(context.ExecutionTarget)", StringComparison.Ordinal) ||
            text.Contains("backend.Render(renderRequest)", StringComparison.Ordinal),
            "TransformTree must dispatch rendering through ExecutionTargetCatalog.Render so capability validation cannot be bypassed.");
        Assert.IsFalse(
            text.Contains("ExecutionTargetIds.CSharpClr", StringComparison.Ordinal) ||
            text.Contains("CSharpClrTargetComposition.CreateRenderInputs", StringComparison.Ordinal),
            "TransformTree must ask the target catalog for backend inputs instead of branching on CSharpClr.");
        Assert.IsFalse(
            text.Contains("new CSharpRenderer(", StringComparison.Ordinal),
            "TransformTree must dispatch through execution backends instead of constructing C# renderers directly.");
        Assert.IsFalse(
            text.Contains("RoslynSharedFactory.CreateCompilation(", StringComparison.Ordinal),
            "TransformTree must not own backend-specific compilation setup.");
        Assert.IsFalse(
            text.Contains("using Musoq.Targets.CSharpClr;", StringComparison.Ordinal) ||
            text.Contains("CSharpClrArtifactCompatibility", StringComparison.Ordinal),
            "TransformTree must consume target render build contributions instead of CSharpClr compatibility helpers.");
    }

    [TestMethod]
    public void QueryExecutionBackend_ShouldUseTargetRenderRequestAndResult()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var backendInterface = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "IQueryExecutionBackend.cs");
        var text = File.ReadAllText(backendInterface);

        StringAssert.Contains(text, "TargetRenderResult Render(TargetRenderRequest request)");
        Assert.IsFalse(
            text.Contains("SemanticBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("PlanningBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("ExecutionBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("TransformPipelineContext", StringComparison.Ordinal),
            "Execution backends must receive the target request contract rather than converter stage artifact containers.");

        var renderResultFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "TargetRenderResult.cs");
        var resultText = File.ReadAllText(renderResultFile);

        StringAssert.Contains(resultText, "RenderedQueryArtifact? Artifact");
        StringAssert.Contains(resultText, "IReadOnlyList<TargetDiagnostic> Diagnostics");
        StringAssert.Contains(resultText, "TargetRenderResult Succeeded(");
        StringAssert.Contains(resultText, "TargetRenderResult Failed(");
        Assert.IsFalse(
            resultText.Contains("QueryMethodRenderMetadata", StringComparison.Ordinal) ||
            resultText.Contains("OptimizationTrace", StringComparison.Ordinal) ||
            resultText.Contains("Musoq.Evaluator.IR.CodeGeneration", StringComparison.Ordinal) ||
            resultText.Contains("Musoq.Evaluator.IR.Optimization", StringComparison.Ordinal),
            "TargetRenderResult must be artifact-first; CSharp render metadata and readability traces belong on CSharp artifacts.");
    }

    [TestMethod]
    public void TargetAnalysisContracts_ShouldStayInExecutionSpiAndImplementationsInAnalysisPackage()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var evaluatorTargetingRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Targeting");
        var executionTargetingRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting");
        var analysisTargetingRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution.Analysis",
            "Targeting");
        var requiredContractFiles = new[]
        {
            "ExecutionTargetCapabilities.cs",
            "ExecutionTargetCompatibilityReport.cs",
            "ExecutionTargetReadinessProfile.cs",
            "ExecutionTargetReadinessReport.cs",
            "ExecutionTargetRequirement.cs",
            "ExecutionTargetRuntimeRequirementAnalyzer.cs",
            "TargetRuntimeContract.cs"
        };
        var requiredImplementationFiles = new[]
        {
            "ExecutionTargetCompatibilityAnalyzer.cs",
            "ExecutionTargetReadinessAnalyzer.cs",
            "TargetRuntimeContractBuilder.cs"
        };

        Assert.IsEmpty(
            Directory.Exists(evaluatorTargetingRoot)
                ? Directory.EnumerateFiles(evaluatorTargetingRoot, "*.cs", SearchOption.AllDirectories).ToArray()
                : [],
            "Target contracts and analyzer implementations must stay in target packages, not evaluator.");

        foreach (var fileName in requiredContractFiles)
        {
            var path = Path.Combine(executionTargetingRoot, fileName);
            Assert.IsTrue(File.Exists(path), $"{fileName} must live in the execution SPI targeting folder.");
            StringAssert.Contains(File.ReadAllText(path), "namespace Musoq.Targets.Execution;");
        }

        foreach (var fileName in requiredImplementationFiles)
        {
            var implementationPath = Path.Combine(analysisTargetingRoot, fileName);
            var oldSpiPath = Path.Combine(executionTargetingRoot, fileName);
            Assert.IsTrue(File.Exists(implementationPath), $"{fileName} must live in the execution analysis package.");
            Assert.IsFalse(File.Exists(oldSpiPath), $"{fileName} must not live in the execution SPI contract package.");
            StringAssert.Contains(File.ReadAllText(implementationPath), "namespace Musoq.Targets.Execution.Analysis;");
        }

        var evaluatorPortabilityRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Portability");
        foreach (var fileName in new[] { "ExecutionPortableSymbolFactory.cs", "ExecutionPortableSymbolCatalog.cs" })
        {
            var evaluatorPath = Path.Combine(evaluatorPortabilityRoot, fileName);
            var oldAnalysisPath = Path.Combine(analysisTargetingRoot, fileName);
            Assert.IsTrue(File.Exists(evaluatorPath), $"{fileName} must live with execution IR portability identity.");
            Assert.IsFalse(File.Exists(oldAnalysisPath), $"{fileName} must not remain in the analysis implementation package.");
            StringAssert.Contains(File.ReadAllText(evaluatorPath), "namespace Musoq.Evaluator.IR.Execution.Portability;");
        }
    }

    [TestMethod]
    public void CSharpClrTarget_ShouldNotReferenceExecutionAnalysisPackage()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var csharpTargetProject = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Musoq.Targets.CSharpClr.csproj");
        var text = File.ReadAllText(csharpTargetProject);

        Assert.IsFalse(
            text.Contains("Musoq.Targets.Execution.Analysis", StringComparison.Ordinal),
            "CSharpClr target must depend on the execution SPI contracts, not the converter-owned analysis implementation package.");
    }

    [TestMethod]
    public void TargetRenderRequest_ShouldCarryOptimizedExecutionPlanOnly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var requestFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "TargetRenderRequest.cs");
        var text = File.ReadAllText(requestFile);

        StringAssert.Contains(text, "ExecutionPlan ExecutionPlan");
        StringAssert.Contains(text, "ExecutionTargetCompatibilityReport CompatibilityReport");
        StringAssert.Contains(text, "TargetRuntimeContract RuntimeContract");
        StringAssert.Contains(text, "TargetRenderIdentity(string CompilationUnitName)");
        StringAssert.Contains(text, "TargetReferenceInventory References");
        StringAssert.Contains(text, "TargetBackendRenderInputs BackendInputs");
        Assert.IsFalse(
            text.Contains("AssemblyName", StringComparison.Ordinal) ||
            text.Contains("SafeNamespaceName", StringComparison.Ordinal),
            "Target-neutral render identity must not use CSharp assembly or namespace vocabulary.");
        Assert.IsFalse(
            text.Contains("PhysicalNode", StringComparison.Ordinal),
            "TargetRenderRequest must not expose physical-planning nodes to target backends.");
        Assert.IsFalse(
            text.Contains("ExecutionPlanBuildResult", StringComparison.Ordinal),
            "TargetRenderRequest must receive a resolved optimized execution plan, not build-stage result containers.");
        Assert.IsFalse(
            text.Contains("System.Reflection", StringComparison.Ordinal) ||
            text.Contains(" Type?", StringComparison.Ordinal) ||
            text.Contains("IReadOnlyList<Type>", StringComparison.Ordinal) ||
            text.Contains("IReadOnlyList<Assembly>", StringComparison.Ordinal),
            "TargetRenderRequest must keep CLR Type and Assembly inputs behind target-specific render inputs.");
    }

    [TestMethod]
    public void TargetRenderRequestCommonContracts_ShouldNotExposeClrReflectionTypes()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var requestFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "TargetRenderRequest.cs");
        var text = File.ReadAllText(requestFile);
        var reflectionTypePattern = new Regex(@"\b(?:Type|Assembly)\b|System\.Reflection", RegexOptions.CultureInvariant);

        Assert.IsFalse(
            reflectionTypePattern.IsMatch(text),
            "Common target render request contracts must not expose CLR Type or Assembly concepts; use target-specific render inputs.");

        var runtimeContractFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting",
            "TargetRuntimeContract.cs");
        var runtimeContractText = File.ReadAllText(runtimeContractFile);
        Assert.IsFalse(
            runtimeContractText.Contains("ExecutionTargetCompatibilityReport", StringComparison.Ordinal) ||
            runtimeContractText.Contains("CompatibilityReport", StringComparison.Ordinal),
            "TargetRenderRequest owns compatibility analysis; TargetRuntimeContract must not embed a duplicate report.");
    }

    [TestMethod]
    public void TargetRenderInputBuildContext_ShouldExposeOnlyDeliberateBuildFacts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var contextFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "TargetRenderInputBuildContext.cs");
        var text = File.ReadAllText(contextFile);

        StringAssert.Contains(text, "internal sealed record TargetRenderInputBuildContext");
        StringAssert.Contains(text, "CompilationOptions CompilationOptions");
        StringAssert.Contains(text, "QueryResultMode QueryResultMode");
        StringAssert.Contains(text, "TargetScriptBindingContract ScriptBinding");
        StringAssert.Contains(text, "TargetReferenceInventory References");
        StringAssert.Contains(text, "TargetRenderInputCompilerState CompilerState");
        Assert.IsFalse(
            text.Contains("Type? OutputType", StringComparison.Ordinal) ||
            text.Contains("IReadOnlyList<Type>", StringComparison.Ordinal) ||
            text.Contains("Assembly", StringComparison.Ordinal) ||
            text.Contains("Scope Scope", StringComparison.Ordinal) ||
            text.Contains("ScriptParameterDefinition", StringComparison.Ordinal) ||
            text.Contains("ScriptVariableDefinition", StringComparison.Ordinal) ||
            text.Contains("ReferenceAssemblies", StringComparison.Ordinal),
            "Common render input build context must not expose CLR/CSharp render facts.");
        Assert.IsFalse(
            text.Contains("TransformPipelineContext", StringComparison.Ordinal) ||
            text.Contains("SemanticBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("PlanningBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("ExecutionBuildArtifacts", StringComparison.Ordinal) ||
            text.Contains("BuildMetadataAndInferTypesVisitor", StringComparison.Ordinal) ||
            text.Contains("BuildMetadataAndInferTypesTraverseVisitor", StringComparison.Ordinal),
            "TargetRenderInputBuildContext must not pass broad converter stage artifacts or metadata visitors into target input factories.");

        Assert.IsFalse(
            text.Contains("Dictionary<string, object>", StringComparison.Ordinal) ||
            text.Contains("RequireTargetSpecificContext", StringComparison.Ordinal),
            "Descriptor input adaptation must not use string-keyed object service locators.");

        var compilerState = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "TargetRenderInputCompilerState.cs"));
        StringAssert.Contains(compilerState, "Type? OutputType");
        StringAssert.Contains(compilerState, "IReadOnlyList<Type> AdditionalReferenceTypes");
        StringAssert.Contains(compilerState, "Scope Scope");
        StringAssert.Contains(compilerState, "IReadOnlyList<Assembly> ReferenceAssemblies");

        var sharedTransform = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Build",
            "TransformTree.ExecutionIr.cs"));
        Assert.IsFalse(
            sharedTransform.Contains("CSharpClrRenderInputBuildContext", StringComparison.Ordinal) ||
            sharedTransform.Contains("CSharpClrArtifactCompatibility", StringComparison.Ordinal) ||
            sharedTransform.Contains("[\"CSharpClr\"]", StringComparison.Ordinal),
            "Shared render orchestration must not construct or key CSharpClr-specific adapter inputs.");

        var csharpComposition = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "CSharpClrTargetComposition.cs"));
        StringAssert.Contains(csharpComposition, "var compilerState = context.CompilerState;");
        StringAssert.Contains(csharpComposition, "new CSharpClrRenderInputs");

        var descriptorText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "ExecutionTargetDescriptor.cs"));
        StringAssert.Contains(descriptorText, "Func<TargetRenderInputBuildContext, TargetBackendRenderInputs>");
        Assert.IsFalse(
            descriptorText.Contains("BuildItems", StringComparison.Ordinal) ||
            descriptorText.Contains("SemanticBuildArtifacts", StringComparison.Ordinal) ||
            descriptorText.Contains("PlanningBuildArtifacts", StringComparison.Ordinal) ||
            descriptorText.Contains("ExecutionBuildArtifacts", StringComparison.Ordinal) ||
            descriptorText.Contains("BuildMetadataAndInferTypes", StringComparison.Ordinal),
            "Descriptor render input factories must receive only TargetRenderInputBuildContext, not broad converter bags or visitors.");

        var fakeTargetTests = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            "NonClrTargetPipelinePressureTests.cs"));
        var fakeInputsStart = fakeTargetTests.IndexOf(
            "private sealed record FakeTargetRenderInputs",
            StringComparison.Ordinal);
        var fakeInputsEnd = fakeTargetTests.IndexOf(
            "private sealed class FakeBackend",
            StringComparison.Ordinal);
        Assert.IsTrue(fakeInputsStart >= 0 && fakeInputsEnd > fakeInputsStart);
        var fakeInputsText = fakeTargetTests[fakeInputsStart..fakeInputsEnd];
        Assert.IsFalse(
            fakeInputsText.Contains("Type", StringComparison.Ordinal) ||
            fakeInputsText.Contains("Assembly", StringComparison.Ordinal) ||
            fakeInputsText.Contains("Scope", StringComparison.Ordinal) ||
            fakeInputsText.Contains("ReferenceAssemblies", StringComparison.Ordinal),
            "Fake non-CLR render inputs must not consume CLR compiler facts.");
    }

    [TestMethod]
    public void CSharpRendererConstruction_ShouldStayInsideCSharpClrBackend()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var csharpBackend = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "CSharpClrExecutionBackend.cs");
        var text = File.ReadAllText(csharpBackend);

        StringAssert.Contains(text, "new CSharpRenderer(");
        StringAssert.Contains(text, "ExecutionTargetIds.CSharpClr");
    }

    [TestMethod]
    public void HighLevelCSharpRenderer_ShouldLiveInCSharpClrTargetPackage()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Rendering",
            "CodeGeneration");
        var evaluatorCodeGenerationRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "CodeGeneration");

        Assert.IsTrue(File.Exists(Path.Combine(rendererRoot, "CSharpRenderer.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(rendererRoot, "RenderContext.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(rendererRoot, "CompiledQueryClassRenderer.cs")));
        Assert.IsEmpty(
            Directory.EnumerateFiles(evaluatorCodeGenerationRoot, "CSharpRenderer*.cs").ToArray(),
            "High-level generated C# renderer partials must stay in Musoq.Targets.CSharpClr.");
    }

    [TestMethod]
    public void CSharpQueryCodeGeneration_ShouldBeOwnedByCSharpClrTarget()
    {
        var evaluatorTypes = typeof(PhysicalToExecutionPlanBuilder).Assembly.GetTypes();
        var evaluatorCodeGenerationTypes = evaluatorTypes
            .Where(type => type.Namespace?.StartsWith(
                "Musoq.Evaluator.Visitors.CodeGeneration",
                StringComparison.Ordinal) == true)
            .ToArray();
        var targetTypes = typeof(CompiledQueryClassRenderer).Assembly.GetTypes();
        var targetCodeGenerationTypes = targetTypes
            .Where(type => type.Namespace?.StartsWith(
                "Musoq.Targets.CSharpClr.Rendering.CodeGeneration",
                StringComparison.Ordinal) == true)
            .ToArray();

        Assert.IsEmpty(
            evaluatorCodeGenerationTypes,
            "Evaluator must not own C# query-rendering helper types: " +
            string.Join(", ", evaluatorCodeGenerationTypes.Select(type => type.FullName)));
        Assert.IsNotEmpty(targetCodeGenerationTypes);
        Assert.AreSame(typeof(CompiledQueryClassRenderer).Assembly, typeof(ClassEmitter).Assembly);
        Assert.AreSame(typeof(CompiledQueryClassRenderer).Assembly, typeof(MethodDeclarationHelper).Assembly);
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_ShouldLiveInCSharpClrTargetPackage()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetRendererRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Rendering",
            "Execution");
        var evaluatorExecutionRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution");

        Assert.IsTrue(File.Exists(Path.Combine(targetRendererRoot, "ExecutionCSharpRenderer.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(targetRendererRoot, "ExecutionCSharpRenderer.NodeDispatch.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(targetRendererRoot, "Rendering", "ExecutionCSharpRenderer.ExpressionRenderer.cs")));
        Assert.IsEmpty(
            Directory.EnumerateFiles(evaluatorExecutionRoot, "ExecutionCSharpRenderer*.cs", SearchOption.AllDirectories).ToArray(),
            "Execution IR to generated C# lowering must stay in Musoq.Targets.CSharpClr.");
    }

    [TestMethod]
    public void GeneratedCSharpRendererTypes_ShouldStayInsideCSharpClrTargetProductionSources()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.CSharpClr");
        const string allowedPrefix = "src/dotnet/Musoq.Targets.CSharpClr/";
        var rendererTypeNames = new[]
        {
            "CSharpRenderer",
            "ExecutionCSharpRenderer"
        };

        foreach (var rendererTypeName in rendererTypeNames)
        {
            var offenders = files
                .Where(file => File.ReadAllText(file).Contains(rendererTypeName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .Where(relativePath => !relativePath.StartsWith(allowedPrefix, StringComparison.Ordinal))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{rendererTypeName} must stay inside the CSharpClr target package: {string.Join(", ", offenders)}");
        }
    }

    [TestMethod]
    public void TurnQueryIntoRunnableCode_ShouldDispatchExecutableFinalizationThroughTargetCatalog()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var turnQueryIntoRunnableCode = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "Build",
            "TurnQueryIntoRunnableCode.cs");
        var text = File.ReadAllText(turnQueryIntoRunnableCode);

        StringAssert.Contains(text, "ExecutionTargetCatalog.CreateFinalizationOptions(");
        StringAssert.Contains(text, "new TargetFinalizationOptionsContext(emitPdb)");
        StringAssert.Contains(text, "ExecutionTargetCatalog.FinalizeArtifact(rendering.Artifact, options)");
        Assert.IsFalse(text.Contains(".Finalize(rendering.Artifact, options)", StringComparison.Ordinal));
        StringAssert.Contains(text, "CompilationBuildArtifacts.From(");
        StringAssert.Contains(text, "artifacts.FinalizationResult.Success");
        Assert.IsFalse(
            text.Contains(".Emit(", StringComparison.Ordinal),
            "TurnQueryIntoRunnableCode must dispatch through finalizers instead of emitting Roslyn compilations directly.");
        Assert.IsFalse(
            text.Contains("EmitResult", StringComparison.Ordinal),
            "TurnQueryIntoRunnableCode must inspect target-neutral finalization results instead of Roslyn EmitResult.");
    }

    [TestMethod]
    public void TargetNeutralFinalizerContract_ShouldNotExposePdbFlag()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var finalizerContract = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "IRenderedQueryFinalizer.cs");
        var text = File.ReadAllText(finalizerContract);

        StringAssert.Contains(text, "TargetFinalizationOptions options");
        Assert.IsFalse(
            text.Contains("emitPdb", StringComparison.Ordinal) ||
            text.Contains("EmitPdb", StringComparison.Ordinal),
            "Target-neutral finalizer contracts must not expose CSharpClr PDB options.");
    }

    [TestMethod]
    public void TargetFinalizationOptionsDispatch_ShouldUseTargetOwnedContext()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = new[]
        {
            Path.Combine(
                repositoryRoot,
                "src",
                "dotnet",
                "Musoq.Converter",
                "ExecutionTargets",
                "ExecutionTargetDescriptor.cs"),
            Path.Combine(
                repositoryRoot,
                "src",
                "dotnet",
                "Musoq.Converter",
                "ExecutionTargets",
                "ExecutionTargetCatalog.cs")
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            StringAssert.Contains(text, "TargetFinalizationOptionsContext");
            Assert.IsFalse(
                text.Contains("Func<bool, TargetFinalizationOptions>", StringComparison.Ordinal) ||
                Regex.IsMatch(text, @"CreateFinalizationOptions\s*\(\s*ExecutionTargetId\s+targetId,\s*bool\b", RegexOptions.CultureInvariant),
                $"{RepositorySourceScan.ToRelative(repositoryRoot, file)} must not expose CSharp-shaped PDB booleans for target finalization options.");
        }
    }

    [TestMethod]
    public void TargetSpecificBackendAndFinalizerConstruction_ShouldStayInsideComposition()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");

        AssertConstructorOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "new CSharpClrExecutionBackend(",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertConstructorOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "new CSharpClrRenderedQueryFinalizer(",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertConstructorOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "new ClrAssemblyExecutableActivator(",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertConstructorOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "new CSharpRenderedQueryInspector(",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");

        var globalUsingOffenders = converterFiles
            .Where(file => File.ReadAllText(file).Contains(
                "global using Musoq.Targets.CSharpClr",
                StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();
        Assert.IsEmpty(
            globalUsingOffenders,
            "Concrete CSharpClr target namespace must not be globally imported into converter: " +
            string.Join(", ", globalUsingOffenders));
    }

    [TestMethod]
    public void ConcreteCSharpTargetReferences_ShouldStayInCompositionAndCompatibilityOwners()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var csharpNamespaceAllowed = new[]
        {
            "src/dotnet/Musoq.Converter/Build/RenderingBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/Build/Stages/CompilationBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Executable.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Rendering.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrCompiledArtifactLoader.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.Artifacts.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.ExecutionCompilationCache.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.Runnables.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.TypedArtifact.cs"
        };

        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "using Musoq.Targets.CSharpClr;",
            csharpNamespaceAllowed);
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "CSharpClrRenderInputs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "CSharpClrExecutionBackend",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "CSharpClrRenderedQueryFinalizer",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "CSharpRenderedQueryInspector",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs");
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "ClrAssemblyExecutableActivator",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.Runnables.cs");
        AssertTextOnlyAppearsIn(
            repositoryRoot,
            converterFiles,
            "CSharpClrArtifactCompatibility",
            "src/dotnet/Musoq.Converter/Build/RenderingBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/Build/Stages/CompilationBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Executable.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Rendering.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrCompiledArtifactLoader.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrTargetComposition.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.Artifacts.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.ExecutionCompilationCache.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.Runnables.cs",
            "src/dotnet/Musoq.Converter/InstanceCreator.TypedArtifact.cs");
    }

    [TestMethod]
    public void CompiledArtifactSupport_ShouldUseTargetPackagesForMetadata()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var artifactSupport = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "CompiledQueryArtifactSupport.cs");
        var text = File.ReadAllText(artifactSupport);

        StringAssert.Contains(text, "TargetArtifactPackage package");
        Assert.IsFalse(
            text.Contains("CSharpRenderedQueryArtifact", StringComparison.Ordinal) ||
            text.Contains("CSharpClrArtifactCompatibility", StringComparison.Ordinal) ||
            text.Contains("RequireRenderedArtifact", StringComparison.Ordinal),
            "Compiled artifact support must consume target packages and not directly require CSharp rendered artifacts.");
    }

    [TestMethod]
    public void PublicCSharpArtifactLoading_ShouldStayInsideCSharpClrArtifactLoader()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter");
        var allowed = "src/dotnet/Musoq.Converter/ExecutionTargets/CSharpClrCompiledArtifactLoader.cs";

        AssertTextOnlyAppearsIn(repositoryRoot, converterFiles, "LoadFromStream(", allowed);
        AssertTextOnlyAppearsIn(repositoryRoot, converterFiles, "CompiledQueryArtifactAssemblyLoadContext", allowed);

        var packageCompilerText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "InstanceCreator.Artifacts.cs"));
        StringAssert.Contains(packageCompilerText, "CompileTargetPackageWithDiagnostics");
        Assert.IsFalse(
            packageCompilerText.Contains("LoadFromStream(", StringComparison.Ordinal) ||
            packageCompilerText.Contains("AssemblyLoadContext", StringComparison.Ordinal),
            "Target-neutral package compilation must not perform CLR assembly loading or type lookup.");
    }

    [TestMethod]
    public void ExecutionCompilationCache_ShouldStoreTargetExecutableArtifacts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var cacheFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "InstanceCreator.ExecutionCompilationCache.cs");
        var text = File.ReadAllText(cacheFile);

        StringAssert.Contains(text, "ExecutionTargetId ExecutionTarget");
        StringAssert.Contains(text, "ExecutableQueryArtifact executableArtifact");
        StringAssert.Contains(text, "public ExecutionTargetId TargetId");
        StringAssert.Contains(text, "public ExecutableQueryArtifact ExecutableArtifact");
        Assert.IsFalse(
            Regex.IsMatch(
                text,
                @"StoreExecutionCompilation\s*\([^)]*\bType\s+\w+",
                RegexOptions.Singleline | RegexOptions.CultureInvariant),
            "Execution compilation cache storage must accept target executable artifacts, not bare CLR Type values.");
    }

    [TestMethod]
    public void ClrLoadedExecutableArtifact_ShouldBeCreatedOnlyByCSharpActivator()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var allowedFiles = new[]
        {
            "src/dotnet/Musoq.Targets.CSharpClr/ClrAssemblyExecutableActivator.cs",
            "src/dotnet/Musoq.Converter.Tests/ClrAssemblyExecutableActivatorTests.cs"
        }.ToHashSet(StringComparer.Ordinal);
        var forbiddenConstruction = "new " + "ClrLoadedExecutableArtifact";

        var violations = Directory
            .GetFiles(Path.Combine(repositoryRoot, "src", "dotnet"), "*.cs", SearchOption.AllDirectories)
            .Select(path => (Path: path, RelativePath: RepositorySourceScan.ToRelative(repositoryRoot, path)))
            .Where(item => !allowedFiles.Contains(item.RelativePath))
            .Where(item => File.ReadAllText(item.Path).Contains(forbiddenConstruction, StringComparison.Ordinal))
            .Select(item => item.RelativePath)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Loaded CLR executable artifacts must be created through the CSharpClr activation phase. Violations: " +
            string.Join(", ", violations));
    }

    [TestMethod]
    public void TargetArtifactPackaging_ShouldStayDescriptorOwnedAndPackageBased()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var descriptorFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "ExecutionTargetDescriptor.cs");
        var catalogFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "ExecutionTargetCatalog.cs");
        var packageFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting",
            "TargetArtifactPackage.cs");
        var redundantPackageFactoryFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting",
            "TargetArtifactPackageFactory.cs");
        var csharpPackageFactoryFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "CSharpClrTargetPackageFactory.cs");
        var artifactSupport = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "CompiledQueryArtifactSupport.cs");

        StringAssert.Contains(File.ReadAllText(descriptorFile), "Func<TargetArtifactPackagingContext, TargetArtifactPackage>");
        StringAssert.Contains(File.ReadAllText(descriptorFile), "Func<RenderedQueryArtifact, RenderedArtifactBuildContribution>");
        Assert.IsFalse(
            File.ReadAllText(descriptorFile).Contains("CreateDefaultArtifactPackage", StringComparison.Ordinal),
            "Target artifact packaging must be an explicit descriptor phase, not a silent default.");
        StringAssert.Contains(File.ReadAllText(catalogFile), "CreateArtifactPackage(");
        StringAssert.Contains(File.ReadAllText(catalogFile), "\"artifact packaging\"");
        Assert.IsFalse(
            File.Exists(redundantPackageFactoryFile),
            "Portable export package creation should use TargetArtifactPackage.CreatePortableExportPackage directly; do not add a wrapper that bypasses descriptor-owned packaging.");
        var packageText = File.ReadAllText(packageFile);
        StringAssert.Contains(packageText, "TargetHostAbiInventory HostAbiInventory");
        StringAssert.Contains(packageText, "private TargetArtifactPackage(");
        StringAssert.Contains(packageText, "CreateValidated(");
        StringAssert.Contains(packageText, "CreatePortableExportPackage");
        Assert.IsFalse(
            packageText.Contains("CreateClrAssemblyPackage", StringComparison.Ordinal) ||
            packageText.Contains("CSharpClr", StringComparison.Ordinal) ||
            packageText.Contains("CLR assembly", StringComparison.Ordinal) ||
            packageText.Contains("GeneratedCodeSha256", StringComparison.Ordinal),
            "TargetArtifactPackage must stay target-neutral; CSharpClr package validation belongs in converter-owned CSharp support.");
        var csharpPackageFactoryText = File.ReadAllText(csharpPackageFactoryFile);
        StringAssert.Contains(csharpPackageFactoryText, "CreateClrAssemblyPackage");
        StringAssert.Contains(csharpPackageFactoryText, "TargetArtifactPackage.CreateValidated(");
        StringAssert.Contains(csharpPackageFactoryText, "ExecutionTargetIds.CSharpClr");
        StringAssert.Contains(csharpPackageFactoryText, "required CLR assembly blob");
        StringAssert.Contains(File.ReadAllText(artifactSupport), "CreateCompiledArtifactFromPackage");
        StringAssert.Contains(File.ReadAllText(artifactSupport), "currently support only");
        var artifactCompilerText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "InstanceCreator.Artifacts.cs"));
        StringAssert.Contains(artifactCompilerText, "CompileTargetPackageWithDiagnostics");
        Assert.IsFalse(
            artifactCompilerText.Contains("ExecutionTargetCompatibilityAnalyzer", StringComparison.Ordinal) ||
            artifactCompilerText.Contains("TargetRuntimeContractBuilder", StringComparison.Ordinal) ||
            artifactCompilerText.Contains("ExecutionTargetReadinessAnalyzer", StringComparison.Ordinal),
            "Target artifact packaging must reuse render-time target analysis instead of recomputing it.");

        var packagingContext = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "TargetArtifactPackagingContext.cs"));
        StringAssert.Contains(packagingContext, "TargetArtifactSemanticFacts SemanticFacts");
        Assert.IsFalse(
            packagingContext.Contains("BuildItems", StringComparison.Ordinal),
            "TargetArtifactPackagingContext must carry explicit semantic facts, not the full BuildItems bag.");
        var semanticFacts = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "TargetArtifactSemanticFacts.cs"));
        StringAssert.Contains(semanticFacts, "PortableOutputTypeName");
        StringAssert.Contains(semanticFacts, "PortableScriptParameters");
        StringAssert.Contains(semanticFacts, "PortableScriptVariables");
        StringAssert.Contains(semanticFacts, "PortableUsedColumns");
        StringAssert.Contains(semanticFacts, "PortablePipelineInferredColumns");
        StringAssert.Contains(semanticFacts, "PortableSourcePlanSignatures");
        var artifactSupportText = File.ReadAllText(artifactSupport);
        StringAssert.Contains(artifactSupportText, "facts.PortableOutputTypeName");
        StringAssert.Contains(artifactSupportText, "facts.PortableScriptParameters");
        StringAssert.Contains(artifactSupportText, "facts.PortableUsedColumns");
        Assert.IsFalse(
            artifactSupportText.Contains("facts.OutputType", StringComparison.Ordinal) ||
            artifactSupportText.Contains("facts.ScriptParameterDefinitions", StringComparison.Ordinal) ||
            artifactSupportText.Contains("facts.ScriptVariableDefinitions", StringComparison.Ordinal) ||
            artifactSupportText.Contains("facts.UsedColumns", StringComparison.Ordinal) ||
            artifactSupportText.Contains("facts.PipelineInferredColumns", StringComparison.Ordinal) ||
            artifactSupportText.Contains("facts.SourcePlanRequestsPerSchema", StringComparison.Ordinal),
            "Semantic hash computation should prefer portable semantic fact views over CLR/schema compatibility fields.");

        var csharpE2eTests = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            "TargetPipelineEndToEndTests.cs"));
        Assert.IsFalse(
            csharpE2eTests.Contains("TargetArtifactPackagingContext", StringComparison.Ordinal),
            "E2E target tests should exercise package compilation through InstanceCreator, not manual packaging contexts.");
        var fakeTargetTests = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            "NonClrTargetPipelinePressureTests.cs"));
        StringAssert.Contains(fakeTargetTests, "FakeNonClrTarget_WhenQueryCallsPluginMethod_ShouldPackagePluginAbi");
        StringAssert.Contains(fakeTargetTests, "CompileTargetPackageWithDiagnostics");
        StringAssert.Contains(fakeTargetTests, "TargetPluginInvocationAbiDetails");
        StringAssert.Contains(fakeTargetTests, "ExecutionPortableSymbolPortability.ClrOnly");
        Assert.IsFalse(
            fakeTargetTests.Contains("context.SemanticFacts.OutputType", StringComparison.Ordinal) ||
            fakeTargetTests.Contains("context.SemanticFacts.UsedColumns", StringComparison.Ordinal) ||
            fakeTargetTests.Contains("context.SemanticFacts.PipelineInferredColumns", StringComparison.Ordinal) ||
            fakeTargetTests.Contains("context.SemanticFacts.SourcePlanRequestsPerSchema", StringComparison.Ordinal),
            "Fake non-CLR package factories must not read CLR/schema-shaped semantic facts.");
    }

    [TestMethod]
    public void PortableSymbols_ShouldExposePortabilityAndReadinessShouldRejectClrOnlySymbols()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var portabilityFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "ExecutionPortableSymbolPortability.cs");
        var typeSymbolFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "ExecutionPortableTypeDescriptor.cs");
        var callableSymbolFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "ExecutionPortableCallableDescriptor.cs");
        var readinessAnalyzer = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution.Analysis",
            "Targeting",
            "ExecutionTargetReadinessAnalyzer.cs");

        var portabilityText = File.ReadAllText(portabilityFile);
        StringAssert.Contains(portabilityText, "Portable");
        StringAssert.Contains(portabilityText, "HostImport");
        StringAssert.Contains(portabilityText, "ClrOnly");
        Assert.IsFalse(portabilityText.Contains("Unknown", StringComparison.Ordinal));
        StringAssert.Contains(File.ReadAllText(typeSymbolFile), "ExecutionPortableSymbolPortability Portability");
        StringAssert.Contains(File.ReadAllText(typeSymbolFile), "string PortabilityReason");
        StringAssert.Contains(File.ReadAllText(callableSymbolFile), "ExecutionPortableSymbolPortability Portability");
        StringAssert.Contains(File.ReadAllText(callableSymbolFile), "string PortabilityReason");
        var readinessAnalyzerText = File.ReadAllText(readinessAnalyzer);
        StringAssert.Contains(readinessAnalyzerText, "AddUnsupportedSymbolPortabilityIssues");
        StringAssert.Contains(readinessAnalyzerText, "SupportsTypeSymbolPortability");
        StringAssert.Contains(readinessAnalyzerText, "SupportsCallableSymbolPortability");

        var symbolFactoryText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Portability",
            "ExecutionPortableSymbolFactory.cs"));
        Assert.IsFalse(
            symbolFactoryText.Contains(".Namespace", StringComparison.Ordinal) ||
            symbolFactoryText.Contains("StartsWith(", StringComparison.Ordinal),
            "Portable symbol classification must use the explicit catalog, not namespace-prefix heuristics.");
        StringAssert.Contains(File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Portability",
            "ExecutionPortableSymbolCatalog.cs")), "ExecutionPortableSymbolCatalog");
    }

    [TestMethod]
    public void TargetHostAbiInventory_ShouldStayInPureContractsAndExecutionSpiBuilder()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var inventoryFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "TargetHostAbiInventory.cs");
        var builderFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting",
            "TargetHostAbiInventoryBuilder.cs");
        var detailsFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "TargetHostAbiImportDetails.cs");

        StringAssert.Contains(File.ReadAllText(inventoryFile), "TargetHostAbiImport");
        StringAssert.Contains(File.ReadAllText(inventoryFile), "TargetHostAbiInventory");
        StringAssert.Contains(File.ReadAllText(inventoryFile), "int ContractVersion");
        StringAssert.Contains(File.ReadAllText(inventoryFile), "TargetHostAbiImportDetails Details");
        StringAssert.Contains(File.ReadAllText(inventoryFile), "IReadOnlyDictionary<string, string> Attributes");
        var detailsText = File.ReadAllText(detailsFile);
        StringAssert.Contains(detailsText, "TargetSourceAccessAbiDetails");
        StringAssert.Contains(detailsText, "TargetPluginInvocationAbiDetails");
        Assert.IsFalse(detailsText.Contains("TargetClrOnlySymbolAbiDetails", StringComparison.Ordinal));
        StringAssert.Contains(File.ReadAllText(inventoryFile), "CreateCustom");
        StringAssert.Contains(detailsText, "ExecutionPortableSymbolPortability? SourcePortability");
        StringAssert.Contains(detailsText, "ExecutionPortableSymbolPortability? TypePortability");
        Assert.IsFalse(
            detailsText.Contains("string SourcePortability", StringComparison.Ordinal) ||
            detailsText.Contains("string TypePortability", StringComparison.Ordinal),
            "Target host ABI portability details must stay typed, not stringly typed.");
        var builderText = File.ReadAllText(builderFile);
        StringAssert.Contains(builderText, "TargetRuntimeContract runtimeContract");
        StringAssert.Contains(builderText, "TargetHostAbiImportKind.SourceAccess");
        StringAssert.Contains(builderText, "TargetHostAbiImportKind.PluginInvocation");
        StringAssert.Contains(builderText, "new TargetSourceAccessAbiDetails");
        StringAssert.Contains(builderText, "new TargetPluginInvocationAbiDetails");
        Assert.IsFalse(File.Exists(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Abstractions",
            "TargetHostImport.cs")), "The legacy string-only host import model must not return.");
        Assert.IsFalse(
            builderText.Contains("ExecutionTargetReadinessReport", StringComparison.Ordinal) ||
            builderText.Contains("ClrOnlySymbol", StringComparison.Ordinal),
            "Readiness blockers are target-policy diagnostics, not host ABI imports.");
        Assert.IsFalse(
            builderText.Contains("CreateCustom", StringComparison.Ordinal) ||
            builderText.Contains("TargetCustomAbiImportDetails", StringComparison.Ordinal),
            "Production ABI inventory builder must use typed import details, not custom/raw ABI imports.");
    }

    [TestMethod]
    public void ClrAssemblyLoadingAndActivatorUsage_ShouldStayInsideClrExecutableActivator()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetBoundaryFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Targets.CSharpClr");
        var allowed = "src/dotnet/Musoq.Targets.CSharpClr/ClrAssemblyExecutableActivator.cs";

        AssertTextOnlyAppearsIn(repositoryRoot, targetBoundaryFiles, "Assembly.Load(", allowed);
        AssertTextOnlyAppearsIn(repositoryRoot, targetBoundaryFiles, "Activator.CreateInstance", allowed);
    }

    [TestMethod]
    public void ClrActivationUsage_ShouldStayInExplicitOwnerPaths()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.CSharpClr");

        AssertTextOnlyAppearsIn(
            repositoryRoot,
            productionFiles,
            "Assembly.Load(",
            "src/dotnet/Musoq.Evaluator/Build/DefaultAssemblyLoader.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/ClrAssemblyExecutableActivator.cs");

        AssertTextOnlyAppearsIn(
            repositoryRoot,
            productionFiles,
            "Activator.CreateInstance",
            "src/dotnet/Musoq.Evaluator/Build/InterpreterCompilationUnit.cs",
            "src/dotnet/Musoq.Evaluator/Helpers/SafeArrayAccess.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.WindowAggregateKernels.cs",
            "src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.Diagnostics.CoreAndSymbols.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/ClrAssemblyExecutableActivator.cs");
    }

    [TestMethod]
    public void ClrActivationUsage_ShouldMatchExactOwnerPaths()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.Execution",
            "Musoq.Targets.CSharpClr");

        AssertTextAppearsExactlyIn(
            repositoryRoot,
            productionFiles,
            "Activator.CreateInstance",
            "src/dotnet/Musoq.Evaluator/Build/InterpreterCompilationUnit.cs",
            "src/dotnet/Musoq.Evaluator/Helpers/SafeArrayAccess.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.WindowAggregateKernels.cs",
            "src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.Diagnostics.CoreAndSymbols.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/ClrAssemblyExecutableActivator.cs");
    }

    [TestMethod]
    public void RoslynEmit_ShouldStayInsideCSharpClrFinalizer()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetBoundaryFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Targets.CSharpClr");

        AssertTextOnlyAppearsIn(
            repositoryRoot,
            targetBoundaryFiles,
            ".Compilation.Emit(",
            "src/dotnet/Musoq.Targets.CSharpClr/CSharpClrRenderedQueryFinalizer.cs");
    }

    [TestMethod]
    public void RoslynCompilationEmit_ShouldStayInExplicitCompilerOwners()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.CSharpClr");

        AssertRegexOnlyAppearsIn(
            repositoryRoot,
            productionFiles,
            new Regex(@"(?:\b[A-Za-z_][A-Za-z0-9_]*\.Compilation|_compilation)\.Emit\(", RegexOptions.CultureInvariant),
            "src/dotnet/Musoq.Evaluator/Build/InterpreterCompilationUnit.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/CSharpClrRenderedQueryFinalizer.cs");
    }

    [TestMethod]
    public void EvaluatorRoslynUsage_ShouldStayInDocumentedExceptionFiles()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var evaluatorFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");
        var allowed = new[]
        {
            "src/dotnet/Musoq.Evaluator/Build/DefaultInterpreterReferenceProvider.cs",
            "src/dotnet/Musoq.Evaluator/Build/IInterpreterReferenceProvider.cs",
            "src/dotnet/Musoq.Evaluator/Build/InterpreterCompilationUnit.cs",
            "src/dotnet/Musoq.Evaluator/Helpers/SyntaxHelper.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/DefaultMetadataReferenceCache.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/EvaluatorRuntimeEnvironment.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/ICSharpCompilationFactory.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/IMetadataReferenceCache.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/IRuntimeReferenceProvider.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/MetadataReferenceCache.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/RoslynCompilationFactory.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/RoslynSharedFactory.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/RuntimeLibraries.cs",
            "src/dotnet/Musoq.Evaluator/Runtime/RuntimeReferenceProvider.cs",
        }.ToHashSet(StringComparer.Ordinal);

        var actual = evaluatorFiles
            .Where(file => File.ReadAllText(file).Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var unexpected = actual
            .Where(path => !allowed.Contains(path))
            .ToArray();
        var staleAllowListEntries = allowed
            .Where(path => !actual.Contains(path, StringComparer.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            unexpected,
            "Evaluator Roslyn usage must stay in documented compiler/interpreter/readability/syntax bridge files: " +
            string.Join(", ", unexpected));
        Assert.IsEmpty(
            staleAllowListEntries,
            "Evaluator Roslyn allow-list contains stale entries; keep the budget exact: " +
            string.Join(", ", staleAllowListEntries));
    }

    [TestMethod]
    public void EvaluatorExecutionIr_ShouldNotUseRoslynSyntaxFactories()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var evaluatorIrFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR",
            "*.cs");
        var offenders = evaluatorIrFiles
            .Where(file => File.ReadAllText(file).Contains("SyntaxFactory", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Evaluator IR must not build Roslyn syntax directly; generated C# syntax factories belong to Musoq.Targets.CSharpClr or legacy compiler visitors: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void CSharpCompilationAccess_ShouldStayInsideCSharpArtifactCompatibilityPaths()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var targetBoundaryFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Targets.CSharpClr");
        var allowed = new[]
        {
            "src/dotnet/Musoq.Converter/Build/RenderingBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Rendering.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration/CompilationContextManager.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/CSharpClrArtifactCompatibility.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/RenderedQueryArtifact.cs"
        };

        AssertTextOnlyAppearsIn(repositoryRoot, targetBoundaryFiles, "CSharpCompilation", allowed);
    }

    [TestMethod]
    public void ConverterRoslynUsage_ShouldStayInCSharpCompatibilityShims()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var allowed = new[]
        {
            "src/dotnet/Musoq.Converter/Build/BuildItems.cs",
            "src/dotnet/Musoq.Converter/Build/RenderingBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/Build/Stages/CompilationBuildArtifacts.cs",
            "src/dotnet/Musoq.Converter/ExecutionTargets/BuildItems.Rendering.cs"
        }.ToHashSet(StringComparer.Ordinal);
        var offenders = converterFiles
            .Where(file => File.ReadAllText(file).Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .Where(path => !allowed.Contains(path))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Converter Roslyn usage must stay in CSharpClr compatibility shim files: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void CSharpArtifactTypeAssumptions_ShouldStayBehindCSharpCompatibilityHelper()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var forbidden = new[]
        {
            "CSharpRenderedQueryArtifact",
            "ClrAssemblyExecutableArtifact",
            "CSharpClrFinalizationResult"
        };

        foreach (var typeName in forbidden)
        {
            var offenders = converterFiles
                .Where(file => File.ReadAllText(file).Contains(typeName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{typeName} must be accessed from Musoq.Converter only through CSharpClrArtifactCompatibility: {string.Join(", ", offenders)}");
        }
    }

    [TestMethod]
    public void ExecutionTargetSwitches_ShouldStayInsideTargetCatalog()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var allowed = new[]
        {
            "src/dotnet/Musoq.Converter/ExecutionTargets/ExecutionTargetCatalog.cs"
        };

        AssertTextOnlyAppearsIn(repositoryRoot, converterFiles, "TargetId switch", allowed);
    }

    [TestMethod]
    public void ExecutionTargetCatalog_ShouldUsePhaseBasedDescriptors()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var catalog = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "ExecutionTargetCatalog.cs");
        var text = File.ReadAllText(catalog);

        StringAssert.Contains(text, "IReadOnlyDictionary<ExecutionTargetId, ExecutionTargetDescriptor>");
        StringAssert.Contains(text, "UseTemporaryDescriptor(ExecutionTargetDescriptor descriptor)");
        StringAssert.Contains(text, "AsyncLocal<TemporaryDescriptorScope?>");
        StringAssert.Contains(text, "scope.IsDisposed");
        StringAssert.Contains(text, "DisposeTemporaryScope(TemporaryDescriptorScope scope)");
        StringAssert.Contains(text, "Render(TargetRenderRequest request)");
        StringAssert.Contains(text, "ResolveBackend(ExecutionTargetId targetId)");
        StringAssert.Contains(text, "ResolveFinalizer(ExecutionTargetId targetId)");
        StringAssert.Contains(text, "ResolveActivator(ExecutionTargetId targetId)");
        StringAssert.Contains(text, "ResolveInspector(ExecutionTargetId targetId)");
        StringAssert.Contains(text, "CreateRenderInputs(");
        StringAssert.Contains(text, "CreateRenderBuildContribution(");
        StringAssert.Contains(text, "does not support");
        Assert.IsFalse(
            text.Contains("switch", StringComparison.Ordinal),
            "ExecutionTargetCatalog should resolve descriptor dictionaries instead of switching over target ids.");

        var registryFiles = RepositorySourceScan
            .ProductionSourceFiles(repositoryRoot, "Musoq.Converter")
            .Where(file => RepositorySourceScan.ToRelative(repositoryRoot, file).Contains(
                "src/dotnet/Musoq.Converter/ExecutionTargets",
                StringComparison.Ordinal))
            .ToArray();
        var partialOverrideOffenders = registryFiles
            .Where(file =>
            {
                var fileText = File.ReadAllText(file);
                return fileText.Contains("UseTemporaryBackend", StringComparison.Ordinal) ||
                       fileText.Contains("UseTemporaryFinalizer", StringComparison.Ordinal) ||
                       fileText.Contains("UseTemporaryInspector", StringComparison.Ordinal) ||
                       fileText.Contains("ExecutionTargetRegistration", StringComparison.Ordinal);
            })
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            partialOverrideOffenders,
            "Target test registration must use phase-based descriptors, not partial component overrides: " +
            string.Join(", ", partialOverrideOffenders));
    }

    [TestMethod]
    public void ExecutionTargetDescriptor_ShouldModelActivationAsOptionalClrPhase()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var descriptorFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "ExecutionTargetDescriptor.cs");
        var text = File.ReadAllText(descriptorFile);

        StringAssert.Contains(text, "IQueryExecutionBackend? RenderPhase");
        StringAssert.Contains(text, "IRenderedQueryFinalizer? FinalizationPhase");
        StringAssert.Contains(text, "IClrExecutableQueryActivator? ActivationPhase");
        StringAssert.Contains(text, "IRenderedQueryInspector? InspectionPhase");
        StringAssert.Contains(text, "IClrExecutableQueryActivator? activationPhase = null");
        Assert.IsFalse(
            Regex.IsMatch(text, @"IClrExecutableQueryActivator\s+ActivationPhase", RegexOptions.CultureInvariant) ||
            Regex.IsMatch(text, @"IClrExecutableQueryActivator\s+activationPhase", RegexOptions.CultureInvariant),
            "CLR runnable activation must be an optional descriptor phase, not a mandatory target contract.");
    }

    [TestMethod]
    public void ExecutionTargetCatalog_ShouldNotReintroduceRegistryFacades()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var executionTargetsRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets");
        var forbiddenFiles = new[]
        {
            "ExecutionBackendRegistry.cs",
            "RenderedQueryFinalizerRegistry.cs",
            "RenderedQueryInspectorRegistry.cs",
            "ClrRunnableActivatorRegistry.cs",
            "TemporaryExecutionTargetRegistryOverride.cs"
        };

        foreach (var forbiddenFile in forbiddenFiles)
        {
            Assert.IsFalse(
                File.Exists(Path.Combine(executionTargetsRoot, forbiddenFile)),
                $"{forbiddenFile} must not be reintroduced; phase lookup belongs in ExecutionTargetCatalog.");
        }

        var converterFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Converter");
        var forbiddenNames = new[]
        {
            "ExecutionBackendRegistry",
            "RenderedQueryFinalizerRegistry",
            "RenderedQueryInspectorRegistry",
            "ClrRunnableActivatorRegistry",
            "TemporaryExecutionTargetRegistryOverride",
            "RegistryOverride",
            "IExecutableQueryActivator"
        };

        foreach (var forbiddenName in forbiddenNames)
        {
            var offenders = converterFiles
                .Where(file => File.ReadAllText(file).Contains(forbiddenName, StringComparison.Ordinal))
                .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
                .ToArray();

            Assert.IsEmpty(
                offenders,
                $"{forbiddenName} must not appear in converter production code; use phase-based ExecutionTargetCatalog lookup: " +
                string.Join(", ", offenders));
        }
    }

    [TestMethod]
    public void ProductionSources_ShouldNotExposeTestOnlyNonClrTargetId()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Targets.Abstractions",
            "Musoq.Targets.Execution",
            "Musoq.Targets.Execution",
            "Musoq.Targets.CSharpClr");

        var offenders = productionFiles
            .Where(file => File.ReadAllText(file).Contains("TestOnlyNonClr", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "The fake non-CLR pressure target id must live only in test infrastructure: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void PortableDescriptorAndExportContracts_ShouldNotExposeClrRunnableContracts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = new[]
        {
            Path.Combine(
                repositoryRoot,
                "src",
                "dotnet",
                "Musoq.Converter",
                "ExecutionTargets",
                "ExecutionTargetDescriptor.cs"),
            Path.Combine(
                repositoryRoot,
                "src",
                "dotnet",
                "Musoq.Targets.Abstractions",
                "TargetExportArtifact.cs")
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.IsFalse(
                text.Contains("ITableRunnable", StringComparison.Ordinal) ||
                text.Contains("ITypedRunnable", StringComparison.Ordinal),
                $"{RepositorySourceScan.ToRelative(repositoryRoot, file)} must not expose CLR runnable contracts.");
        }
    }

    [TestMethod]
    public void PortableTargetHardening_ShouldRemainEnforced()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterRoot = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter");
        var executionRoot = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Targets.Execution");
        var abstractionsRoot = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Targets.Abstractions");

        var catalog = File.ReadAllText(Path.Combine(
            converterRoot,
            "ExecutionTargets",
            "ExecutionTargetCatalog.cs"));
        StringAssert.Contains(catalog, "FinalizeArtifact(");
        StringAssert.Contains(catalog, "InspectArtifact(");
        StringAssert.Contains(catalog, "TryInspectArtifact(");
        StringAssert.Contains(catalog, "ValidateProducedTarget(");

        var finalization = File.ReadAllText(Path.Combine(
            abstractionsRoot,
            "TargetFinalizationResult.cs"));
        StringAssert.Contains(finalization, "Successful target finalization must produce an executable artifact");
        StringAssert.Contains(finalization, "cannot contain an executable artifact");
        StringAssert.Contains(finalization, "Successful target finalization cannot contain error diagnostics");

        var package = File.ReadAllText(Path.Combine(
            executionRoot,
            "Targeting",
            "TargetArtifactPackage.cs"));
        StringAssert.Contains(package, "private TargetArtifactPackage(");
        StringAssert.Contains(package, "CreateValidated(");
        StringAssert.Contains(package, "TargetRuntimeEntrypointKind.TypedQuery");
        Assert.IsFalse(
            package.Contains("RequiredServices.Count == 0", StringComparison.Ordinal),
            "Portable exports may be self-contained and declare zero host runtime services.");

        var capabilities = File.ReadAllText(Path.Combine(
            executionRoot,
            "Targeting",
            "ExecutionTargetCapabilities.cs"));
        StringAssert.Contains(capabilities, "SupportedTypeSymbolPortabilities");
        StringAssert.Contains(capabilities, "SupportedCallableSymbolPortabilities");
        StringAssert.Contains(capabilities, "FormatUnsupportedSymbol");

        var packageCompiler = File.ReadAllText(Path.Combine(
            converterRoot,
            "InstanceCreator.Artifacts.cs"));
        StringAssert.Contains(packageCompiler, "TryInspectArtifact");
        StringAssert.Contains(packageCompiler, "CompileTargetPackageWithDiagnostics<TOut>");
        Assert.IsFalse(
            packageCompiler.Contains("if (inspection == null)", StringComparison.Ordinal),
            "Inspection must remain optional for export package compilation.");

        var abiBuilder = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.Execution",
            "Targeting",
            "TargetHostAbiInventoryBuilder.cs"));
        Assert.IsFalse(
            abiBuilder.Contains("ReadinessReport", StringComparison.Ordinal) ||
            abiBuilder.Contains("ClrOnlySymbol", StringComparison.Ordinal),
            "Readiness blockers must stay separate from actual host ABI imports.");

        var pressureTests = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            "NonClrTargetPipelinePressureTests.cs"));
        StringAssert.Contains(pressureTests, "WhenInspectionPhaseIsAbsent_ShouldStillBuildPackage");
        StringAssert.Contains(pressureTests, "WhenTypedResultIsRequested_ShouldBuildTypedExportPackage");
        StringAssert.Contains(pressureTests, "WhenRuntimeServicesAreTargetProvided_ShouldPackageWithoutHostAbiImports");
        StringAssert.Contains(pressureTests, "WhenRealQueryUsesUnsupportedCallablePortability_ShouldFailBeforeRender");

        var overrideTests = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            "ExecutionTargetCatalogOverrideTests.cs"));
        StringAssert.Contains(overrideTests, "WhenOverridesAreDisposedOutOfOrder_ShouldNotResurrectDisposedDescriptor");
        StringAssert.Contains(overrideTests, "WhenCapturedAsyncContextOutlivesRegistration_ShouldNotResolveDisposedDescriptor");
    }

    [TestMethod]
    public void ExecutionTargetBoundaryDocumentation_ShouldDescribePipelineAndFutureBackendRules()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var documentation = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "ExecutionTargets",
            "README.md");
        var text = File.ReadAllText(documentation);

        StringAssert.Contains(text, "AST -> logical -> physical -> execution IR -> render phase -> finalization/export phase -> packaging phase");
        StringAssert.Contains(text, "optional inspection phase");
        StringAssert.Contains(text, "optional activation phase");
        StringAssert.Contains(text, "New backends must declare capabilities");
        StringAssert.Contains(text, "produce their own rendered artifact type");
        StringAssert.Contains(text, "avoid adding C# assumptions");
        StringAssert.Contains(text, "IClrExecutableQueryActivator");
        StringAssert.Contains(text, "IRenderedQueryInspector");
        StringAssert.Contains(text, "TargetRuntimeContract");
        StringAssert.Contains(text, "ExecutionTargetDescriptor");
        StringAssert.Contains(text, "Musoq.Targets.Abstractions");
        StringAssert.Contains(text, "Musoq.Targets.Execution");
        StringAssert.Contains(text, "Musoq.Targets.Execution.Analysis");
        StringAssert.Contains(text, "CSharpClrTargetComposition");
        StringAssert.Contains(text, "CSharpClrRenderInputs");
        StringAssert.Contains(text, "CSharpClrFinalizationOptions");
        StringAssert.Contains(text, "TargetRenderRequest");
        StringAssert.Contains(text, "TargetRenderInputBuildContext");
        StringAssert.Contains(text, "TargetRenderInputCompilerState");
        StringAssert.Contains(text, "CompilationUnitName");
        StringAssert.Contains(text, "TargetBackendRenderInputs");
        StringAssert.Contains(text, "TargetFinalizationOptions");
        StringAssert.Contains(text, "TargetFinalizationOptionsContext");
        StringAssert.Contains(text, "TargetFinalizationResult");
        StringAssert.Contains(text, "TargetExportArtifact");
        StringAssert.Contains(text, "RenderedArtifactBuildContribution");
        StringAssert.Contains(text, "TargetArtifactPackagingContext");
        StringAssert.Contains(text, "TargetArtifactPackage");
        StringAssert.Contains(text, "TargetArtifactSemanticFacts");
        StringAssert.Contains(text, "CSharpClrTargetPackageFactory.CreateClrAssemblyPackage");
        StringAssert.Contains(text, "TargetArtifactPackage.CreatePortableExportPackage");
        StringAssert.Contains(text, "CreateValidated");
        StringAssert.Contains(text, "TypedQuery");
        StringAssert.Contains(text, "zero host runtime services");
        StringAssert.Contains(text, "CompileTargetPackageWithDiagnostics");
        StringAssert.Contains(text, "CSharpClrCompiledArtifactLoader");
        StringAssert.Contains(text, "ExecutionTargetCatalog.Render");
        StringAssert.Contains(text, "FinalizeArtifact");
        StringAssert.Contains(text, "inspection and activation are independently optional");
        StringAssert.Contains(text, "ExecutionPortableSymbolCatalog");
        StringAssert.Contains(text, "TargetHostAbiInventory");
        StringAssert.Contains(text, "TargetHostAbiImportDetails");
        StringAssert.Contains(text, "ExecutionPortableSymbolPortability");
        StringAssert.Contains(text, "TargetHostAbiImport.CreateCustom");
        StringAssert.Contains(text, "ContractVersion");
        StringAssert.Contains(text, "derived immutable string `Attributes`");
        StringAssert.Contains(text, "Portable`, `HostImport`, or `ClrOnly`");
        StringAssert.Contains(text, "broad requirement categories separately from type/callable symbol portability");
        StringAssert.Contains(text, "SupportedTypeSymbolPortabilities");
        StringAssert.Contains(text, "Readiness blockers are diagnostics, not imports");
        StringAssert.Contains(text, "PortableOutputTypeName");
        StringAssert.Contains(text, "PortableScriptParameters");
        StringAssert.Contains(text, "PortableSourcePlanSignatures");
        StringAssert.Contains(text, "callable/plugin query");
        StringAssert.Contains(text, "TargetPluginInvocationAbiDetails");
        StringAssert.Contains(text, "Cache values must be target-aware executable artifacts");
        StringAssert.Contains(text, "artifact-first");
        StringAssert.Contains(text, "phase-based `ExecutionTargetDescriptor`");
        StringAssert.Contains(text, "optional CLR activation phase");
        StringAssert.Contains(text, "export-only");
        StringAssert.Contains(text, "fake non-CLR pipeline harness");
        StringAssert.Contains(text, "intentional legacy C# compiler seams");
        StringAssert.Contains(text, "TestExecutionTargetIds.TestOnlyNonClr");
        StringAssert.Contains(text, "atomically disposed linked scopes");
        StringAssert.Contains(text, "converter-owned internal composition");
        StringAssert.Contains(text, "not a public external SPI");
        StringAssert.Contains(text, "no public target selector exists");
        StringAssert.Contains(text, "Public compiled-artifact APIs currently reject non-`CSharpClr`");
        Assert.IsFalse(
            text.Contains("registry facades used by existing build-chain code", StringComparison.Ordinal) ||
            text.Contains("complete descriptor", StringComparison.Ordinal) ||
            text.Contains("CSharpClrRenderInputBuildContext", StringComparison.Ordinal) ||
            text.Contains("CLR-only symbol blockers", StringComparison.Ordinal),
            "Execution target documentation must describe phase-based catalog lookup, not old registry facades or mandatory complete descriptors.");
    }

    [TestMethod]
    public void ContributorInstructions_ShouldDescribeCorrectedTargetPackageBoundary()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterInstructions = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter",
            "copilot-instructions.md"));
        var evaluatorInstructions = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "copilot-instructions.md"));

        StringAssert.Contains(converterInstructions, "phase-based `ExecutionTargetDescriptor`");
        StringAssert.Contains(converterInstructions, "Musoq.Targets.Execution");
        StringAssert.Contains(converterInstructions, "Musoq.Targets.Execution.Analysis");
        StringAssert.Contains(converterInstructions, "CSharpClrRenderInputs");
        StringAssert.Contains(converterInstructions, "CSharpClrFinalizationOptions");
        StringAssert.Contains(converterInstructions, "TargetRenderRequest");
        StringAssert.Contains(converterInstructions, "TargetRenderInputBuildContext");
        StringAssert.Contains(converterInstructions, "TargetRenderInputCompilerState");
        StringAssert.Contains(converterInstructions, "CompilationUnitName");
        StringAssert.Contains(converterInstructions, "TargetFinalizationOptions");
        StringAssert.Contains(converterInstructions, "TargetFinalizationOptionsContext");
        StringAssert.Contains(converterInstructions, "TargetRuntimeContract");
        StringAssert.Contains(converterInstructions, "TargetExportArtifact");
        StringAssert.Contains(converterInstructions, "RenderedArtifactBuildContribution");
        StringAssert.Contains(converterInstructions, "TargetArtifactPackagingContext");
        StringAssert.Contains(converterInstructions, "TargetArtifactPackage");
        StringAssert.Contains(converterInstructions, "TargetArtifactSemanticFacts");
        StringAssert.Contains(converterInstructions, "CSharpClrTargetPackageFactory.CreateClrAssemblyPackage");
        StringAssert.Contains(converterInstructions, "TargetArtifactPackage.CreatePortableExportPackage");
        StringAssert.Contains(converterInstructions, "CompileTargetPackageWithDiagnostics");
        StringAssert.Contains(converterInstructions, "CSharpClrCompiledArtifactLoader");
        StringAssert.Contains(converterInstructions, "ExecutionTargetCatalog.Render");
        StringAssert.Contains(converterInstructions, "optional inspection");
        StringAssert.Contains(converterInstructions, "ExecutionPortableSymbolCatalog");
        StringAssert.Contains(converterInstructions, "TargetHostAbiInventory");
        StringAssert.Contains(converterInstructions, "TargetHostAbiImportDetails");
        StringAssert.Contains(converterInstructions, "ExecutionPortableSymbolPortability");
        StringAssert.Contains(converterInstructions, "TargetHostAbiImport.CreateCustom");
        StringAssert.Contains(converterInstructions, "ContractVersion");
        StringAssert.Contains(converterInstructions, "Attributes");
        StringAssert.Contains(converterInstructions, "Portable`, `HostImport`, or `ClrOnly`");
        StringAssert.Contains(converterInstructions, "PortableOutputTypeName");
        StringAssert.Contains(converterInstructions, "PortableScriptParameters");
        StringAssert.Contains(converterInstructions, "PortableSourcePlanSignatures");
        StringAssert.Contains(converterInstructions, "callable/plugin query");
        StringAssert.Contains(converterInstructions, "TargetPluginInvocationAbiDetails");
        StringAssert.Contains(converterInstructions, "must not contain readiness/`ClrOnlySymbol` blockers");
        StringAssert.Contains(converterInstructions, "converter-owned internal `ExecutionTargetDescriptor` composition");
        StringAssert.Contains(converterInstructions, "public external target SPI");
        StringAssert.Contains(converterInstructions, "broad readiness categories independently from type/callable symbol portability");
        StringAssert.Contains(converterInstructions, "artifact-first");
        StringAssert.Contains(converterInstructions, "optional activation phase");
        StringAssert.Contains(converterInstructions, "fake non-CLR pipeline harness");
        StringAssert.Contains(converterInstructions, "deeply immutable");
        StringAssert.Contains(converterInstructions, "There is no public target-selection API");
        Assert.IsFalse(
            converterInstructions.Contains("converter owns only the internal `ExecutionTargetCatalog`, `CSharpClrTargetComposition`, and registry facades", StringComparison.Ordinal) ||
            converterInstructions.Contains("complete `ExecutionTargetDescriptor`", StringComparison.Ordinal),
            "Converter contributor instructions must document phase-based catalog lookup, not old registry facades or mandatory complete descriptors.");

        StringAssert.Contains(evaluatorInstructions, "Target-facing runtime contract/readiness report types live in `Musoq.Targets.Execution`");
        StringAssert.Contains(evaluatorInstructions, "plan-walking target analysis implementations live in `Musoq.Targets.Execution.Analysis`");
        StringAssert.Contains(evaluatorInstructions, "generated-query C# lowering lives in `Musoq.Targets.CSharpClr`");
        StringAssert.Contains(evaluatorInstructions, "Musoq.Targets.CSharpClr.Optimization.Codegen");
        StringAssert.Contains(evaluatorInstructions, "Musoq.Targets.Execution.Analysis.ExecutionTargetCompatibilityAnalyzer");
        StringAssert.Contains(evaluatorInstructions, "Musoq.Targets.Execution.Analysis.TargetRuntimeContractBuilder");
        StringAssert.Contains(evaluatorInstructions, "Musoq.Targets.Execution.TargetHostAbiInventoryBuilder");
        StringAssert.Contains(evaluatorInstructions, "ExecutionPortableSymbolCatalog");
        StringAssert.Contains(evaluatorInstructions, "Portable`, `HostImport`, or `ClrOnly`");
        StringAssert.Contains(evaluatorInstructions, "broad requirement categories separately from type/callable symbol portability");
        StringAssert.Contains(evaluatorInstructions, "Host ABI inventory");
        StringAssert.Contains(evaluatorInstructions, "TargetHostAbiImportDetails");
        StringAssert.Contains(evaluatorInstructions, "ExecutionPortableSymbolPortability");
        StringAssert.Contains(evaluatorInstructions, "TargetHostAbiImport.CreateCustom");
        StringAssert.Contains(evaluatorInstructions, "readiness and `ClrOnly` blockers must not be represented as imports");
        StringAssert.Contains(evaluatorInstructions, "ContractVersion");
        StringAssert.Contains(evaluatorInstructions, "Attributes");
        StringAssert.Contains(evaluatorInstructions, "Callable/plugin target coverage");
        StringAssert.Contains(evaluatorInstructions, "TargetPluginInvocationAbiDetails");
        StringAssert.Contains(evaluatorInstructions, "Target-specific rendering");
        StringAssert.Contains(evaluatorInstructions, "Target-neutral final projection/sink planning metadata");
        Assert.IsFalse(
            evaluatorInstructions.Contains("Musoq.Targets.Execution.ExecutionTargetCompatibilityAnalyzer", StringComparison.Ordinal) ||
            evaluatorInstructions.Contains("Musoq.Targets.Execution.TargetRuntimeContractBuilder", StringComparison.Ordinal),
            "Evaluator contributor instructions must point analyzer implementations to Musoq.Targets.Execution.Analysis.");
        Assert.IsFalse(
            evaluatorInstructions.Contains("IR/Execution/Rendering", StringComparison.Ordinal),
            "Evaluator contributor instructions must not direct generated C# rendering work back to evaluator renderer folders.");
        Assert.IsFalse(
            evaluatorInstructions.Contains("`CSharpRenderer`", StringComparison.Ordinal) ||
            evaluatorInstructions.Contains("`ExecutionCSharpRenderer`", StringComparison.Ordinal),
            "Evaluator contributor instructions must not describe C# renderers as evaluator-owned key classes.");
    }

    private static void AssertConstructorOnlyAppearsIn(
        string repositoryRoot,
        System.Collections.Generic.IEnumerable<string> files,
        string constructor,
        string allowedRelativePath)
    {
        var offenders = files
            .Where(file => File.ReadAllText(file).Contains(constructor, StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .Where(relativePath => !string.Equals(relativePath, allowedRelativePath, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"{constructor} must be constructed only by {allowedRelativePath}: {string.Join(", ", offenders)}");
    }

    private static void AssertTextOnlyAppearsIn(
        string repositoryRoot,
        System.Collections.Generic.IEnumerable<string> files,
        string text,
        params string[] allowedRelativePaths)
    {
        var allowed = allowedRelativePaths.ToHashSet(StringComparer.Ordinal);
        var offenders = files
            .Where(file => File.ReadAllText(file).Contains(text, StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .Where(relativePath => !allowed.Contains(relativePath))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"{text} must appear only in {string.Join(", ", allowedRelativePaths)}: {string.Join(", ", offenders)}");
    }

    private static void AssertTextAppearsExactlyIn(
        string repositoryRoot,
        System.Collections.Generic.IEnumerable<string> files,
        string text,
        params string[] expectedRelativePaths)
    {
        var expected = expectedRelativePaths.ToHashSet(StringComparer.Ordinal);
        var actual = files
            .Where(file => File.ReadAllText(file).Contains(text, StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var unexpected = actual
            .Where(path => !expected.Contains(path))
            .ToArray();
        var staleExpected = expected
            .Where(path => !actual.Contains(path, StringComparer.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            unexpected,
            $"{text} must appear only in {string.Join(", ", expectedRelativePaths)}: {string.Join(", ", unexpected)}");
        Assert.IsEmpty(
            staleExpected,
            $"Exact allow-list for {text} contains stale entries: {string.Join(", ", staleExpected)}");
    }

    private static void AssertRegexOnlyAppearsIn(
        string repositoryRoot,
        System.Collections.Generic.IEnumerable<string> files,
        Regex pattern,
        params string[] allowedRelativePaths)
    {
        var allowed = allowedRelativePaths.ToHashSet(StringComparer.Ordinal);
        var offenders = files
            .Where(file => pattern.IsMatch(File.ReadAllText(file)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .Where(relativePath => !allowed.Contains(relativePath))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"{pattern} must appear only in {string.Join(", ", allowedRelativePaths)}: {string.Join(", ", offenders)}");
    }
}
