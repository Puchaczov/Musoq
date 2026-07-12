using System;
using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace Musoq.Targets.CSharpClr;

internal sealed class CSharpClrRenderedQueryFinalizer : IRenderedQueryFinalizer
{
    public ExecutionTargetId TargetId => ExecutionTargetIds.CSharpClr;

    public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
    {
        if (artifact is not CSharpRenderedQueryArtifact csharp)
            throw new InvalidOperationException(
                $"C# CLR finalizer expected artifact type '{nameof(CSharpRenderedQueryArtifact)}' for target '{ExecutionTargetIds.CSharpClr}', but received artifact type '{artifact.GetType().Name}' for target '{artifact.TargetId}'.");

        if (options is not CSharpClrFinalizationOptions csharpOptions)
            throw new NotSupportedException(
                $"C# CLR finalizer expected options type '{nameof(CSharpClrFinalizationOptions)}', but received '{options.GetType().Name}'.");

        var emitPdb = csharpOptions.EmitPdb;
        using var dllStream = new MemoryStream();
        using var pdbStream = emitPdb ? new MemoryStream() : null;

        var result = emitPdb
            ? csharp.Compilation.Emit(dllStream, pdbStream,
                options: new EmitOptions(false, DebugInformationFormat.PortablePdb))
            : csharp.Compilation.Emit(dllStream);

        if (!result.Success)
            return new CSharpClrFinalizationResult(result, null);

        byte[]? pdbFile;
        if (emitPdb)
        {
            if (!pdbStream!.TryGetBuffer(out var pdbBuffer))
                pdbBuffer = new ArraySegment<byte>(pdbStream.ToArray());

            pdbFile = pdbBuffer.Count == pdbBuffer.Array!.Length ? pdbBuffer.Array : pdbBuffer.ToArray();
        }
        else
        {
            pdbFile = null;
        }

        if (!dllStream.TryGetBuffer(out var dllBuffer))
            dllBuffer = new ArraySegment<byte>(dllStream.ToArray());

        var dllFile = dllBuffer.Count == dllBuffer.Array!.Length ? dllBuffer.Array : dllBuffer.ToArray();
        var executable = new ClrAssemblyExecutableArtifact(dllFile, pdbFile, csharp.AccessToClassPath);

        return new CSharpClrFinalizationResult(result, executable);
    }
}
