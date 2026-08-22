using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Wildcard;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    private const string PipelineQuery =
        "select * exclude (Other) replace (Score + 1 as Score) from #wildcard.rows() a " +
        "where a.Other = 'source-column' order by a.Score desc";

    [TestMethod]
    public void LegacyExecution_ShouldRetainPredicateAndReplacementInputs()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        using var table = Compile(PipelineQuery, recorder).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 11]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsNotEmpty(recorder.ExecutionContexts);
        Assert.IsEmpty(recorder.QueryScopedExecutionContexts);
        Assert.IsTrue(
            recorder.ExecutionContexts.Any(static context =>
                context.Columns.Contains("Id", StringComparer.OrdinalIgnoreCase) &&
                context.Columns.Contains("Other", StringComparer.OrdinalIgnoreCase) &&
                context.Columns.Contains("Score", StringComparer.OrdinalIgnoreCase)),
            "Legacy execution must retain predicate and replacement source columns.");
    }

    [TestMethod]
    public void QueryScopedExecution_ShouldRetainPredicateAndReplacementInputs()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        using var query = InstanceCreator.CompileForExecution(
            PipelineQuery,
            Guid.NewGuid().ToString(),
            new ProjectionSensitiveWildcardSchemaProvider(recorder, queryScopedRowsEnabled: true),
            new TestsLoggerResolver());

        using var table = query.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 11]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsEmpty(recorder.ExecutionContexts);
        Assert.IsNotEmpty(recorder.QueryScopedExecutionContexts);
        Assert.IsNotEmpty(recorder.QueryScopedShapes);
        Assert.IsTrue(
            recorder.QueryScopedShapes.SelectMany(static shape => shape).Any(static field =>
                string.Equals(field.Name, "Other", StringComparison.OrdinalIgnoreCase)),
            "Query-scoped transfer must carry the excluded predicate column.");
    }

    [TestMethod]
    public void Inspection_ShouldDiscoverCompleteSchemaAndRenderFinalProjection()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var inspection = InstanceCreator.CompileForInspection(
            PipelineQuery,
            Guid.NewGuid().ToString(),
            new ProjectionSensitiveWildcardSchemaProvider(recorder),
            new TestsLoggerResolver());

        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));
        Assert.IsTrue(inspection.Diagnostics.All(static diagnostic => !diagnostic.IsError));
        Assert.Contains("Score", inspection.GeneratedCSharpCode);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void DiagnosticCompilation_ShouldCollectValidAndInvalidWildcardResults()
    {
        var validRecorder = new ProjectionSensitiveWildcardRecorder();
        var valid = InstanceCreator.CompileWithDiagnostics(
            PipelineQuery,
            Guid.NewGuid().ToString(),
            new ProjectionSensitiveWildcardSchemaProvider(validRecorder),
            new TestsLoggerResolver());

        Assert.IsTrue(valid.Succeeded, string.Join(Environment.NewLine, valid.Errors));
        Assert.IsTrue(valid.Diagnostics.All(static diagnostic => !diagnostic.IsError));
        Assert.IsTrue(validRecorder.MetadataRequests.Any(static request => request.Length == 0));

        var invalid = InstanceCreator.CompileWithDiagnostics(
            "select * exclude (Missing) from #wildcard.rows() a",
            Guid.NewGuid().ToString(),
            new ProjectionSensitiveWildcardSchemaProvider(new ProjectionSensitiveWildcardRecorder()),
            new TestsLoggerResolver());

        Assert.IsFalse(invalid.Succeeded);
        Assert.IsTrue(invalid.Errors.Any(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3041_StarExcludeColumnNotFound));
    }

    [TestMethod]
    public void RepeatedCompilation_ShouldReuseSemanticExecutionArtifactWithoutChangingResults()
    {
        var threshold = -Random.Shared.Next(1, int.MaxValue);
        var queryText =
            $"select * exclude (Other) from #wildcard.rows() a where a.Id > {threshold}";
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var provider = new ProjectionSensitiveWildcardSchemaProvider(recorder);

        var first = InstanceCreator.CompileWithDiagnostics(
            queryText,
            $"ProjectionSensitiveWildcardCacheFirst_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver());
        Assert.IsTrue(first.Succeeded, string.Join(Environment.NewLine, first.Errors));
        Assert.IsNotNull(first.BuildItems);
        Assert.IsFalse(first.BuildItems.StopAfterPlanning);
        using (var firstTable = first.CompiledQuery!.Run())
            TableMaterializationTestHelper.AssertRowsInOrder(firstTable, [1, "Ada", 10]);
        first.CompiledQuery.Dispose();

        var second = InstanceCreator.CompileWithDiagnostics(
            queryText,
            $"ProjectionSensitiveWildcardCacheSecond_{Guid.NewGuid():N}",
            provider,
            new TestsLoggerResolver());
        Assert.IsTrue(second.Succeeded, string.Join(Environment.NewLine, second.Errors));
        Assert.IsNotNull(second.BuildItems);
        Assert.IsTrue(second.BuildItems.StopAfterPlanning);
        using (var secondTable = second.CompiledQuery!.Run())
            TableMaterializationTestHelper.AssertRowsInOrder(secondTable, [1, "Ada", 10]);
        second.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void StaticSchemaStar_ShouldRetainExistingExpansionBehavior()
    {
        using var table = InstanceCreator.CompileForExecution(
                "select * from #A.entities() a where a.Population > 0",
                Guid.NewGuid().ToString(),
                new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] = [new BasicEntity("Ada", 50m) { Population = 10m }]
                }),
                new TestsLoggerResolver())
            .Run();

        Assert.AreEqual(9, table.Columns.Count());
        Assert.AreEqual(1, table.Count);
    }
}
