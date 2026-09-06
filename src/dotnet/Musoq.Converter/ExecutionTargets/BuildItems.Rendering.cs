using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal bool EnableContextualExecution
    {
        get => GetFlag(BuildItemKeys.EnableContextualExecution, defaultWhenMissing: false);
        set => SetFlag(BuildItemKeys.EnableContextualExecution, value);
    }

    internal RenderedQueryArtifact RenderingArtifact
    {
        get
        {
            if (TryGetArtifact<RenderedQueryArtifact>(BuildItemKeys.RenderingArtifact, out var artifact))
                return artifact;

            if (TryGetArtifact<CSharpCompilation>(BuildItemKeys.Compilation, out var compilation) &&
                TryGetArtifact<string>(BuildItemKeys.AccessToClassPath, out var accessToClassPath))
            {
                artifact = CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath);
                SetRequired(BuildItemKeys.RenderingArtifact, artifact);
                return artifact;
            }

            return GetRequired<RenderedQueryArtifact>(BuildItemKeys.RenderingArtifact);
        }
        set => SetRenderingArtifact(value);
    }

    public CSharpCompilation Compilation
    {
        get => GetRequired<CSharpCompilation>(BuildItemKeys.Compilation);
        set
        {
            SetRequired(BuildItemKeys.Compilation, value);
            TryRefreshCSharpRenderingArtifact();
        }
    }

    public string AccessToClassPath
    {
        get => GetRequired<string>(BuildItemKeys.AccessToClassPath);
        set
        {
            SetRequired(BuildItemKeys.AccessToClassPath, value);
            TryRefreshCSharpRenderingArtifact();
        }
    }

    private void SetRenderingArtifact(RenderedQueryArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        SetRequired(BuildItemKeys.RenderingArtifact, artifact);

        if (CSharpClrArtifactCompatibility.TryGetRenderedArtifact(artifact, out var csharpArtifact))
        {
            SetRequired(BuildItemKeys.Compilation, csharpArtifact.Compilation);
            SetRequired(BuildItemKeys.AccessToClassPath, csharpArtifact.AccessToClassPath);
            SetRequired(
                BuildItemKeys.QueryMethodRenderMetadata,
                CSharpClrArtifactCompatibility.GetQueryMethodRenderMetadata(artifact));
        }
    }

    private void TryRefreshCSharpRenderingArtifact()
    {
        if (TryGetArtifact<CSharpCompilation>(BuildItemKeys.Compilation, out var compilation) &&
            TryGetArtifact<string>(BuildItemKeys.AccessToClassPath, out var accessToClassPath))
        {
            SetRequired(
                BuildItemKeys.RenderingArtifact,
                CSharpClrArtifactCompatibility.CreateRenderedArtifact(compilation, accessToClassPath));
        }
    }
}

/// <summary>
/// C#-specific compatibility seam for the internal exhaustive-compilation test
/// batcher. The batch coordinator itself remains independent of Roslyn types.
/// </summary>
internal static class CSharpClrBatchCompatibility
{
    internal static IReadOnlyList<CSharpClrBatchActivationResult> ActivateBatch(
        ExecutionTargetId executionTarget,
        ExecutableQueryArtifact executable,
        IReadOnlyList<CSharpClrBatchActivationRequest> requests,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activator = ExecutionTargetCatalog.ResolveActivator(executionTarget)
            as ClrAssemblyExecutableActivator ??
            throw new InvalidOperationException(
                $"Execution target '{executionTarget}' does not expose CLR batch activation.");
        var activationRequests = new ClrBatchTableActivationRequest[requests.Count];
        for (var index = 0; index < requests.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            activationRequests[index] = new ClrBatchTableActivationRequest(
                requests[index].RunnableTypeName,
                requests[index].Binding);
        }

        IReadOnlyList<ClrBatchTableActivationResult>? results = null;
        try
        {
            results = activator.ActivateTableBatch(executable, activationRequests);
            cancellationToken.ThrowIfCancellationRequested();
            var converted = new CSharpClrBatchActivationResult[results.Count];
            for (var index = 0; index < results.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                converted[index] = new CSharpClrBatchActivationResult(
                    results[index].Runnable,
                    results[index].Exception);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return converted;
        }
        catch (OperationCanceledException)
        {
            if (results is not null)
                foreach (var result in results)
                    (result.Runnable as IDisposable)?.Dispose();

            throw;
        }
    }

    internal static string CreateBatchCompatibilityKey(
        RenderingBuildArtifacts rendering,
        ExecutionTargetId executionTarget,
        bool emitPdb,
        bool hasInterpreter,
        QueryResultMode resultMode)
    {
        return CreateBatchCompatibilityKey(
            rendering,
            executionTarget,
            emitPdb,
            hasInterpreter,
            resultMode,
            CancellationToken.None);
    }

    internal static string CreateBatchCompatibilityKey(
        RenderingBuildArtifacts rendering,
        ExecutionTargetId executionTarget,
        bool emitPdb,
        bool hasInterpreter,
        QueryResultMode resultMode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compilation = rendering.Compilation;
        var references = string.Join(
            "\u001f",
            compilation.References
                .Select(reference =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return reference.Display ?? reference.ToString();
                })
                .Order(StringComparer.Ordinal));
        cancellationToken.ThrowIfCancellationRequested();
        var syntaxShape = hasInterpreter ? "interpreter-fallback" : "no-interpreter";
        return string.Join(
            "\u001e",
            executionTarget,
            compilation.Options.ToString(),
            references,
            syntaxShape,
            emitPdb,
            resultMode);
    }

