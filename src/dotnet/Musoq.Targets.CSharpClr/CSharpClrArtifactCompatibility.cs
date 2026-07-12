using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Targets.CSharpClr;

internal static class CSharpClrArtifactCompatibility
{
    public static CSharpRenderedQueryArtifact CreateRenderedArtifact(
        CSharpCompilation compilation,
        string accessToClassPath)
    {
        return new CSharpRenderedQueryArtifact(compilation, accessToClassPath);
    }

    public static ClrAssemblyExecutableArtifact CreateAssemblyExecutable(
        byte[] dllFile,
        byte[]? pdbFile,
        string runnableTypeName)
    {
        return new ClrAssemblyExecutableArtifact(dllFile, pdbFile, runnableTypeName);
    }

    public static TargetFinalizationResult CreateFinalizationResult(
        EmitResult emitResult,
        ExecutableQueryArtifact? artifact)
    {
        return new CSharpClrFinalizationResult(emitResult, artifact);
    }

    public static CSharpCompilation RequireCompilation(RenderedQueryArtifact artifact, string operation)
    {
        return RequireRenderedArtifact(artifact, operation).Compilation;
    }

    public static string RequireAccessToClassPath(RenderedQueryArtifact artifact, string operation)
    {
        return RequireRenderedArtifact(artifact, operation).AccessToClassPath;
    }

    public static CSharpRenderedQueryArtifact RequireRenderedArtifact(
        RenderedQueryArtifact artifact,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return artifact as CSharpRenderedQueryArtifact ??
               throw new InvalidOperationException(
                   $"C# CLR compatibility requires a C# rendered artifact for {operation}, but got '{artifact.TargetId}'.");
    }

    public static bool TryGetRenderedArtifact(
        RenderedQueryArtifact artifact,
        out CSharpRenderedQueryArtifact csharpArtifact)
    {
        if (artifact is CSharpRenderedQueryArtifact rendered)
        {
            csharpArtifact = rendered;
            return true;
        }

        csharpArtifact = null!;
        return false;
    }

    public static QueryMethodRenderMetadata GetQueryMethodRenderMetadata(RenderedQueryArtifact artifact)
    {
        return TryGetQueryMethodRenderMetadata(artifact, out var metadata)
            ? metadata
            : QueryMethodRenderMetadata.Unknown;
    }

    public static bool TryGetQueryMethodRenderMetadata(
        RenderedQueryArtifact artifact,
        out QueryMethodRenderMetadata metadata)
    {
        if (artifact is CSharpRenderedQueryArtifact rendered)
        {
            metadata = rendered.QueryMethodRenderMetadata;
            return true;
        }

        metadata = QueryMethodRenderMetadata.Unknown;
        return false;
    }

    public static bool TryGetOptimizationTrace(
        RenderedQueryArtifact artifact,
        out OptimizationTrace? trace)
    {
        if (artifact is CSharpRenderedQueryArtifact rendered)
        {
            trace = rendered.OptimizationTrace;
            return true;
        }

        trace = null;
        return false;
    }

    public static bool TryGetAssemblyExecutable(
        ExecutableQueryArtifact? artifact,
        out ClrAssemblyExecutableArtifact clrArtifact)
    {
        if (artifact is ClrAssemblyExecutableArtifact executable)
        {
            clrArtifact = executable;
            return true;
        }

        clrArtifact = null!;
        return false;
    }

    public static ClrAssemblyExecutableArtifact RequireAssemblyExecutable(
        ExecutableQueryArtifact artifact,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return artifact as ClrAssemblyExecutableArtifact ??
               throw new InvalidOperationException(
                   $"C# CLR compatibility requires a CLR assembly executable artifact for {operation}, but got '{artifact.TargetId}'.");
    }

    public static ClrLoadedExecutableArtifact RequireLoadedExecutable(
        ExecutableQueryArtifact artifact,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return artifact as ClrLoadedExecutableArtifact ??
               throw new InvalidOperationException(
                   $"C# CLR compatibility requires a loaded CLR executable artifact for {operation}, but got '{artifact.TargetId}'.");
    }

    public static byte[]? GetDllFile(ExecutableQueryArtifact? artifact)
    {
        return artifact is ClrAssemblyExecutableArtifact clrArtifact
            ? clrArtifact.DllFile
            : null;
    }

    public static byte[]? GetPdbFile(ExecutableQueryArtifact? artifact)
    {
        return artifact is ClrAssemblyExecutableArtifact clrArtifact
            ? clrArtifact.PdbFile
            : null;
    }

    public static CSharpClrFinalizationResult RequireFinalizationResult(
        TargetFinalizationResult result,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result as CSharpClrFinalizationResult ??
               throw new InvalidOperationException(
                   $"C# CLR compatibility requires a C# finalization result for {operation}, but got '{result.TargetId}'.");
    }

    public static EmitResult RequireEmitResult(
        TargetFinalizationResult result,
        string operation)
    {
        return RequireFinalizationResult(result, operation).EmitResult;
    }

    public static bool TryGetEmitResult(
        TargetFinalizationResult result,
        out EmitResult emitResult)
    {
        if (result is CSharpClrFinalizationResult csharpResult)
        {
            emitResult = csharpResult.EmitResult;
            return true;
        }

        emitResult = null!;
        return false;
    }

    public static string ComputeGeneratedCodeHash(RenderedQueryArtifact artifact)
    {
        return ComputeGeneratedCodeHash(
            RequireRenderedArtifact(artifact, "generated code hashing").Compilation);
    }

    public static string ComputeGeneratedCodeHash(CSharpCompilation compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        var builder = new StringBuilder();
        var index = 0;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText().ToString();
            builder
                .Append(CultureInfo.InvariantCulture, $"tree:{index}:")
                .Append(text.Length)
                .AppendLine()
                .Append(text)
                .AppendLine();
            index++;
        }

        return ComputeHash(builder.ToString());
    }

    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
