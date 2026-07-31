using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Targets.Abstractions;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class AbiImportDuplicationReproductionTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileArtifact_WhenNullablePluginPredicateIsNestedOrRepeated_ShouldNotDuplicateAbiImport()
    {
        var provider = new PluginArtifactSchemaProvider("folder/file");

        var queries = new[]
        {
            """
            select
                case
                    when Contains(i.Value, '/') then 'path'
                    else 'name'
                end as Kind
            from #artifact.items() i
            """,
            """
            with first as (
                select i.Value
                from #artifact.items() i
            ), second as (
                select
                    case
                        when f.Value = '' then 'empty'
                        else case
                            when Contains(f.Value, '/') then 'path'
                            else 'name'
                        end
                    end as Kind
                from first f
            )
            select s.Kind
            from second s
            """
        };

        for (var queryIndex = 0; queryIndex < queries.Length; queryIndex++)
        {
            var query = queries[queryIndex];
            var direct = InstanceCreator.CompileWithDiagnostics(
                query,
                $"AbiImportDuplication_Direct_{queryIndex}",
                provider,
                _loggerResolver);

            Assert.IsTrue(
                direct.Succeeded,
                FormatFailure($"Direct compilation {queryIndex}", direct));

            using (var table = direct.CompiledQuery.Run())
            {
                Assert.HasCount(1, table);
                Assert.AreEqual("path", table[0][0]);
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var artifact = InstanceCreator.CompileArtifactWithDiagnostics(
                    query,
                    $"AbiImportDuplication_Artifact_{queryIndex}_{attempt}",
                    provider,
                    _loggerResolver);

                Assert.IsTrue(
                    artifact.Succeeded,
                    FormatFailure($"Artifact compilation {queryIndex}/{attempt}", artifact));
                Assert.IsNotNull(artifact.Artifact);

                var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
                    query,
                    artifact.Artifact!,
                    provider,
                    _loggerResolver);

                Assert.IsTrue(
                    loaded.Succeeded,
                    FormatFailure($"Artifact loading {queryIndex}/{attempt}", loaded));
                using (var table = loaded.CompiledQuery.Run())
                {
                    Assert.HasCount(1, table);
                    Assert.AreEqual("path", table[0][0]);
                }

                var package = InstanceCreator.CompileTargetPackageWithDiagnostics(
                    query,
                    $"AbiImportDuplication_Package_{queryIndex}_{attempt}",
                    provider,
                    _loggerResolver,
                    ExecutionTargetIds.CSharpClr);

                Assert.IsTrue(
                    package.Succeeded,
                    FormatFailure($"Package compilation {queryIndex}/{attempt}", package));
                Assert.IsNotNull(package.Package);
                Assert.HasCount(
                    1,
                    package.Package.HostAbiInventory.Imports.Where(import =>
                        import.Kind == TargetHostAbiImportKind.PluginInvocation &&
                        import.Name.Contains(nameof(LibraryBase.Contains), StringComparison.Ordinal)));
            }
        }
    }

    private static string FormatFailure(string attempt, ArtifactBuildResult result)
    {
        return $"{attempt}{System.Environment.NewLine}" +
               $"{result.CaughtException}{System.Environment.NewLine}" +
               string.Join(
                   System.Environment.NewLine,
                   result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }

    private static string FormatFailure(string attempt, BuildResult result)
    {
        return $"{attempt}{System.Environment.NewLine}" +
               $"{result.CaughtException}{System.Environment.NewLine}" +
               string.Join(
                   System.Environment.NewLine,
                   result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }

    private static string FormatFailure(string attempt, TargetPackageBuildResult result)
    {
        return $"{attempt}{System.Environment.NewLine}" +
               $"{result.CaughtException}{System.Environment.NewLine}" +
               string.Join(
                   System.Environment.NewLine,
                   result.Diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}

public sealed class PluginArtifactSchemaProvider(string value, LibraryBase? library = null) : ISchemaProvider
{
    private readonly PluginArtifactSchema _schema = new(value, library);

    public ISchema GetSchema(string schemaName)
    {
        return _schema;
    }
}

public sealed class PluginArtifactSchema(string value, LibraryBase? library = null) : SchemaBase("artifact", CreateLibrary(library))
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new ArtifactTable(typeof(ArtifactRow), typeof(string));
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        return EnsureSourceType<T, ArtifactRow>(name, new ArtifactRowSource(value));
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<ArtifactRowSource>("items");
    }

    private static MethodsAggregator CreateLibrary(LibraryBase? library)
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(library ?? new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
