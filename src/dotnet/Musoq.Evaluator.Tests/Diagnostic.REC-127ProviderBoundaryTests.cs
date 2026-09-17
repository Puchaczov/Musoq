using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRec127ProviderBoundaryTests
{
    private static readonly string[] QueryShapes =
    [
        "select e.Name from #boundary.items() e",
        "select e.Name as Label from #boundary.items() e",
        "select e.Name, e.Population from #boundary.items() e",
        "select e.Name from #boundary.items() e where e.Population >= 0",
        "select e.Name from #boundary.items() e order by e.Name",
        "select e.Name from #boundary.items() e take 1",
        "select Count(e.Name) as Total from #boundary.items() e",
        "with rows as (select e.Name from #boundary.items() e) select Name from rows",
        "select e.Name from #boundary.items() e inner join #boundary.items() f on e.Name = f.Name",
        "select e.Name from #boundary.items() e where e.Name is not null order by e.Name",
        "select e.Name from #boundary.items() e skip 0",
        "select e.Name from #boundary.items() e order by e.Name take 1"
    ];

    private static readonly IReadOnlyList<BoundaryCandidate> Candidates = CreateCandidates();

    public static IEnumerable<object[]> ProviderBoundaryCases()
    {
        foreach (var candidate in Candidates)
            yield return [candidate];
    }

    [TestMethod]
    public void CandidateMatrix_ShouldCoverFrozenBoundaryContract()
    {
        Assert.AreEqual(48, Candidates.Count);
        Assert.AreEqual(48, Candidates.Select(static candidate => candidate.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(12, Candidates.Count(static candidate => candidate.Operation == BoundaryOperation.Construct));
        Assert.AreEqual(12, Candidates.Count(static candidate => candidate.Operation == BoundaryOperation.Describe));
        Assert.AreEqual(12, Candidates.Count(static candidate => candidate.Operation == BoundaryOperation.Plan));
        Assert.AreEqual(10, Candidates.Count(static candidate => candidate.Operation == BoundaryOperation.Open));
        Assert.AreEqual(2, Candidates.Count(static candidate => candidate.Operation == BoundaryOperation.Guard));
    }

    [TestMethod]
    public void ValidSeedMatrix_ShouldCompileAndExecuteWithoutProviderFault()
    {
        foreach (var shape in QueryShapes)
        {
            var query = $"{shape} /* REC127-valid-seed */";
            var provider = new BoundaryProvider(BoundaryOperation.None);
            using var compiled = Compile(query, provider);

            var table = TableMaterializationTestHelper.Materialize(compiled.Run());

            Assert.IsGreaterThan(0, table.Count, query);
            Assert.AreEqual(
                query.Split("#boundary.items(", StringSplitOptions.None).Length - 1,
                provider.OpenCount,
                query);
        }
    }

    [TestMethod]
    [DynamicData(nameof(ProviderBoundaryCases))]
    public void ProviderBoundaryMatrix_ShouldExposeSafeOperationFacts(BoundaryCandidate candidate)
    {
        var provider = new BoundaryProvider(candidate.Operation);

        if (candidate.Operation is BoundaryOperation.Open or BoundaryOperation.Guard)
        {
            using var compiled = Compile(candidate.Query, provider);

            Assert.IsTrue(provider.DescribeCount > 0, candidate.Id);
            Assert.IsTrue(provider.PlanCount > 0, candidate.Id);

            if (candidate.Id == "E02")
            {
                var exception = Assert.Throws<QueryExecutionException>(() =>
                    _ = compiled.Run().Count);

                AssertParameterEnvelope(exception, candidate.Id);
                Assert.AreEqual(0, provider.OpenCount, candidate.Id);
                return;
            }

            provider.Operation = BoundaryOperation.Open;
            var openException = Assert.Throws<QueryExecutionException>(() =>
                _ = TableMaterializationTestHelper.Materialize(compiled.Run()).Count);

            AssertBoundaryEnvelope(openException.Envelope, candidate, candidate.ExpectedOperation);
            Assert.AreEqual(1, provider.OpenCount, candidate.Id);
            return;
        }

        var result = CompileWithDiagnostics(candidate.Query, provider);
        Assert.IsFalse(result.Succeeded, candidate.Id);
        Assert.AreEqual(1, result.Errors.Count, candidate.Id);

        var envelope = result.ToEnvelopes().Single();
        AssertBoundaryEnvelope(envelope, candidate, candidate.ExpectedOperation);
        Assert.IsFalse((int)envelope.Code >= 1000 && (int)envelope.Code < 3000, candidate.Id);
    }

    private static IReadOnlyList<BoundaryCandidate> CreateCandidates()
    {
        var candidates = new List<BoundaryCandidate>(48);

        AddFamily(candidates, "A", BoundaryOperation.Construct, "construct", QueryShapes.Length);
        AddFamily(candidates, "B", BoundaryOperation.Describe, "describe", QueryShapes.Length);
        AddFamily(candidates, "C", BoundaryOperation.Plan, "plan", QueryShapes.Length);
        AddFamily(candidates, "D", BoundaryOperation.Open, "open", QueryShapes.Length - 2);

        candidates.Add(new BoundaryCandidate(
            "E01",
            BoundaryOperation.Guard,
            WithFaultMarker(QueryShapes[0], "E01"),
            DiagnosticCode.MQ7010_DataSourceOpenFailed,
            "open"));
        candidates.Add(new BoundaryCandidate(
            "E02",
            BoundaryOperation.Guard,
            "param(key: string) select e.Name from #boundary.items($key) e /* E02-fault */",
            DiagnosticCode.MQ7003_RequiredScriptParameterMissing,
            "parameter-binding"));

        return candidates;
    }

    private static void AddFamily(
        ICollection<BoundaryCandidate> candidates,
        string prefix,
        BoundaryOperation operation,
        string expectedOperation,
        int count)
    {
        for (var index = 0; index < count; index++)
        {
            var id = $"{prefix}{index + 1:00}";
            candidates.Add(new BoundaryCandidate(
                id,
                operation,
                WithFaultMarker(QueryShapes[index], id),
                DiagnosticCode.MQ7010_DataSourceOpenFailed,
                expectedOperation));
        }
    }

    private static string WithFaultMarker(string query, string caseId)
    {
        return $"{query} /* {caseId}-fault */";
    }

    private static CompiledQuery Compile(
        string query,
        BoundaryProvider provider,
        BoundaryOperation operation)
    {
        provider.Operation = operation;
        var result = CompileWithDiagnostics(query, provider);
        Assert.IsTrue(result.Succeeded, string.Join(" | ", result.Errors.Select(static error => error.ToDetailedString())));
        Assert.IsNotNull(result.CompiledQuery);
        return result.CompiledQuery;
    }

    private static CompiledQuery Compile(string query, BoundaryProvider provider)
    {
        return Compile(query, provider, BoundaryOperation.None);
    }

    private static BuildResult CompileWithDiagnostics(string query, BoundaryProvider provider)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString("N"),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
    }

    private static void AssertBoundaryEnvelope(
        MusoqErrorEnvelope? envelope,
        BoundaryCandidate candidate,
        string expectedOperation)
    {
        Assert.IsNotNull(envelope, candidate.Id);
        Assert.AreEqual(candidate.ExpectedCode, envelope.Code, candidate.Id);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity, candidate.Id);
        Assert.AreEqual(DiagnosticPhase.DataSource, envelope.Phase, candidate.Id);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, envelope.SourceKind, candidate.Id);
        Assert.AreEqual("#boundary", envelope.Arguments["schema"], candidate.Id);
        Assert.AreEqual("items", envelope.Arguments["source"], candidate.Id);
        Assert.AreEqual("e", envelope.Arguments["alias"], candidate.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Arguments["sourceContextId"]), candidate.Id);
        Assert.AreEqual(expectedOperation, envelope.Arguments["operation"], candidate.Id);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, envelope.Arguments["causeType"], candidate.Id);
        Assert.DoesNotContain("secret-provider-argument", envelope.Message, candidate.Id);
        Assert.DoesNotContain("secret-provider-argument", envelope.ToString() ?? string.Empty, candidate.Id);
    }

    private static void AssertParameterEnvelope(QueryExecutionException exception, string caseId)
    {
        var envelope = exception.Envelope;
        Assert.IsNotNull(envelope, caseId);
        Assert.AreEqual(DiagnosticCode.MQ7003_RequiredScriptParameterMissing, envelope.Code, caseId);
        Assert.AreEqual(DiagnosticPhase.Runtime, envelope.Phase, caseId);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, envelope.SourceKind, caseId);
        Assert.Contains("Required script parameter 'key' was not provided", envelope.Message, caseId);
        Assert.DoesNotContain("secret-provider-argument", exception.Message, caseId);
    }

    public enum BoundaryOperation
    {
        None,
        Construct,
        Describe,
        Plan,
        Open,
        Guard
    }

    public sealed record BoundaryCandidate(
        string Id,
        BoundaryOperation Operation,
        string Query,
        DiagnosticCode ExpectedCode,
        string ExpectedOperation);

    private sealed class BoundaryProvider(BoundaryOperation operation) : ISchemaProvider
    {
        private readonly BoundarySchema _schema = new();

        public BoundaryOperation Operation { get; set; } = operation;

        public int GetSchemaCount { get; private set; }

        public int OpenCount => _schema.OpenCount;

        public int DescribeCount => _schema.DescribeCount;

        public int PlanCount => _schema.PlanCount;

        public ISchema GetSchema(string schema)
        {
            Assert.AreEqual("#boundary", schema);
            GetSchemaCount++;
            if (Operation == BoundaryOperation.Construct)
                throw CreateProviderException();

            _schema.Operation = Operation;
            return _schema;
        }

        private static InvalidOperationException CreateProviderException()
        {
            return new InvalidOperationException("secret-provider-argument");
        }
    }

    private sealed class BoundarySchema : GenericSchema<BasicEntity, BasicEntityTable>
    {
        private static readonly IReadOnlyList<BasicEntity> Rows =
        [
            new BasicEntity("alpha") { Population = 1 },
            new BasicEntity("beta") { Population = 2 }
        ];

        public BoundarySchema()
            : base([], BasicEntity.TestNameToIndexMap, BasicEntity.TestIndexToObjectAccessMap)
        {
            AddSource<EntitySource<BasicEntity>>(
                "items",
                new[] { Rows },
                new Dictionary<string, int>(BasicEntity.TestNameToIndexMap),
                new Dictionary<int, Func<BasicEntity, object?>>(BasicEntity.TestIndexToObjectAccessMap));
            AddTable<BasicEntityTable>("items");
        }

        public BoundaryOperation Operation { get; set; }

        public int OpenCount { get; private set; }

        public int DescribeCount { get; private set; }

        public int PlanCount { get; private set; }

        public override SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            DescribeCount++;
            if (Operation == BoundaryOperation.Describe)
                throw CreateProviderException();

            return base.DescribeSource(name, context, parameters);
        }

        public override SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            PlanCount++;
            if (Operation == BoundaryOperation.Plan)
                throw CreateProviderException();

            return base.TryPlanSource(name, request, parameters);
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            OpenCount++;
            if (Operation == BoundaryOperation.Open)
                throw CreateProviderException();

            return EnsureSourceType<T, BasicEntity>(
                name,
                new EntitySource<BasicEntity>(
                    [Rows],
                    new Dictionary<string, int>(BasicEntity.TestNameToIndexMap),
                    new Dictionary<int, Func<BasicEntity, object?>>(BasicEntity.TestIndexToObjectAccessMap)));
        }

        private static InvalidOperationException CreateProviderException()
        {
            return new InvalidOperationException("secret-provider-argument");
        }
    }
}
