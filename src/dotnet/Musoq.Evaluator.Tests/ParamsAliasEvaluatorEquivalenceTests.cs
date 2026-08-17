using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ParamsAliasEvaluatorEquivalenceTests : BasicEntityTestBase
{
    [TestMethod]
    public void ParamsAlias_ShouldMatchScalarDefaultsMetadataAndFiltering()
    {
        const string script = """
            (country: string, marker: string = '!')
            select Name + $marker as Label, $country as Requested
            from #A.Entities()
            where Country = $country
            order by Name
            """;
        var pair = CompilePair(script, CreateSources());

        AssertParameterMetadataEqual(pair.Canonical, pair.Alias);
        pair.Canonical.Parameters["country"] = "PL";
        pair.Alias.Parameters["country"] = "PL";

        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchCollectionPredicateAndCaseSensitiveNames()
    {
        const string script = """
            (ids: int[], lower: string, Upper: string)
            select Name, $lower as Lower, $Upper as Upper
            from #A.Entities()
            where Id in $ids
            order by Name
            """;
        var pair = CompilePair(script, CreateSources());

        pair.Canonical.Parameters["ids"] = new[] { 1, 3 };
        pair.Canonical.Parameters["lower"] = "lower";
        pair.Canonical.Parameters["Upper"] = "upper";
        pair.Alias.Parameters["ids"] = new[] { 1, 3 };
        pair.Alias.Parameters["lower"] = "lower";
        pair.Alias.Parameters["Upper"] = "upper";

        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchGroupingHavingOrderingAndPaginationInputs()
    {
        const string script = """
            (country: string, minimum: int)
            select Country, Count(Name) as Total
            from #A.Entities()
            where Country in ($country, 'DE')
            group by Country
            having Count(Name) >= $minimum
            order by Country
            """;
        var pair = CompilePair(script, CreateSources());

        pair.Canonical.Parameters["country"] = "PL";
        pair.Canonical.Parameters["minimum"] = 2;
        pair.Alias.Parameters["country"] = "PL";
        pair.Alias.Parameters["minimum"] = 2;

        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchCteJoinWindowAndOrderingExecution()
    {
        const string script = """
            (country: string, city: string, suffix: string)
            with filteredA as (
                select Name, Country, $suffix as Label
                from #A.Entities()
                where Country = $country
            ),
            filteredB as (
                select Name, Country, $city as RequestedCity
                from #B.Entities()
                where City = $city
            )
            select a.Name as LeftName,
                   b.Name as RightName,
                   b.RequestedCity,
                   RowNumber() over(order by a.Name) as RowNumber
            from filteredA a
            inner join filteredB b on a.Country = b.Country
            order by a.Name, b.Name
            """;
        var pair = CompilePair(script, CreateSources());

        pair.Canonical.Parameters["country"] = "PL";
        pair.Canonical.Parameters["city"] = "Warsaw";
        pair.Canonical.Parameters["suffix"] = "-cte";
        pair.Alias.Parameters["country"] = "PL";
        pair.Alias.Parameters["city"] = "Warsaw";
        pair.Alias.Parameters["suffix"] = "-cte";

        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchRepeatedExecutionRebinding()
    {
        const string script = """
            (country: string)
            select Name
            from #A.Entities()
            where Country = $country
            order by Name
            """;
        var pair = CompilePair(script, CreateSources());

        pair.Canonical.Parameters["country"] = "PL";
        pair.Alias.Parameters["country"] = "PL";
        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));

        pair.Canonical.Parameters["country"] = "DE";
        pair.Alias.Parameters["country"] = "DE";
        AssertTablesEqual(
            pair.Canonical.Run(CancellationToken.None),
            pair.Alias.Run(CancellationToken.None));
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchDefaultedSourceArgumentExecution()
    {
        const string script = """
            (key: string = 'KEY_2')
            select Key, Value
            from #EnvironmentVariables.All($key)
            """;
        var canonicalProvider = new ParameterizedSchemaProvider();
        var aliasProvider = new ParameterizedSchemaProvider();
        var canonical = CompileWithProvider("param" + script, canonicalProvider);
        var alias = CompileWithProvider("params" + script, aliasProvider);

        canonical.Parameters["key"] = "KEY_1";
        alias.Parameters["key"] = "KEY_1";

        AssertParameterMetadataEqual(canonical, alias);
        AssertTablesEqual(
            canonical.Run(CancellationToken.None),
            alias.Run(CancellationToken.None));
        Assert.AreEqual(1, canonicalProvider.OpenCount);
        Assert.AreEqual(1, aliasProvider.OpenCount);
    }

    [TestMethod]
    [DataRow(
        "param(author: string); param(limit: int); select 1 from #EnvironmentVariables.All()",
        "params(author: string); params(limit: int); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3056_DuplicateScriptParameterBlock)]
    [DataRow(
        "select 1 from #EnvironmentVariables.All(); param(author: string)",
        "select 1 from #EnvironmentVariables.All(); params(author: string)",
        DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement)]
    [DataRow(
        "param(author: string, author: int); select 1 from #EnvironmentVariables.All()",
        "params(author: string, author: int); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3058_DuplicateScriptParameterName)]
    [DataRow(
        "param(author: string); select $missing from #EnvironmentVariables.All()",
        "params(author: string); select $missing from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3059_UndeclaredScriptParameter)]
    [DataRow(
        "param(author: object); select 1 from #EnvironmentVariables.All()",
        "params(author: object); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType)]
    [DataRow(
        "param(limit: int = 'abc'); select 1 from #EnvironmentVariables.All()",
        "params(limit: int = 'abc'); select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault)]
    [DataRow(
        "param(name: string); select 1 from #EnvironmentVariables.All($name)",
        "params(name: string); select 1 from #EnvironmentVariables.All($name)",
        DiagnosticCode.MQ3062_InvalidScriptParameterSourceArgument)]
    [DataRow(
        "param(name: string); let name: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        "params(name: string); let name: string = 'KEY_1'; select 1 from #EnvironmentVariables.All()",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName)]
    public void ParamsAlias_ShouldPreserveBindingDiagnostics(
        string canonicalQuery,
        string aliasQuery,
        DiagnosticCode expectedCode)
    {
        var canonical = CompileFailure(canonicalQuery);
        var alias = CompileFailure(aliasQuery);

        Assert.HasCount(1, canonical.Envelopes);
        Assert.HasCount(1, alias.Envelopes);
        Assert.AreEqual(expectedCode, canonical.PrimaryEnvelope.Code);
        Assert.AreEqual(expectedCode, alias.PrimaryEnvelope.Code);
        Assert.AreEqual(canonical.PrimaryEnvelope.Phase, alias.PrimaryEnvelope.Phase);
        Assert.AreEqual(canonical.PrimaryEnvelope.Message, alias.PrimaryEnvelope.Message);
    }

    [TestMethod]
    public void ParamsAlias_ShouldAllowMixedBlocksAndKeepDuplicateBlockDiagnostic()
    {
        var exception = CompileFailure(
            "param(author: string); params(limit: int); select 1 from #EnvironmentVariables.All()");

        Assert.AreEqual(DiagnosticCode.MQ3056_DuplicateScriptParameterBlock, exception.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, exception.PrimaryEnvelope.Phase);
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchDiagnosticsCompilation()
    {
        const string canonicalQuery =
            "param(limit: int = 'abc'); select 1 from #EnvironmentVariables.All()";
        const string aliasQuery =
            "params(limit: int = 'abc'); select 1 from #EnvironmentVariables.All()";
        var canonical = InstanceCreator.CompileWithDiagnostics(
            canonicalQuery,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var alias = InstanceCreator.CompileWithDiagnostics(
            aliasQuery,
            Guid.NewGuid().ToString(),
            new EnvironmentVariablesSchemaProvider(),
            LoggerResolver);
        var canonicalEnvelope = canonical.ToEnvelopes().Single();
        var aliasEnvelope = alias.ToEnvelopes().Single();

        Assert.AreEqual(canonicalEnvelope.Code, aliasEnvelope.Code);
        Assert.AreEqual(canonicalEnvelope.Phase, aliasEnvelope.Phase);
        Assert.AreEqual(canonicalEnvelope.Message, aliasEnvelope.Message);
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchMissingParameterValidationBeforeSourceOpening()
    {
        const string script = """
            (key: string)
            select Key from #EnvironmentVariables.All()
            where Key = $key
            """;
        var canonicalProvider = new ParameterizedSchemaProvider();
        var aliasProvider = new ParameterizedSchemaProvider();
        var canonical = CompileWithProvider("param" + script, canonicalProvider);
        var alias = CompileWithProvider("params" + script, aliasProvider);

        var canonicalException = RunFailure(canonical);
        var aliasException = RunFailure(alias);

        AssertRuntimeExceptionsEqual(canonicalException, aliasException);
        Assert.AreEqual(0, canonicalProvider.OpenCount);
        Assert.AreEqual(0, aliasProvider.OpenCount);
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchWrongTypeValidationBeforeSourceOpening()
    {
        const string script = """
            (limit: int)
            select Key from #EnvironmentVariables.All()
            """;
        var canonicalProvider = new ParameterizedSchemaProvider();
        var aliasProvider = new ParameterizedSchemaProvider();
        var canonical = CompileWithProvider("param" + script, canonicalProvider);
        var alias = CompileWithProvider("params" + script, aliasProvider);
        canonical.Parameters["limit"] = "10";
        alias.Parameters["limit"] = "10";

        AssertRuntimeExceptionsEqual(RunFailure(canonical), RunFailure(alias));
        Assert.AreEqual(0, canonicalProvider.OpenCount);
        Assert.AreEqual(0, aliasProvider.OpenCount);
    }

    [TestMethod]
    public void ParamsAlias_ShouldMatchUnknownParameterValidationBeforeSourceOpening()
    {
        const string script = """
            (key: string)
            select Key from #EnvironmentVariables.All()
            """;
        var canonicalProvider = new ParameterizedSchemaProvider();
        var aliasProvider = new ParameterizedSchemaProvider();
        var canonical = CompileWithProvider("param" + script, canonicalProvider);
        var alias = CompileWithProvider("params" + script, aliasProvider);
        canonical.Parameters["key"] = "KEY_1";
        alias.Parameters["key"] = "KEY_1";
        canonical.Parameters["extra"] = "ignored";
        alias.Parameters["extra"] = "ignored";

        AssertRuntimeExceptionsEqual(RunFailure(canonical), RunFailure(alias));
        Assert.AreEqual(0, canonicalProvider.OpenCount);
        Assert.AreEqual(0, aliasProvider.OpenCount);
    }

    private MusoqQueryException CompileFailure(string query)
    {
        return Assert.Throws<MusoqQueryException>(() =>
            InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                new EnvironmentVariablesSchemaProvider(),
                LoggerResolver));
    }

    private CompiledQuery CompileWithProvider(
        string query,
        ParameterizedSchemaProvider provider)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver);
    }

    private (CompiledQuery Canonical, CompiledQuery Alias) CompilePair(
        string script,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        return (
            CreateAndRunVirtualMachine("param" + script, sources),
            CreateAndRunVirtualMachine("params" + script, sources));
    }

    private static QueryExecutionException RunFailure(CompiledQuery query)
    {
        return Assert.Throws<QueryExecutionException>(() => _ = query.Run(CancellationToken.None).Count);
    }

    private static void AssertRuntimeExceptionsEqual(
        QueryExecutionException canonical,
        QueryExecutionException alias)
    {
        Assert.IsNotNull(canonical.Envelope);
        Assert.IsNotNull(alias.Envelope);
        Assert.AreEqual(canonical.Envelope!.Code, alias.Envelope!.Code);
        Assert.AreEqual(canonical.Envelope.Phase, alias.Envelope.Phase);
        Assert.AreEqual(canonical.Envelope.Message, alias.Envelope.Message);
    }

    private static void AssertParameterMetadataEqual(CompiledQuery canonical, CompiledQuery alias)
    {
        CollectionAssert.AreEqual(
            canonical.ParameterDefinitions.ToArray(),
            alias.ParameterDefinitions.ToArray());
        CollectionAssert.AreEqual(
            canonical.ParameterContracts.ToArray(),
            alias.ParameterContracts.ToArray());
        CollectionAssert.AreEqual(
            canonical.RequiredParameters.ToArray(),
            alias.RequiredParameters.ToArray());
    }

    private static void AssertTablesEqual(Table canonical, Table alias)
    {
        CollectionAssert.AreEqual(canonical.Columns.ToArray(), alias.Columns.ToArray());
        Assert.AreEqual(canonical.Count, alias.Count);

        for (var index = 0; index < canonical.Count; index++)
            CollectionAssert.AreEqual(canonical[index].Values, alias[index].Values);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                [
                    new BasicEntity { Name = "Alice", Country = "PL", City = "Warsaw", Id = 1 },
                    new BasicEntity { Name = "Bob", Country = "DE", City = "Berlin", Id = 2 },
                    new BasicEntity { Name = "Cara", Country = "PL", City = "Krakow", Id = 3 },
                    new BasicEntity { Name = "Dora", Country = "FR", City = "Paris", Id = 4 }
                ]
            },
            {
                "#B",
                [
                    new BasicEntity { Name = "TargetWarsaw", Country = "PL", City = "Warsaw", Id = 1 },
                    new BasicEntity { Name = "TargetBerlin", Country = "DE", City = "Berlin", Id = 2 }
                ]
            }
        };
    }

    private sealed class ParameterizedSchemaProvider : ISchemaProvider
    {
        private readonly ParameterizedSchema _schema = new();

        public int OpenCount => _schema.OpenCount;

        public ISchema GetSchema(string schema)
        {
            return _schema;
        }
    }

    private sealed class ParameterizedSchema : SchemaBase
    {
        private static readonly EnvironmentVariableEntity[] Rows =
        [
            new("KEY_1", "VALUE_1"),
            new("KEY_2", "VALUE_2")
        ];

        public ParameterizedSchema()
            : base("parameterized", new MethodsAggregator(new MethodsManager()))
        {
        }

        public int OpenCount { get; private set; }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new EnvironmentVariableEntityTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            OpenCount++;
            var key = parameters.Length > 0 ? parameters[0] as string : null;
            var rows = key == null
                ? Rows
                : Rows.Where(row => row.Key == key).ToArray();

            return EnsureSourceType<T, EnvironmentVariableEntity>(
                name,
                new ParameterizedSource(rows));
        }

        private sealed class ParameterizedSource(IReadOnlyList<EnvironmentVariableEntity> rows)
            : RowSource<EnvironmentVariableEntity>
        {
            public override IEnumerable<IReadOnlyList<EnvironmentVariableEntity>> Chunks => [rows];
        }
    }
}
