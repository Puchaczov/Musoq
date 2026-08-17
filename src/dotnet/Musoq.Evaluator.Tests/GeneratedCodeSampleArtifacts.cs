using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedCodeSampleArtifacts
{
    private const int SnapshotMaxDegreeOfParallelism = 24;
    private const int CacheGenerationVersion = 1;

    private static readonly GeneratedCodeArtifactCache<GeneratedCodeSampleCacheKey, string> Cache = new();
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly Regex GeneratedNamespaceRegex = new(
        @"(?m)^namespace (?!Musoq\.Generated\.Interpreters\b)[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*",
        RegexOptions.Compiled);

    private static readonly Regex InvalidIdentifierPartRegex = new("[^A-Za-z0-9_]", RegexOptions.Compiled);

    public static string SamplesDirectory { get; } = Path.Combine(
        FindRepoRoot(),
        "generated-code-samples",
        "current");

    public static string ProfiledSamplesDirectory { get; } = Path.Combine(
        FindRepoRoot(),
        "generated-code-samples",
        "profiled");

    public static string GetSamplePath(GeneratedCodeSample sample)
    {
        return Path.Combine(SamplesDirectory, sample.FileName);
    }

    public static string GetProfiledSamplePath(GeneratedCodeSample sample)
    {
        return Path.Combine(ProfiledSamplesDirectory, sample.FileName);
    }

    public static string Generate(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        var key = CreateCacheKey(sample);
        var generated = Cache.GetOrAdd(
            key,
            _ => GenerateCoreWithTiming(sample, loggerResolver),
            out var cacheHit);

        if (cacheHit)
            GeneratedCodeSampleTiming.RecordCacheHit(sample);

        return generated;
    }

    internal static string GenerateUncachedForRefresh(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        return GenerateCoreWithTiming(sample, loggerResolver);
    }

    private static string GenerateCoreWithTiming(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        var startedUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return GenerateCore(sample, loggerResolver);
        }
        finally
        {
            stopwatch.Stop();
            GeneratedCodeSampleTiming.RecordGeneration(
                sample,
                startedUtc,
                DateTimeOffset.UtcNow,
                stopwatch.Elapsed);
        }
    }

    private static string GenerateCore(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        var compilationOptions = CreateSnapshotCompilationOptions(sample.CompilationOptions);

        if (sample.CompilationOptions != null)
            return GenerateWithCompilationOptions(sample, loggerResolver, compilationOptions);

        var buildItems = InstanceCreator.CreateForAnalyze(
            sample.Query,
            CreateAssemblyName(sample),
            sample.CreateSchemaProvider(),
            loggerResolver,
            compilationOptions);

        if (!buildItems.TryGetValue("COMPILATION", out var compilationValue) ||
            compilationValue is not CSharpCompilation compilation)
        {
            var diagnostics = GetDiagnosticErrors(buildItems);
            var detail = diagnostics.Length == 0
                ? "No diagnostics were collected."
                : string.Join(Environment.NewLine, diagnostics);

            throw new InvalidOperationException(
                $"Compilation should not be null for {sample.FileName}.{Environment.NewLine}{detail}");
        }

        return FormatSample(sample, buildItems, compilation);
    }

    private static string GenerateWithCompilationOptions(
        GeneratedCodeSample sample,
        ILoggerResolver loggerResolver,
        CompilationOptions compilationOptions)
    {
        var inspection = InstanceCreator.CompileForInspection(
            sample.Query,
            CreateAssemblyName(sample),
            sample.CreateSchemaProvider(),
            loggerResolver,
            compilationOptions);

        return FormatSample(sample, inspection);
    }

    private static CompilationOptions CreateSnapshotCompilationOptions(CompilationOptions? options)
    {
        options ??= new CompilationOptions();

        return new CompilationOptions(
            options.ParallelizationMode,
            options.UseHashJoin,
            options.UseSortMergeJoin,
            options.UseCommonSubexpressionElimination,
            options.UseConstantFolding,
            options.UsePrimitiveTypeValidation,
            options.UseCteParallelization,
            options.UseCteSidecarIndexes,
            options.SourceRuntimeSettingsResolver,
            options.InstrumentationMode,
            SnapshotMaxDegreeOfParallelism,
            options.ForceTableResultMaterialization)
            .WithRecursiveCteLimits(options.RecursiveCteLimits);
    }

    private static GeneratedCodeSampleCacheKey CreateCacheKey(GeneratedCodeSample sample)
    {
        var snapshotOptions = CreateSnapshotCompilationOptions(sample.CompilationOptions);

        return new GeneratedCodeSampleCacheKey(
            CacheGenerationVersion,
            sample.Name,
            sample.FileName,
            sample.Query,
            sample.Category,
            sample.Format,
            CompilationOptionsFingerprint.Compute(snapshotOptions));
    }

    public static void Write(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        Write(sample, loggerResolver, SamplesDirectory);
    }

    public static void WriteProfiled(GeneratedCodeSample sample, ILoggerResolver loggerResolver)
    {
        Write(sample, loggerResolver, ProfiledSamplesDirectory);
    }

    private static void Write(
        GeneratedCodeSample sample,
        ILoggerResolver loggerResolver,
        string samplesDirectory)
    {
        Directory.CreateDirectory(samplesDirectory);
        File.WriteAllText(
            Path.Combine(samplesDirectory, sample.FileName),
            NormalizeLineEndingsForSnapshot(GenerateUncachedForRefresh(sample, loggerResolver)),
            Utf8WithBom);
    }

    public static string NormalizeForComparison(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .TrimEnd();

        return GeneratedNamespaceRegex.Replace(normalized, "namespace __generated_sample_namespace__");
    }

    private static string NormalizeLineEndingsForSnapshot(string text)
    {
        return text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");
    }

    private static string FormatSample(GeneratedCodeSample sample, BuildItems buildItems, CSharpCompilation compilation)
    {
        var builder = new StringBuilder();

        AppendInspectionSection(builder, "Parsed Query", sample.Query);
        AppendInspectionSection(builder, "Logical Plan", GetLogicalPlanText(sample, buildItems));
        AppendInspectionSection(builder, "Physical Plan", GetPhysicalPlanText(sample, buildItems));
        AppendInspectionSection(builder, "Execution Plan", GetExecutionPlanText(buildItems));
        builder.AppendLine("// === Generated C# ===");
        builder.AppendLine();

        foreach (var tree in compilation.SyntaxTrees)
        {
            if (sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode &&
                TryAppendCombinedSyntaxTrees(builder, tree))
                continue;

            var normalized = tree.GetRoot().NormalizeWhitespace();
            var formatted = sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode
                ? normalized
                : new SwitchExpressionBraceFormatter().Visit(normalized);
            var formattedText = GeneratedCSharpCodeFormatter.Normalize(formatted.ToFullString());

            if (sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode &&
                TryAppendFormattedCombinedSyntaxTrees(builder, tree.FilePath, formattedText))
                continue;

            builder.AppendLine($"// === SyntaxTree: {tree.FilePath} ===");
            builder.AppendLine(formattedText);
            builder.AppendLine();
        }

        return $"{builder.ToString().TrimEnd()}{Environment.NewLine}";
    }

    private static bool TryAppendCombinedSyntaxTrees(StringBuilder builder, SyntaxTree tree)
    {
        return TryAppendRawCombinedSyntaxTrees(builder, tree.FilePath, tree.GetRoot().ToFullString());
    }

    private static bool TryAppendRawCombinedSyntaxTrees(
        StringBuilder builder,
        string filePath,
        string treeText)
    {
        var markers = Regex.Matches(
            treeText,
            @"(?m)^[\uFEFF \t]*// === SYNTAX TREE \d+ ===\r?\n",
            RegexOptions.Compiled);

        if (markers.Count == 0)
            return false;

        for (var index = 0; index < markers.Count; index++)
        {
            var start = markers[index].Index + markers[index].Length;
            var end = index + 1 < markers.Count ? markers[index + 1].Index : treeText.Length;
            var syntaxTreeText = treeText[start..end].Trim();

            if (syntaxTreeText.Length == 0)
                continue;

            var normalized = CSharpSyntaxTree.ParseText(syntaxTreeText).GetRoot().NormalizeWhitespace();
            builder.AppendLine($"// === SyntaxTree: {filePath} ===");
            builder.AppendLine(GeneratedCSharpCodeFormatter.Normalize(normalized.ToFullString()));
            builder.AppendLine();
        }

        return true;
    }

    private static bool TryAppendFormattedCombinedSyntaxTrees(
        StringBuilder builder,
        string filePath,
        string formattedText)
    {
        var markers = Regex.Matches(
            formattedText,
            @"// === SYNTAX TREE \d+ ===",
            RegexOptions.Compiled);

        if (markers.Count == 0)
            return false;

        for (var index = 0; index < markers.Count; index++)
        {
            var start = markers[index].Index + markers[index].Length;
            var end = index + 1 < markers.Count ? markers[index + 1].Index : formattedText.Length;
            var syntaxTreeText = formattedText[start..end].Trim();

            if (syntaxTreeText.Length == 0)
                continue;

            var normalized = CSharpSyntaxTree.ParseText(syntaxTreeText).GetRoot().NormalizeWhitespace();
            builder.AppendLine($"// === SyntaxTree: {filePath} ===");
            builder.AppendLine(GeneratedCSharpCodeFormatter.Normalize(normalized.ToFullString()));
            builder.AppendLine();
        }

        return true;
    }

    private static string FormatSample(GeneratedCodeSample sample, QueryInspectionResult inspection)
    {
        var builder = new StringBuilder();

        AppendInspectionSection(builder, "Parsed Query", sample.Query);
        AppendInspectionSection(builder, "Logical Plan", inspection.LogicalPlanText);
        AppendInspectionSection(builder, "Physical Plan", inspection.PhysicalPlanText);
        AppendInspectionSection(builder, "Execution Plan", inspection.ExecutionPlanText);
        builder.AppendLine("// === Generated C# ===");
        builder.AppendLine();

        if (sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode &&
            TryAppendRawCombinedSyntaxTrees(builder, string.Empty, inspection.GeneratedCSharpCode))
            return $"{builder.ToString().TrimEnd()}{Environment.NewLine}";

        var normalized = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode).GetRoot().NormalizeWhitespace();
        var formatted = sample.Format == GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode
            ? normalized
            : new SwitchExpressionBraceFormatter().Visit(normalized);
        builder.AppendLine("// === SyntaxTree:  ===");
        builder.AppendLine(GeneratedCSharpCodeFormatter.Normalize(formatted.ToFullString()));

        return $"{builder.ToString().TrimEnd()}{Environment.NewLine}";
    }

    private static void AppendInspectionSection(StringBuilder builder, string title, string content)
    {
        builder.AppendLine($"// === {title} ===");
        builder.AppendLine("/*");
        builder.AppendLine(EscapeBlockComment(content.Trim()));
        builder.AppendLine("*/");
        builder.AppendLine();
    }

    private static string GetLogicalPlanText(GeneratedCodeSample sample, BuildItems buildItems)
    {
        if (buildItems.LogicalPlan is { } logicalPlan)
            return LogicalPlanPrinter.Print(logicalPlan);

        throw new InvalidOperationException($"Logical plan should not be null for {sample.FileName}.");
    }

    private static string GetPhysicalPlanText(GeneratedCodeSample sample, BuildItems buildItems)
    {
        if (buildItems.PhysicalPlan is { } physicalPlan)
            return PhysicalPlanPrinter.Print(physicalPlan);

        throw new InvalidOperationException($"Physical plan should not be null for {sample.FileName}.");
    }

    private static string GetExecutionPlanText(BuildItems buildItems)
    {
        if (buildItems.ExecutionPlanText is { } executionPlanText)
            return executionPlanText;

        return ExecutionPlanPrinter.PrintUnsupported(
            "Execution IR inspection was not produced by the compilation pipeline.");
    }

    private static string EscapeBlockComment(string content)
    {
        return content.Replace("*/", "* /", StringComparison.Ordinal);
    }

    private static string CreateAssemblyName(GeneratedCodeSample sample)
    {
        var name = InvalidIdentifierPartRegex.Replace(Path.GetFileNameWithoutExtension(sample.FileName), "_");

        if (string.IsNullOrWhiteSpace(name))
            return "GeneratedSample";

        if (char.IsDigit(name[0]))
            name = $"_{name}";

        return $"GeneratedSample_{name}";
    }

    private static string[] GetDiagnosticErrors(BuildItems buildItems)
    {
        return buildItems.DiagnosticContext.Errors
            .Select(static diagnostic => diagnostic.ToString())
            .ToArray();
    }

    private static string FindRepoRoot()
    {
        var directory = Directory.GetCurrentDirectory();

        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory, "global.json")))
                return directory;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the current directory.");
    }

    private sealed class SwitchExpressionBraceFormatter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            var visited = base.VisitSwitchExpression(node) ??
                throw new InvalidOperationException("Switch expression visitor returned null.");
            node = (SwitchExpressionSyntax)visited;

            var indentation = node.OpenBraceToken.LeadingTrivia
                .Where(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));

            var closeBrace = node.CloseBraceToken
                .WithLeadingTrivia(SyntaxFactory.TriviaList(
                    [SyntaxFactory.CarriageReturnLineFeed, .. indentation]));

            return node.WithCloseBraceToken(closeBrace);
        }
    }

    private readonly record struct GeneratedCodeSampleCacheKey(
        int GenerationVersion,
        string Name,
        string FileName,
        string Query,
        string Category,
        GeneratedCodeSampleFormat Format,
        string CompilationOptionsFingerprint);
}
