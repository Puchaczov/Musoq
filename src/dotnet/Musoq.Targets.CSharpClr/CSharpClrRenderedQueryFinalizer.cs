using System.IO;
using System.Threading;
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

        csharpOptions.CancellationToken.ThrowIfCancellationRequested();

        var emitPdb = csharpOptions.EmitPdb;
        var dllStream = new MemoryStream();
        var pdbStream = emitPdb ? new MemoryStream() : null;

        EmitResult result;
        try
        {
            result = emitPdb
                ? csharp.Compilation.Emit(dllStream, pdbStream,
                    options: new EmitOptions(false, DebugInformationFormat.PortablePdb),
                    cancellationToken: csharpOptions.CancellationToken)
                : csharp.Compilation.Emit(
                    dllStream,
                    cancellationToken: csharpOptions.CancellationToken);
        }
        catch
        {
            pdbStream?.Dispose();
            dllStream.Dispose();
            throw;
        }

        csharpOptions.CancellationToken.ThrowIfCancellationRequested();

        if (!result.Success)
        {
            pdbStream?.Dispose();
            dllStream.Dispose();
            return new CSharpClrFinalizationResult(result, null);
        }

        if (csharpOptions.Purpose == TargetFinalizationPurpose.Execution)
        {
            ClrAssemblyExecutableArtifact? artifactResult = null;
            try
            {
                artifactResult = new ClrAssemblyExecutableArtifact(dllStream, pdbStream, csharp.AccessToClassPath);
                csharpOptions.CancellationToken.ThrowIfCancellationRequested();
                return new CSharpClrFinalizationResult(result, artifactResult);
            }
            catch
            {
                artifactResult?.Dispose();
                if (artifactResult == null)
                {
                    pdbStream?.Dispose();
                    dllStream.Dispose();
                }

                throw;
            }
        }

        try
        {
            var dllFile = ToByteArray(dllStream, csharpOptions.CancellationToken);
            var pdbFile = pdbStream is null
                ? null
                : ToByteArray(pdbStream, csharpOptions.CancellationToken);
            var finalizationResult = new CSharpClrFinalizationResult(
                result,
                new ClrAssemblyExecutableArtifact(dllFile, pdbFile, csharp.AccessToClassPath));
            csharpOptions.CancellationToken.ThrowIfCancellationRequested();
            return finalizationResult;
        }
        finally
        {
            pdbStream?.Dispose();
            dllStream.Dispose();
        }
    }

    private static byte[] ToByteArray(MemoryStream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (stream.TryGetBuffer(out var buffer) &&
            buffer.Offset == 0 &&
            buffer.Count == buffer.Array!.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return buffer.Array;
        }

        var result = stream.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
