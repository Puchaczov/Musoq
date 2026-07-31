using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Targets.Abstractions;
using Musoq.Targets.CSharpClr;
using Musoq.Targets.Execution;

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
        IReadOnlyList<CSharpClrBatchActivationRequest> requests)
    {
        var activator = ExecutionTargetCatalog.ResolveActivator(executionTarget)
            as ClrAssemblyExecutableActivator ??
            throw new InvalidOperationException(
                $"Execution target '{executionTarget}' does not expose CLR batch activation.");
        var results = activator.ActivateTableBatch(
            executable,
            requests
                .Select(static request => new ClrBatchTableActivationRequest(
                    request.RunnableTypeName,
                    request.Binding))
                .ToArray());
        return results
            .Select(static result => new CSharpClrBatchActivationResult(
                result.Runnable,
                result.Exception))
            .ToArray();
    }

    internal static string CreateBatchCompatibilityKey(
        RenderingBuildArtifacts rendering,
        ExecutionTargetId executionTarget,
        bool emitPdb,
        bool hasInterpreter,
        QueryResultMode resultMode)
    {
        var compilation = rendering.Compilation;
        var references = string.Join(
            "\u001f",
            compilation.References
                .Select(static reference => reference.Display ?? reference.ToString())
                .Order(StringComparer.Ordinal));
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
        bool emitPdb)
    {
        ArgumentNullException.ThrowIfNull(renderings);
        if (renderings.Count == 0)
            throw new ArgumentException("At least one rendered item is required.", nameof(renderings));

        var first = renderings[0];
        var compilation = first.Compilation
            .RemoveAllSyntaxTrees()
            .AddSyntaxTrees(renderings.SelectMany(static item => item.Compilation.SyntaxTrees));
        var renderedArtifact = CSharpClrArtifactCompatibility.CreateRenderedArtifact(
            compilation,
            first.AccessToClassPath);
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            executionTarget,
            new TargetFinalizationOptionsContext(emitPdb));
        return ExecutionTargetCatalog.FinalizeArtifact(renderedArtifact, options);
    }

    internal static ExecutableQueryArtifact CreateBatchExecutable(
        TargetFinalizationResult finalization,
        string runnableTypeName)
    {
        ArgumentNullException.ThrowIfNull(finalization);
        if (!finalization.Success || finalization.Artifact is null)
            throw new InvalidOperationException("Cannot create a batch executable from failed finalization.");

        var dllFile = CSharpClrArtifactCompatibility.GetDllFile(finalization.Artifact) ??
                      throw new InvalidOperationException("Batch finalization produced no DLL.");
        return CSharpClrArtifactCompatibility.CreateAssemblyExecutable(
            dllFile,
            CSharpClrArtifactCompatibility.GetPdbFile(finalization.Artifact),
            runnableTypeName);
    }
}

internal static class CSharpClrGeneratedCodeCompatibility
{
    internal static CSharpGeneratedSyntaxIdentity CreateStructuralIdentity(RenderedQueryArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

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
            var root = syntaxTree.GetRoot();
            builder.Append("tree:").Append(treeIndex++).Append(';');
            foreach (var token in root.DescendantTokens())
            {
                var value = token.IsKind(SyntaxKind.IdentifierToken) &&
                            identityParts.TryGetValue(token.ValueText, out var replacement)
                    ? replacement
                    : token.Text;
                builder.Append(token.RawKind).Append(':').Append(value.Length).Append(':').Append(value);
                AppendStructuredTrivia(builder, token.LeadingTrivia);
                AppendStructuredTrivia(builder, token.TrailingTrivia);
                builder.Append(';');
            }
        }

        var descriptor = builder.ToString();
        return new CSharpGeneratedSyntaxIdentity(
            descriptor,
            global::Musoq.Converter.CompiledQueryArtifactSupport.ComputeHash(descriptor));
    }

    private static void AppendStructuredTrivia(StringBuilder builder, SyntaxTriviaList trivia)
    {
        foreach (var item in trivia)
        {
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