    internal static TargetFinalizationResult FinalizeBatch(
        IReadOnlyList<RenderingBuildArtifacts> renderings,
        ExecutionTargetId executionTarget,
        bool emitPdb,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(renderings);
        if (renderings.Count == 0)
            throw new ArgumentException("At least one rendered item is required.", nameof(renderings));

        var first = renderings[0];
        var compilation = first.Compilation
            .RemoveAllSyntaxTrees()
            .AddSyntaxTrees(renderings.SelectMany(item =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return item.Compilation.SyntaxTrees;
            }));
        cancellationToken.ThrowIfCancellationRequested();
        var renderedArtifact = CSharpClrArtifactCompatibility.CreateRenderedArtifact(
            compilation,
            first.AccessToClassPath);
        cancellationToken.ThrowIfCancellationRequested();
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            executionTarget,
            new TargetFinalizationOptionsContext(emitPdb, CancellationToken: cancellationToken));
        return ExecutionTargetCatalog.FinalizeArtifact(renderedArtifact, options);
    }

    internal static ExecutableQueryArtifact CreateBatchExecutable(
        TargetFinalizationResult finalization,
        string runnableTypeName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(finalization);
        if (!finalization.Success || finalization.Artifact is null)
            throw new InvalidOperationException("Cannot create a batch executable from failed finalization.");

        var dllFile = CSharpClrArtifactCompatibility.GetDllFile(finalization.Artifact) ??
                      throw new InvalidOperationException("Batch finalization produced no DLL.");
        var executable = CSharpClrArtifactCompatibility.CreateAssemblyExecutable(
            dllFile,
            CSharpClrArtifactCompatibility.GetPdbFile(finalization.Artifact),
            runnableTypeName);
        cancellationToken.ThrowIfCancellationRequested();
        return executable;
    }
}

internal static class CSharpClrGeneratedCodeCompatibility
{
    internal static CSharpGeneratedSyntaxIdentity CreateStructuralIdentity(RenderedQueryArtifact artifact)
    {
        return CreateStructuralIdentity(artifact, CancellationToken.None);
    }

    internal static CSharpGeneratedSyntaxIdentity CreateStructuralIdentity(
        RenderedQueryArtifact artifact,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        cancellationToken.ThrowIfCancellationRequested();

        var compilation = CSharpClrArtifactCompatibility.RequireCompilation(
            artifact,
            "canonical generated artifact identity");
        var accessPath = CSharpClrArtifactCompatibility.RequireAccessToClassPath(
            artifact,
            "canonical generated artifact identity");
        var identityParts = accessPath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .Select((part, index) => (part, Replacement: $"__generated_identity_{index}__"))
            .ToDictionary(static item => item.part, static item => item.Replacement, StringComparer.Ordinal);
        var builder = new StringBuilder();
        var treeIndex = 0;

        foreach (var syntaxTree in compilation.SyntaxTrees.OrderBy(
                     static tree => tree.FilePath,
                     StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = syntaxTree.GetRoot();
            builder.Append("tree:").Append(treeIndex++).Append(';');
            foreach (var token in root.DescendantTokens())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = token.IsKind(SyntaxKind.IdentifierToken) &&
                            identityParts.TryGetValue(token.ValueText, out var replacement)
                    ? replacement
                    : token.Text;
                builder.Append(token.RawKind).Append(':').Append(value.Length).Append(':').Append(value);
                AppendStructuredTrivia(builder, token.LeadingTrivia, cancellationToken);
                AppendStructuredTrivia(builder, token.TrailingTrivia, cancellationToken);
                builder.Append(';');
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var descriptor = builder.ToString();
        return new CSharpGeneratedSyntaxIdentity(
            descriptor,
            CompiledQueryArtifactSupport.ComputeHash(descriptor));
    }

    private static void AppendStructuredTrivia(
        StringBuilder builder,
        SyntaxTriviaList trivia,
        CancellationToken cancellationToken)
    {
        foreach (var item in trivia)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!item.HasStructure)
                continue;

            var text = item.ToFullString();
            builder.Append("trivia:").Append(item.RawKind).Append(':').Append(text.Length).Append(':').Append(text).Append(';');
        }
    }
}

internal readonly record struct CSharpGeneratedSyntaxIdentity(string Descriptor, string Hash);

internal sealed record CSharpClrBatchActivationRequest(
    string RunnableTypeName,
    QueryRuntimeBinding Binding);

internal sealed record CSharpClrBatchActivationResult(
    ITableRunnable? Runnable,
    Exception? Exception)
{
    public bool Succeeded => Runnable is not null && Exception is null;
}
