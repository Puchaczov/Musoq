using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Musoq.Benchmarks;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Schema;
using Musoq.Tests.Common;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Evaluator.Tests;

internal sealed record GeneratedBenchmarkSnapshots(
    BenchmarkSnapshotManifest Manifest,
    IReadOnlyDictionary<string, string> Files
)
{
    internal static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    internal void WriteTo(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var (file, text) in Files)
            File.WriteAllText(Path.Combine(directory, file), text, Encoding);
        ArtifactJson.Write(Path.Combine(directory, BenchmarkSnapshotManifest.FileName), Manifest);
    }
}

internal static class BenchmarkSnapshotGenerator
{
    internal static string DirectoryPath { get; } =
        Path.Combine(RepositoryPaths.FindRoot(), BenchmarkSnapshotManifest.RelativeDirectory);

    internal static GeneratedBenchmarkSnapshots Generate()
    {
        using var culture = new CultureScope(Culture.DefaultCulture);
        var cases = CampaignCases.CreateComplete(new CampaignOptions(Mode: "Official"), Path.GetTempPath());
        var lifecycle = CaseLifecycleCatalog.Read(
            Path.Combine(RepositoryPaths.FindRoot(), CaseLifecycleCatalog.RelativePath),
            cases.Select(item => item.Definition.Id)
        );
        var active = cases.Where(item => lifecycle.Retirement(item.Definition.Id) is null);
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var mappings = new List<BenchmarkSnapshotCase>();
        foreach (var workload in active.GroupBy(DescribeWorkload))
            GenerateWorkload(workload, files, mappings);

        var manifest = new BenchmarkSnapshotManifest(
            ScenarioCatalog.SuiteVersion,
            mappings.OrderBy(item => item.Definition.Id, StringComparer.Ordinal).ToArray()
        );
        manifest.Validate();
        return new(manifest, files);
    }

    private static void GenerateWorkload(
        IGrouping<Workload, CampaignCase> workload,
        SortedDictionary<string, string> files,
        List<BenchmarkSnapshotCase> mappings
    )
    {
        var scenario = ScenarioCatalog.Get(workload.Key.ScenarioId);
        var variants = workload
            .OrderByDescending(item => item.Definition.InputCount)
            .GroupBy(item => item.Definition.InputCount)
            .Select(cases => new { Cases = cases.ToArray(), Text = GenerateText(scenario, workload.Key, cases.Key) })
            .GroupBy(variant => variant.Text, StringComparer.Ordinal);
        var primary = true;
        foreach (var variant in variants)
        {
            var definitions = variant.SelectMany(item => item.Cases).Select(item => item.Definition).ToArray();
            var file = primary ? workload.Key.Name + ".cs" : $"{workload.Key.Name}_{definitions[0].Size}.cs";
            primary = false;
            files.Add(file, variant.Key);
            mappings.AddRange(
                definitions.Select(definition => new BenchmarkSnapshotCase(
                    BenchmarkSnapshotManifest.PortableDefinition(definition),
                    file,
                    workload.Key.OutputType?.FullName
                ))
            );
        }
    }

    private static string GenerateText(Scenario scenario, Workload workload, int count)
    {
        var provider = FixtureCatalog.Prepare(scenario, count).CreateSchemaProvider();
        var options = GeneratedCodeSampleArtifacts.CreateSnapshotCompilationOptions(
            ExecutionSettings.Create(scenario, count)
        );
        var input = new CompilationInput(scenario.Sql, "GeneratedSample_" + workload.Name, provider, options);
        var inspection = workload.Path switch
        {
            CompilationPath.Artifact => InspectArtifact(input),
            CompilationPath.Table => InspectTable(input),
            CompilationPath.Typed => InspectTyped(workload.OutputType, input),
            _ => throw new InvalidOperationException($"Unsupported benchmark compilation path {workload.Path}."),
        };
        return GeneratedCodeSampleArtifacts.NormalizeLineEndingsForSnapshot(
            GeneratedCodeSampleArtifacts.FormatInspection(
                scenario.Sql,
                GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
                inspection
            )
        );
    }

