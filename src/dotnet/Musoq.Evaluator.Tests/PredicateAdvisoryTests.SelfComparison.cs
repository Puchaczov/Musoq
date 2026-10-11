using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using GenericProofSchema = Musoq.Evaluator.Tests.Schema.Generic.GenericSchema<Musoq.Plugins.LibraryBase>;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public sealed partial class PredicateAdvisoryTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0029")]
    public void DeterministicSelfComparison_ShouldWarnAndPreserveRows()
    {
        const string query = "select Id from A.entities() where Id = Id";
        var entities = new[] { new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 } };
        var provider = new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = entities,
            ["#A"] = entities
        });
        var build = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var compiled = build.CompiledQuery;
        Assert.IsTrue(build.Succeeded);
        Assert.AreEqual(1, build.Warnings.Count(warning => warning.CodeString == "MQ5010"));
        Assert.IsTrue(build.Warnings.Single(warning => warning.CodeString == "MQ5010").IsWarning);
        using var result = compiled!.Run();
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 1 }, new object?[] { 2 });
    }

    [TestMethod]
    [DataRow("=", DiagnosticCode.MQ5010_TautologicalCondition)]
    [DataRow("<=", DiagnosticCode.MQ5010_TautologicalCondition)]
    [DataRow(">=", DiagnosticCode.MQ5010_TautologicalCondition)]
    [DataRow("<>", DiagnosticCode.MQ5011_ContradictoryCondition)]
    [DataRow("<", DiagnosticCode.MQ5011_ContradictoryCondition)]
    [DataRow(">", DiagnosticCode.MQ5011_ContradictoryCondition)]
    public void StableNonNullableSelfComparison_ShouldReportProof(string comparison, DiagnosticCode code)
    {
        AssertCode(Analyze($"select Id from #A.Entities() where Id {comparison} Id"), code);
    }

    [TestMethod]
    [DataRow("select NullableValue from #A.Entities() where NullableValue = NullableValue")]
    [DataRow("select Name from #A.Entities() where Name = Name")]
    [DataRow("select Id from #A.Entities() where Abs(Id) = Abs(Id)")]
    [DataRow("select a.Id from #A.Entities() a join #A.Entities() b on a.Id = b.Id")]
    public void UnsafeOrDifferentColumnIdentity_ShouldRemainQuiet(string query)
    {
        var result = Analyze(query);
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(warning => warning.Code is DiagnosticCode.MQ5010_TautologicalCondition or DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    [DataRow("FloatingValue")]
    [DataRow("VolatileValue")]
    public void FloatingOrVolatileSelfComparison_ShouldRemainQuiet(string column)
    {
        var result = new QueryAnalyzer(CreateSelfComparisonProvider()).Analyze(
            $"select {column} from #probe.Entities() where {column} = {column}");
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(warning => warning.Code is DiagnosticCode.MQ5010_TautologicalCondition or DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    [TestProperty("CandidateId", "PROMOTED-DeterministicSelfComparisonEmitsMq5010ForEmptySource")]
    public void DeterministicSelfComparisonEmitsMq5010ForEmptySource()
    {
        const string query = "select 1 from A.entities() where Id = Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>> { ["#A"] = Array.Empty<BasicEntity>() };
        var build = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources), new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false));
        using var compiled = build.CompiledQuery;
        Assert.IsTrue(build.Succeeded);
        Assert.AreEqual(1, build.Warnings.Count);
        Assert.AreEqual("MQ5010", build.Warnings[0].CodeString);
        Assert.IsTrue(build.Warnings[0].IsWarning);
    }

    private static GenericSchemaProvider CreateSelfComparisonProvider()
    {
        var table = new GenericEntityTable<SelfComparisonEntity>();
        var index = GenericEntityTable<SelfComparisonEntity>.NameToIndexMap[nameof(SelfComparisonEntity.VolatileValue)];
        table.Columns[index] = new Musoq.Schema.DataSources.SchemaColumn(nameof(SelfComparisonEntity.VolatileValue), index, typeof(int), (string?)null, ColumnStability.Volatile);
        var schema = new GenericProofSchema(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["Entities"] = (table, new GenericChunkSource<SelfComparisonEntity>([],
                GenericEntityTable<SelfComparisonEntity>.NameToIndexMap,
                GenericEntityTable<SelfComparisonEntity>.IndexToObjectAccessMap))
        });
        return new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#probe"] = schema });
    }

    public sealed class SelfComparisonEntity
    {
        public double FloatingValue { get; init; }
        public int VolatileValue { get; init; }
    }
}
