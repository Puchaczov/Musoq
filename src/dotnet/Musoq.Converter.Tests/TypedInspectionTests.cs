using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Tests.Common;
using MusoqApi = Musoq.Converter.Musoq;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;
using Person = Musoq.Converter.Tests.TwoModeTestFixtures.Person;
using static Musoq.Converter.Tests.TwoModeTestFixtures;

namespace Musoq.Converter.Tests;

[TestClass]
public class TypedInspectionTests
{
    static TypedInspectionTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void InspectTyped_WhenProjectionIsSimple_ShouldExposeGeneratedCodeAndMetadata()
    {
        var result = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .InspectTyped<NameDto>();

        Assert.AreEqual(QueryResultMode.TypedEnumerable, result.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, result.SelectedResultSinkKind);
        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, result.RowsKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, result.RowPathKind);
        Assert.IsFalse(result.RequiresComputeTableMethod);
        Assert.AreEqual(typeof(NameDto), result.OutputType);
        Assert.IsFalse(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, result.FinalSinkRejectionKind);
        Assert.IsNull(result.FinalSinkRejectionReason);
        Assert.IsNotNull(result.Query);
        StringAssert.Contains(result.GeneratedCSharpCode, "public IEnumerable<");
        StringAssert.Contains(result.GeneratedCSharpCode, " Run(CancellationToken token)");
    }

    [TestMethod]
    public void CompileForTypedInspection_WhenCalledDirectly_ShouldExposeTypedGeneratedCode()
    {
        var result = InstanceCreator.CompileForTypedInspection<DualDto>(
            "select d.Dummy as Dummy from #system.dual() d",
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver(),
            new CompilationOptions(ParallelizationMode.None));

        Assert.AreEqual(QueryResultMode.TypedEnumerable, result.ResultMode);
        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, result.RowsKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, result.RowPathKind);
        Assert.IsFalse(result.RequiresComputeTableMethod);
        Assert.IsFalse(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
        StringAssert.Contains(result.GeneratedCSharpCode, "ITypedRunnable<");
    }

    [TestMethod]
    public void InspectTyped_WhenQueryNeedsHiddenSortColumn_ShouldReportDirectRows()
    {
        var result = MusoqApi
            .Query("select p.Name as Name from #A.entities() p order by p.Age")
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .InspectTyped<NameDto>();

        Assert.AreEqual(QueryResultMode.TypedEnumerable, result.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, result.SelectedResultSinkKind);
        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, result.RowsKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, result.RowPathKind);
        Assert.IsFalse(result.RequiresComputeTableMethod);
        Assert.IsFalse(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, result.FinalSinkRejectionKind);
        Assert.IsNull(result.FinalSinkRejectionReason);
        StringAssert.Contains(result.GeneratedCSharpCode, "private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(");
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("QueryRows.FromTable<ResultRow0>", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InspectTyped_WhenOutputBindingIsInvalid_ShouldReturnBindingDiagnostics()
    {
        var result = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .InspectTyped<MissingNameDto>();

        Assert.AreEqual(QueryResultMode.TypedEnumerable, result.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, result.SelectedResultSinkKind);
        Assert.AreEqual(TypedGeneratedRowsKind.Unknown, result.RowsKind);
        Assert.AreEqual(QueryResultRowPathKind.Unknown, result.RowPathKind);
        Assert.IsFalse(result.RequiresComputeTableMethod);
        Assert.IsTrue(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
        Assert.AreEqual(string.Empty, result.GeneratedCSharpCode);
        StringAssert.Contains(result.OutputBindingDiagnostics[0], "does not expose writable member 'Name'");
    }

    [TestMethod]
    public void TypedBuildSetup_WhenUsingInMemoryTypes_ShouldResolveReferencesForCompileArtifactAndInspection()
    {
        const string query = "select p.Name as Name from #A.entities() p where p.Age > 30";
        var rows = new[] { new Person("Alice", 35, "NY"), new Person("Bob", 20, "LA") };

        var compiled = MusoqApi
            .Query(query)
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .Compile<NameDto>();
        var compiledRows = compiled
            .Run(CancellationToken.None, MusoqApi.Source("#A", "entities", Chunks(rows)))
            .ToArray();

        var artifact = MusoqApi
            .Query(query)
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileArtifact<NameDto>();
        var loadedRows = MusoqApi
            .Load<NameDto>(artifact)
            .Run(CancellationToken.None, MusoqApi.Source("#A", "entities", Chunks(rows)))
            .ToArray();

        var inspection = MusoqApi
            .Query(query)
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .InspectTyped<NameDto>();
        var profiledRows = MusoqApi
            .Query(query)
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .CompileForProfile<NameDto>()
            .RunWithProfile(CancellationToken.None, MusoqApi.Source("#A", "entities", Chunks(rows)))
            .Rows
            .ToArray();

        Assert.AreEqual("Alice", compiledRows.Single().Name);
        Assert.AreEqual("Alice", loadedRows.Single().Name);
        Assert.AreEqual("Alice", profiledRows.Single().Name);
        Assert.IsFalse(inspection.HasOutputBindingDiagnostics);
        StringAssert.Contains(inspection.GeneratedCSharpCode, nameof(NameDto));
    }

    public sealed record DualDto(string Dummy);

    public sealed class MissingNameDto
    {
        public string Other { get; set; } = string.Empty;
    }
}
