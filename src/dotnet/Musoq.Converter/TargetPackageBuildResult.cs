using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Converter.Build;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter;

internal sealed class TargetPackageBuildResult
{
    private TargetPackageBuildResult(
        TargetArtifactPackage? package,
        RenderedQueryInspection? inspection,
        IReadOnlyList<Diagnostic> diagnostics,
        Exception? caughtException,
        BuildItems? buildItems,
        string? compilationOptionsSignature)
    {
        Package = package;
        Inspection = inspection;
        Diagnostics = diagnostics;
        CaughtException = caughtException;
        BuildItems = buildItems;
        CompilationOptionsSignature = compilationOptionsSignature;
        Errors = diagnostics.Where(static diagnostic => diagnostic.IsError).ToList();
        Warnings = diagnostics.Where(static diagnostic => diagnostic.IsWarning).ToList();
    }

    [MemberNotNullWhen(true, nameof(Package), nameof(CompilationOptionsSignature))]
    public bool Succeeded => Package != null && Errors.Count == 0;

    public TargetArtifactPackage? Package { get; }

    public RenderedQueryInspection? Inspection { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public IReadOnlyList<Diagnostic> Errors { get; }

    public IReadOnlyList<Diagnostic> Warnings { get; }

    public Exception? CaughtException { get; }

    public BuildItems? BuildItems { get; }

    public string? CompilationOptionsSignature { get; }

    public static TargetPackageBuildResult Success(
        TargetArtifactPackage package,
        RenderedQueryInspection? inspection,
        IReadOnlyList<Diagnostic> diagnostics,
        BuildItems buildItems,
        string compilationOptionsSignature)
    {
        return new TargetPackageBuildResult(
            package,
            inspection,
            diagnostics,
            caughtException: null,
            buildItems,
            compilationOptionsSignature);
    }

    public static TargetPackageBuildResult Failure(
        IReadOnlyList<Diagnostic> diagnostics,
        Exception? caughtException = null,
        BuildItems? buildItems = null)
    {
        return new TargetPackageBuildResult(
            package: null,
            inspection: null,
            diagnostics,
            caughtException,
            buildItems,
            compilationOptionsSignature: null);
    }
}
