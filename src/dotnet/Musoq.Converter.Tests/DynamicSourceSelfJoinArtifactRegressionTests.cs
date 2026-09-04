using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DynamicSourceSelfJoinArtifactRegressionTests
{
    private const string Query = """
        select p.Who, p2.Age
        from #dynamic.all() p
        inner join #dynamic.all() p2 on p.Who = p2.Who
        where p.Age > 26
        """;

    private const string SingleSourceQuery = """
        select p.Who, p.Age
        from #dynamic.all() p
        """;

    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void DynamicSelfJoin_ArtifactCompilationAndExecutionShouldMatchDirectCompilation()
    {
        var provider = CreateProvider(
        [
            new SchemaColumn("Who", 7, typeof(string)),
            new SchemaColumn("Age", 7, typeof(int))
        ]);

        var direct = InstanceCreator.CompileWithDiagnostics(
            Query,
            $"DynamicSelfJoin_Direct_{Guid.NewGuid():N}",
            provider,
            _loggerResolver);

        Assert.IsTrue(direct.Succeeded, FormatDiagnostics(direct.Diagnostics, direct.CaughtException));
        var directRows = ReadRows(direct.CompiledQuery!);
        CollectionAssert.AreEqual(new[] { ("Katarina", 43) }, directRows);

        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(
            Query,
            $"DynamicSelfJoin_Artifact_{Guid.NewGuid():N}",
            provider,
            _loggerResolver);

        Assert.IsTrue(artifact.Succeeded, FormatDiagnostics(artifact.Diagnostics, artifact.CaughtException));
        Assert.IsNull(artifact.CaughtException, FormatDiagnostics(artifact.Diagnostics, artifact.CaughtException));
        Assert.IsNotNull(artifact.Artifact);

        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(
            Query,
            artifact.Artifact!,
            provider,
            _loggerResolver);

        Assert.IsTrue(loaded.Succeeded, FormatDiagnostics(loaded.Diagnostics, loaded.CaughtException));
        Assert.IsNull(loaded.CaughtException, FormatDiagnostics(loaded.Diagnostics, loaded.CaughtException));
        CollectionAssert.AreEqual(directRows, ReadRows(loaded.CompiledQuery!));
    }

    [TestMethod]
    public void DynamicSelfJoin_TargetPackageShouldExposeUniqueSourceAbiIndicesAndPreserveAliases()
    {
        var provider = CreateProvider(
        [
            new SchemaColumn("Who", 7, typeof(string)),
            new SchemaColumn("Age", 7, typeof(int))
        ]);

        var package = InstanceCreator.CompileTargetPackageWithDiagnostics(
            Query,
            $"DynamicSelfJoin_Package_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            ExecutionTargetIds.CSharpClr);

        Assert.IsTrue(package.Succeeded, FormatDiagnostics(package.Diagnostics, package.CaughtException));
        Assert.IsNull(package.CaughtException, FormatDiagnostics(package.Diagnostics, package.CaughtException));
        Assert.IsNotNull(package.BuildItems);
        Assert.IsNotNull(package.BuildItems!.TargetRuntimeContract);
        Assert.IsNotNull(package.Package);

        var sourceContracts = package.BuildItems.TargetRuntimeContract!.SourceAccess
            .Where(static source => source.Kind == "schema-source")
            .ToArray();
        Assert.HasCount(2, sourceContracts);

        foreach (var source in sourceContracts)
        {
            CollectionAssert.AreEqual(new[] { 0, 1 }, source.Fields.Select(static field => field.Index).ToArray());
        }

        CollectionAssert.AreEquivalent(
            new[] { "p.Who", "p.Age", "p2.Who", "p2.Age" },
            sourceContracts
                .SelectMany(static source => source.Fields)
                .Select(static field => field.QualifiedName)
                .ToArray());

        var sourceImports = package.Package!.HostAbiInventory.Imports
            .Where(static import => import.Kind == TargetHostAbiImportKind.SourceAccess)
            .ToArray();
        Assert.HasCount(2, sourceImports);
        foreach (var sourceImport in sourceImports)
        {
            var details = Assert.IsInstanceOfType<TargetSourceAccessAbiDetails>(sourceImport.Details);
            CollectionAssert.AreEqual(new[] { 0, 1 }, details.Fields.Select(static field => field.Index).ToArray());
        }
    }

    [TestMethod]
    public void DynamicSource_WhenPhysicalIndicesAreSparseAndOutOfOrder_ShouldUseDenseAbiIndices()
    {
        var provider = CreateProvider(
        [
            new SchemaColumn("Who", 4, typeof(string)),
            new SchemaColumn("Age", 1, typeof(int))
        ]);

        var package = InstanceCreator.CompileTargetPackageWithDiagnostics(
            SingleSourceQuery,
            $"DynamicSource_SparsePackage_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            ExecutionTargetIds.CSharpClr);

        Assert.IsTrue(package.Succeeded, FormatDiagnostics(package.Diagnostics, package.CaughtException));
        Assert.IsNull(package.CaughtException, FormatDiagnostics(package.Diagnostics, package.CaughtException));

        var source = package.BuildItems!.TargetRuntimeContract!.SourceAccess
            .Single(static candidate => candidate.Kind == "schema-source");

        CollectionAssert.AreEqual(new[] { 0, 1 }, source.Fields.Select(static field => field.Index).ToArray());
        CollectionAssert.AreEqual(new[] { "Who", "Age" }, source.Fields.Select(static field => field.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "p.Who", "p.Age" }, source.Fields.Select(static field => field.QualifiedName).ToArray());
    }

    private static DynamicRowsSchemaProvider CreateProvider(IReadOnlyList<ISchemaColumn> schemaColumns)
    {
        var columns = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["Who"] = typeof(string),
            ["Age"] = typeof(int)
        };
        var rows = new IReadOnlyDictionary<string, object>[]
        {
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Who"] = "Katarina",
                ["Age"] = 43
            },
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["Who"] = "Marek",
                ["Age"] = 25
            }
        };

        return new DynamicRowsSchemaProvider(columns, rows, schemaColumns);
    }

    private static (string Who, int Age)[] ReadRows(CompiledQuery compiledQuery)
    {
        using var table = compiledQuery.Run();
        var rows = new (string Who, int Age)[table.Count];
        for (var index = 0; index < table.Count; index++)
        {
            rows[index] = ((string)table[index][0]!, (int)table[index][1]!);
        }

        return rows;
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, Exception? caughtException)
    {
        return $"{caughtException}{Environment.NewLine}" +
               string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()));
    }
}