    private static QueryInspectionResult InspectTable(CompilationInput input)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            input.Sql,
            input.Assembly,
            input.Provider,
            BenchmarkLogger.Instance,
            input.Options,
            requireExecutionPlan: true,
            CancellationToken.None
        );
        using var query = result.CompiledQuery;
        if (!result.Succeeded || result.BuildItems is not { } items)
            throw new InvalidOperationException(
                $"Table inspection failed for {input.Assembly}: {string.Join(Environment.NewLine, result.Diagnostics)}",
                result.CaughtException
            );
        return InspectBuild(items, ExecutionTargetCatalog.InspectArtifact(items.RenderingArtifact));
    }

    private static QueryInspectionResult InspectArtifact(CompilationInput input)
    {
        var result = InstanceCreator.CompileTargetPackageWithDiagnostics(
            input.Sql,
            input.Assembly,
            input.Provider,
            BenchmarkLogger.Instance,
            ExecutionTargetIds.CSharpClr,
            input.Options
        );
        if (!result.Succeeded || result.BuildItems is not { } items || result.Inspection is not { } inspection)
            throw new InvalidOperationException(
                $"Artifact inspection failed for {input.Assembly}: {string.Join(Environment.NewLine, result.Diagnostics)}",
                result.CaughtException
            );
        return InspectBuild(items, inspection);
    }

    private static QueryInspectionResult InspectBuild(BuildItems items, RenderedQueryInspection inspection)
    {
        if (
            items.LogicalPlan is not { } logical
            || items.PhysicalPlan is not { } physical
            || items.ExecutionPlan is not { } execution
            || string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode)
        )
            throw new InvalidOperationException(
                $"Benchmark compilation did not provide all snapshot sections for {items.AssemblyName}."
            );

        return new QueryInspectionResult(
            logical,
            physical,
            LogicalPlanPrinter.Print(logical),
            PhysicalPlanPrinter.Print(physical),
            inspection.GeneratedCSharpCode
        )
        {
            ExecutionPlanText = items.ExecutionPlanText ?? ExecutionPlanPrinter.Print(execution),
        };
    }

    private static QueryInspectionResult InspectTyped(Type? outputType, CompilationInput input)
    {
        var result = outputType switch
        {
            var type when type == typeof(ProjectionOutput) => InspectTyped<ProjectionOutput>(input),
            var type when type == typeof(GroupOutput) => InspectTyped<GroupOutput>(input),
            var type when type == typeof(WindowOutput) => InspectTyped<WindowOutput>(input),
            _ => throw new InvalidOperationException($"No typed benchmark inspection registered for '{outputType}'."),
        };
        return result.Query
            ?? throw new InvalidOperationException(
                $"Typed inspection failed for {input.Assembly}: {string.Join(Environment.NewLine, result.OutputBindingDiagnostics)}"
            );
    }

    private static TypedQueryInspectionResult InspectTyped<T>(CompilationInput input) =>
        InstanceCreator.CompileForTypedInspection<T>(
            input.Sql,
            input.Assembly,
            input.Provider,
            BenchmarkLogger.Instance,
            input.Options
        );

    private static Workload DescribeWorkload(CampaignCase item)
    {
        var definition = item.Definition;
        var registration = BenchmarkRegistrations.For(item.Benchmark.Descriptor.Type);
        var path =
            definition.Phase == "artifact-load" ? CompilationPath.Artifact
            : definition.ResultApi == "Typed" ? CompilationPath.Typed
            : CompilationPath.Table;
        return new(definition.ScenarioId, path, registration.OutputType);
    }

    private enum CompilationPath
    {
        Table,
        Typed,
        Artifact,
    }

    private sealed record Workload(string ScenarioId, CompilationPath Path, Type? OutputType)
    {
        internal string Name => $"{ScenarioId}_Benchmark{Path}";
    }

    private sealed record CompilationInput(
        string Sql,
        string Assembly,
        ISchemaProvider Provider,
        CompilationOptions Options
    );
}
