using System;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the compilation stage output: the emit result and produced
/// assembly/debug payloads.
/// </summary>
internal sealed record CompilationBuildArtifacts(TargetFinalizationResult FinalizationResult)
{
    public CompilationBuildArtifacts(EmitResult emitResult, byte[]? dllFile, byte[]? pdbFile)
        : this(
            CSharpClrArtifactCompatibility.CreateFinalizationResult(
                emitResult,
                dllFile is { Length: > 0 }
                    ? CSharpClrArtifactCompatibility.CreateAssemblyExecutable(dllFile, pdbFile, string.Empty)
                    : null))
    {
    }

    public CompilationBuildArtifacts(EmitResult emitResult, ExecutableQueryArtifact? artifact)
        : this(CSharpClrArtifactCompatibility.CreateFinalizationResult(emitResult, artifact))
    {
    }

    public ExecutableQueryArtifact? Artifact => FinalizationResult.Artifact;

    public EmitResult EmitResult => CSharpClrArtifactCompatibility.RequireEmitResult(
        FinalizationResult,
        "compilation artifact emit result access");

    public byte[]? DllFile => CSharpClrArtifactCompatibility.GetDllFile(Artifact);

    public byte[]? PdbFile => CSharpClrArtifactCompatibility.GetPdbFile(Artifact);

    public static CompilationBuildArtifacts From(TargetFinalizationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new CompilationBuildArtifacts(result);
    }

    public bool TryGetEmitResult(out EmitResult emitResult)
    {
        return CSharpClrArtifactCompatibility.TryGetEmitResult(FinalizationResult, out emitResult);
    }
}
