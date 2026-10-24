using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QualifyCompiledQueryArtifactTests
{
    private const string PagingQuery = """
        param(offset: long, country: string)
        SELECT e.Name AS Name,
          ROW_NUMBER() OVER (ORDER BY e.Population DESC, e.Name) AS Position
        FROM #items.entities() e WHERE e.Country = $country
        QUALIFY Position > $offset AND Position <= $offset + 2
        ORDER BY Position
        """;

    private static readonly CompilationOptions Options = new(forceTableResultMaterialization: true);
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    [DataRow(CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash)]
    [DataRow(CompiledQueryArtifactValidationMode.Fast)]
    public void ParameterizedPaging_ValidatesAndReturnsIndependentExpectedRows(
        CompiledQueryArtifactValidationMode mode)
    {
        var artifact = Compile(PagingQuery, CreateProvider());
        using var query = Activate(PagingQuery, artifact, CreateProvider(), mode);
        BindPage(query, "A", 0);

        Assert.AreEqual("Ada:1|Bob:2", ReadPage(query));
    }

    [TestMethod]
    [DataRow(1L, "Bob:2|Cara:3")]
    [DataRow(2L, "Cara:3")]
    [DataRow(3L, "")]
    public void FastActivation_AfterStrictExecution_UsesChangedPagingParameters(long offset, string expected)
    {
        var artifact = Compile(PagingQuery, CreateProvider());
        Prime(PagingQuery, artifact);
        using var query = Activate(PagingQuery, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.Fast);
        BindPage(query, "A", offset);

        Assert.AreEqual(expected, ReadPage(query));
    }

    [TestMethod]
    public void FastActivation_RebindsChangedProviderData()
    {
        var artifact = Compile(PagingQuery, CreateProvider());
        Prime(PagingQuery, artifact);
        var provider = CreateProvider([new EntitySetEntity { Name = "Fresh", Country = "A", Population = 300 }]);
        using var query = Activate(PagingQuery, artifact, provider, CompiledQueryArtifactValidationMode.Fast);
        BindPage(query, "A", 0);

        Assert.AreEqual("Fresh:1", ReadPage(query));
    }

    [TestMethod]
    public void PartitionedRank_PreservesTiesAndDeterministicOrderingAfterReload()
    {
        const string sql = """
            SELECT e.Name AS Name,
              RANK() OVER (PARTITION BY e.Country ORDER BY e.Population DESC) AS Position
            FROM #items.entities() e QUALIFY Position <= 2
            ORDER BY e.Country, Position, e.Name
            """;
        var artifact = Compile(sql, CreateProvider());
        using var first = Activate(sql, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash);
        using var second = Activate(sql, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.Fast);

        Assert.AreEqual(("Ada:1|Bob:1|Dora:1|Evan:2|Faye:2", "Ada:1|Bob:1|Dora:1|Evan:2|Faye:2"),
            (ReadPage(first), ReadPage(second)));
    }

    [TestMethod]
    public void CteJoinPaging_PreservesFilteringAndPositionsAfterReload()
    {
        const string sql = """
            param(offset: long, country: string)
            WITH selected AS (
              SELECT e.Name AS Name, e.Population AS Population
              FROM #items.entities() e WHERE e.Country = $country
            ), tags AS (
              SELECT t.Name AS Name FROM #tags.entities() t WHERE t.Country = 'chosen'
            )
            SELECT s.Name AS Name, ROW_NUMBER() OVER (ORDER BY s.Population DESC, s.Name) AS Position
            FROM selected s INNER JOIN tags t ON s.Name = t.Name
            QUALIFY Position > $offset AND Position <= $offset + 2 ORDER BY Position
            """;
        var artifact = Compile(sql, CreateProvider());
        using var first = Activate(sql, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash);
        using var second = Activate(sql, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.Fast);
        BindPage(first, "A", 0);
        BindPage(second, "A", 1);

        Assert.AreEqual(("Ada:1|Cara:2", "Cara:2"), (ReadPage(first), ReadPage(second)));
    }

    [TestMethod]
    public async Task OverlappingExecutions_KeepParametersAndProvidersIndependent()
    {
        var artifact = Compile(PagingQuery, CreateProvider());
        Prime(PagingQuery, artifact);
        using var barrier = new Barrier(2);
        using var first = Activate(PagingQuery, artifact, CreateProvider(new OverlappingRows(Rows(), barrier)),
            CompiledQueryArtifactValidationMode.Fast);
        using var second = Activate(PagingQuery, artifact, CreateProvider(new OverlappingRows(Rows(), barrier)),
            CompiledQueryArtifactValidationMode.Fast);
        BindPage(first, "A", 0);
        BindPage(second, "B", 1);

        var results = await Task.WhenAll(Task.Run(() => ReadPage(first)), Task.Run(() => ReadPage(second)));

        CollectionAssert.AreEqual(new[] { "Ada:1|Bob:2", "Evan:2|Faye:3" }, results);
    }

    private ICompiledQueryArtifact Compile(string sql, ISchemaProvider provider)
    {
        var result = InstanceCreator.CompileArtifactWithDiagnostics(sql, "QualifyArtifact", provider, _loggerResolver, Options);
        return result.Artifact ?? throw new AssertFailedException(DescribeFailure(result.Diagnostics));
    }

    private CompiledQuery Activate(string sql, ICompiledQueryArtifact artifact, ISchemaProvider provider,
        CompiledQueryArtifactValidationMode mode)
    {
        var result = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(sql, artifact, provider,
            _loggerResolver, new CompiledQueryArtifactLoadOptions { ValidationMode = mode }, Options);
        return result.CompiledQuery ?? throw new AssertFailedException(DescribeFailure(result.Diagnostics));
    }

    private void Prime(string sql, ICompiledQueryArtifact artifact)
    {
        using var query = Activate(sql, artifact, CreateProvider(), CompiledQueryArtifactValidationMode.StrictGeneratedCodeHash);
        BindPage(query, "A", 0);
        Assert.AreEqual("Ada:1|Bob:2", ReadPage(query));
    }

    private static string DescribeFailure(IEnumerable<Diagnostic> diagnostics) =>
        string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToDetailedString()));

    private static void BindPage(CompiledQuery query, string country, long offset)
    {
        query.TypedParameters.Set("country", country);
        query.TypedParameters.Set("offset", offset);
    }

    private static string ReadPage(CompiledQuery query)
    {
        using var table = query.Run();
        return string.Join("|", table.Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1)}"));
    }

    private static EntitySetSchemaProvider CreateProvider(IReadOnlyList<EntitySetEntity>? rows = null) => new(
        new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#items"] = rows ?? Rows(),
            ["#tags"] =
            [
                new EntitySetEntity { Name = "Ada", Country = "chosen" },
                new EntitySetEntity { Name = "Bob", Country = "other" },
                new EntitySetEntity { Name = "Cara", Country = "chosen" }
            ]
        });

    private static EntitySetEntity[] Rows() =>
    [
        new() { Name = "Cara", Country = "A", Population = 50 },
        new() { Name = "Bob", Country = "A", Population = 100 },
        new() { Name = "Ada", Country = "A", Population = 100 },
        new() { Name = "Faye", Country = "B", Population = 80 },
        new() { Name = "Dora", Country = "B", Population = 90 },
        new() { Name = "Evan", Country = "B", Population = 80 }
    ];

    private sealed class OverlappingRows(IReadOnlyList<EntitySetEntity> rows, Barrier barrier)
        : IReadOnlyList<EntitySetEntity>
    {
        public int Count => rows.Count;
        public EntitySetEntity this[int index] => rows[index];

        public IEnumerator<EntitySetEntity> GetEnumerator()
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("The two artifact executions did not overlap at source enumeration.");
            return rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
