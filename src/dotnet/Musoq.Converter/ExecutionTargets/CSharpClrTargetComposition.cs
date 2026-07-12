using Musoq.Converter;
using Musoq.Targets.CSharpClr;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Build;

internal static class CSharpClrTargetComposition
{
    public static ExecutionTargetDescriptor CreateDescriptor()
    {
        return ExecutionTargetDescriptor.Create(
            ExecutionTargetIds.CSharpClr,
            renderPhase: new CSharpClrExecutionBackend(),
            finalizationPhase: new CSharpClrRenderedQueryFinalizer(),
            activationPhase: new ClrAssemblyExecutableActivator(),
            inspectionPhase: new CSharpRenderedQueryInspector(),
            createRenderInputs: CreateRenderInputs,
            createFinalizationOptions: static context => new CSharpClrFinalizationOptions(context.EmitPdb),
            createRenderBuildContribution: CreateRenderBuildContribution,
            createArtifactPackage: CreateArtifactPackage);
    }

    private static TargetBackendRenderInputs CreateRenderInputs(TargetRenderInputBuildContext context)
    {
        var compilerState = context.CompilerState;

        return new CSharpClrRenderInputs
        {
            CompilationOptions = context.CompilationOptions,
            AssemblyName = compilerState.CompilationUnitName,
            NamespaceName = SanitizeNameForNamespace(compilerState.CompilationUnitName),
            QueryResultMode = context.QueryResultMode,
            OutputType = compilerState.OutputType,
            AdditionalReferenceTypes = compilerState.AdditionalReferenceTypes,
            InterpreterSourceCode = compilerState.InterpreterSourceCode,
            Scope = compilerState.Scope,
            ScriptParameterDefinitions = compilerState.ScriptParameterDefinitions,
            ScriptVariableDefinitions = compilerState.ScriptVariableDefinitions,
            ReferenceAssemblies = compilerState.ReferenceAssemblies
        };
    }

    private static string SanitizeNameForNamespace(string name)
    {
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '.' && chars[i] != '_')
                chars[i] = '_';
        }

        return char.IsDigit(chars[0])
            ? $"_{new string(chars)}"
            : new string(chars);
    }

    private static RenderedArtifactBuildContribution CreateRenderBuildContribution(RenderedQueryArtifact artifact)
    {
        var optimizationTrace = CSharpClrArtifactCompatibility.TryGetOptimizationTrace(artifact, out var trace)
            ? trace
            : null;

        return new RenderedArtifactBuildContribution(
            CSharpClrArtifactCompatibility.GetQueryMethodRenderMetadata(artifact),
            optimizationTrace,
            CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(artifact));
    }

    private static TargetArtifactPackage CreateArtifactPackage(TargetArtifactPackagingContext context)
    {
        var renderedArtifact = CSharpClrArtifactCompatibility.RequireRenderedArtifact(
            context.RenderedArtifact,
            "compiled artifact packaging");
        var executableArtifact = CSharpClrArtifactCompatibility.RequireAssemblyExecutable(
            context.ExecutableArtifact,
            "compiled artifact packaging");
        var runnableTypeName = string.IsNullOrWhiteSpace(executableArtifact.RunnableTypeName)
            ? CompiledQueryArtifactSupport.GetRunnableTypeName(context.PackageName)
            : executableArtifact.RunnableTypeName;
        var generatedCodeSha256 = CSharpClrArtifactCompatibility.ComputeGeneratedCodeHash(renderedArtifact);

        return CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(
            CompiledQueryArtifactSupport.ArtifactKindRuntimeV2Query,
            CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
            context.SemanticsContract,
            metadata: CompiledQueryArtifactSupport.CreateMetadata(
                context,
                runnableTypeName,
                CompiledQueryArtifactSupport.ExecutableArtifactKindClrAssembly,
                generatedCodeSha256),
            binaryBlobs: CreateBinaryBlobs(context.ExecutableArtifact),
            entrypoints:
            [
                new TargetRuntimeEntrypoint(
                    "CompiledQuery",
                    TargetRuntimeEntrypointKind.TableQuery,
                    runnableTypeName)
            ],
            hostAbiInventory: context.RuntimeContract is null
                ? TargetHostAbiInventory.Empty
                : TargetHostAbiInventoryBuilder.Build(context.RuntimeContract),
            assemblyBlobName: CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
            generatedCodeSha256MetadataKey: CompiledQueryArtifactSupport.MetadataGeneratedCodeSha256,
            requiredMetadataKeys:
            [
                CompiledQueryArtifactSupport.MetadataArtifactKind,
                CompiledQueryArtifactSupport.MetadataAssemblyName,
                CompiledQueryArtifactSupport.MetadataRuntimeV2ContractSignature,
                CompiledQueryArtifactSupport.MetadataExecutionSemanticsVersion,
                CompiledQueryArtifactSupport.MetadataExecutionTarget,
                CompiledQueryArtifactSupport.MetadataExecutableArtifactKind,
                CompiledQueryArtifactSupport.MetadataScriptSha256,
                CompiledQueryArtifactSupport.MetadataSemanticShapeSha256
            ],
            executionIrVersion: context.ExecutionIrVersion);
    }

    private static TargetExportBinaryBlob[] CreateBinaryBlobs(
        ExecutableQueryArtifact executableArtifact)
    {
        var clrArtifact = CSharpClrArtifactCompatibility.RequireAssemblyExecutable(
            executableArtifact,
            "compiled artifact packaging");

        if (clrArtifact.PdbFile is not { Length: > 0 } pdbFile)
        {
            return
            [
                new TargetExportBinaryBlob(
                    CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
                    clrArtifact.DllFile,
                    CompiledQueryArtifactSupport.CSharpClrAssemblyContentType)
            ];
        }

        return
        [
            new TargetExportBinaryBlob(
                CompiledQueryArtifactSupport.CSharpClrAssemblyBlobName,
                clrArtifact.DllFile,
                CompiledQueryArtifactSupport.CSharpClrAssemblyContentType),
            new TargetExportBinaryBlob(
                CompiledQueryArtifactSupport.CSharpClrSymbolsBlobName,
                pdbFile,
                CompiledQueryArtifactSupport.CSharpClrSymbolsContentType)
        ];
    }
}
