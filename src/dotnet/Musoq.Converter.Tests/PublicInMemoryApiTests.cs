using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Schema;
using Musoq.Tests.Common;
using MusoqApi = Musoq.Converter.Musoq;
using AmbiguousPerson = Musoq.Converter.Tests.TwoModeTestFixtures.AmbiguousPerson;
using FieldPerson = Musoq.Converter.Tests.TwoModeTestFixtures.FieldPerson;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;
using NumberDto = Musoq.Converter.Tests.TwoModeTestFixtures.NumberDto;
using OtherPerson = Musoq.Converter.Tests.TwoModeTestFixtures.NameRow;
using Person = Musoq.Converter.Tests.TwoModeTestFixtures.Person;
using static Musoq.Converter.Tests.TwoModeTestFixtures;

namespace Musoq.Converter.Tests;

[TestClass]
public class PublicInMemoryApiTests
{
    static PublicInMemoryApiTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void CompileAndRun_WhenRowsAreSuppliedOnBuilder_ShouldReturnTypedRows()
    {
        var people = new[]
        {
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA")
        };

        var rows = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source("#A", "entities", Chunks(people))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void CompileAndRun_WhenSourceRowsAreScalars_ShouldExposeValueColumn()
    {
        var rows = MusoqApi
            .Query("select n.Value as Number from #A.entities() n where n.Value > 1 order by n.Value")
            .Source("#A", "entities", Chunks(new[] { 3, 1, 2 }))
            .CompileAndRun<NumberDto>(CancellationToken.None)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 2, 3 }, rows.Select(static row => row.Number).ToArray());
    }

    [TestMethod]
    public void CompileAndRun_WhenSourceRowsUseFields_ShouldExposeFieldColumns()
    {
        var rows = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source("#A", "entities", Chunks(new[]
            {
                new FieldPerson("Alice", 35),
                new FieldPerson("Bob", 20)
            }))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void Compile_WhenInMemoryRowMembersAreAmbiguous_ShouldKeepSameDiagnostic()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<AmbiguousPerson>("#A", "entities")
            .Compile<NameDto>());

        StringAssert.Contains(exception.Message, "In-memory source row member 'name' is ambiguous.");
    }

    [TestMethod]
    public void Compile_WhenTypedQueryHasSemanticError_ShouldExposeBuildDiagnostics()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => MusoqApi
            .Query("select p.Missing as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>());

        StringAssert.Contains(exception.Message, "Build diagnostics:");
        StringAssert.Contains(exception.Message, "Unknown column 'Missing'");
    }

    [TestMethod]
    public void Compile_WhenSameInMemoryShapeIsUsedRepeatedly_ShouldKeepBehavior()
    {
        var query = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        for (var i = 0; i < 3; i++)
        {
            var rows = query.Run(
                    CancellationToken.None,
                    MusoqApi.Source("#A", "entities", Chunks(new[] { new Person($"Alice{i}", 35, "NY") })))
                .ToArray();

            Assert.AreEqual($"Alice{i}", rows.Single().Name);
        }
    }

    [TestMethod]
    public void Compile_WhenTypedQueryIsBuilt_ShouldExposeStructuredDiagnostics()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .Compile<NameDto>();

        var diagnostics = compiled.Diagnostics;

        Assert.IsNotNull(diagnostics.RunnableType);
        Assert.AreEqual(QueryResultMode.TypedEnumerable, diagnostics.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, diagnostics.SelectedResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, diagnostics.RowPathKind);
        Assert.IsFalse(diagnostics.RequiresComputeTableMethod);
        Assert.IsFalse(diagnostics.HasFinalSinkRejectionDiagnostics);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, diagnostics.FinalSinkRejectionKind);
        Assert.IsNull(diagnostics.FinalSinkRejectionReason);
        Assert.IsFalse(diagnostics.IsProfiled);
        Assert.AreEqual(TypedQueryProfileMode.None, diagnostics.ProfileMode);
    }

    [TestMethod]
    public void Compile_WhenTypedQueryUsesHiddenSortColumn_ShouldExposeDirectRowsDiagnostics()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p order by p.Age")
            .Source<Person>("#A", "entities")
            .WithCompilationOptions(new CompilationOptions(ParallelizationMode.None))
            .Compile<NameDto>();

        var diagnostics = compiled.Diagnostics;

        Assert.AreEqual(FinalResultSinkKind.TypedSerialEnumerable, diagnostics.SelectedResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, diagnostics.RowPathKind);
        Assert.IsFalse(diagnostics.RequiresComputeTableMethod);
        Assert.IsFalse(diagnostics.HasFinalSinkRejectionDiagnostics);
        Assert.AreEqual(FinalProjectionSinkRejectionKind.None, diagnostics.FinalSinkRejectionKind);
        Assert.IsNull(diagnostics.FinalSinkRejectionReason);
    }

    [TestMethod]
    public void Compile_WhenRowsAreSuppliedPerRun_ShouldReuseCompiledQueryWithIndependentRows()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        var first = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .ToArray();
        var second = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Bob", 41, "LA") })))
            .ToArray();

        Assert.AreEqual("Alice", first.Single().Name);
        Assert.AreEqual("Bob", second.Single().Name);
    }

    [TestMethod]
    public void Compile_GenericShorthand_WhenRowsAreSuppliedPerRun_ShouldUseAEntitiesConvention()
    {
        var compiled = MusoqApi.Compile<Person, NameDto>(
            "select p.Name as Name from #A.entities() p where p.Age > 30");

        var rows = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void TypedArtifactApi_ShouldSeparateInMemoryAndPortableLoadingContracts()
    {
        var compileArtifact = typeof(MusoqQueryBuilder)
            .GetMethods()
            .Single(static method => method.Name == nameof(MusoqQueryBuilder.CompileArtifact) &&
                                     method.IsGenericMethodDefinition);
        Assert.AreEqual(typeof(CompiledTypedQueryArtifact), compileArtifact.ReturnType);

        var publicLoad = typeof(global::Musoq.Converter.Musoq)
            .GetMethods()
            .Single(static method => method.Name == nameof(global::Musoq.Converter.Musoq.Load) &&
                                     method.IsGenericMethodDefinition);
        Assert.AreEqual(typeof(CompiledTypedQueryArtifact), publicLoad.GetParameters()[0].ParameterType);

        var portableLoad = typeof(InstanceCreator)
            .GetMethods()
            .Single(static method => method.Name == nameof(InstanceCreator.LoadTypedArtifact) &&
                                     method.IsGenericMethodDefinition &&
                                     method.GetParameters().Length == 3);
        Assert.AreEqual(typeof(ICompiledTypedQueryArtifact), portableLoad.GetParameters()[0].ParameterType);
    }
    [TestMethod]
    public void CompileArtifact_WhenLoaded_ShouldReuseArtifactWithDifferentRows()
    {
        var artifact = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .CompileArtifact<NameDto>();
        var compiled = MusoqApi.Load<NameDto>(artifact);

        var first = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .Single();
        var second = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Bob", 41, "LA") })))
            .Single();

        Assert.AreEqual("Alice", first.Name);
        Assert.AreEqual("Bob", second.Name);
    }

    [TestMethod]
    public void Load_WhenArtifactOutputTypeDoesNotMatch_ShouldReject()
    {
        var artifact = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .CompileArtifact<NameDto>();

        Assert.Throws<InvalidOperationException>(() => MusoqApi.Load<OtherPerson>(artifact));
    }

    [TestMethod]
    public void Load_WhenArtifactDllIsEmpty_ShouldReject()
    {
        var artifact = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .CompileArtifact<NameDto>();
        var broken = new CompiledTypedQueryArtifact(
            [],
            artifact.PdbFile,
            artifact.RunnableTypeName,
            artifact.ResultMode,
            artifact.OutputType,
            artifact.SourceRuntimeSettingsBySourceContextId,
            artifact.SourceRuntimeSettingDescriptionsBySourceContextId,
            artifact.SourceExecutionPlans,
            artifact.ParameterDefinitions);

        Assert.Throws<InvalidOperationException>(() => InstanceCreator.LoadTypedArtifact<NameDto>(
            broken,
            new EmptyInMemorySchemaProvider(),
            new TestsLoggerResolver()));
    }

    [TestMethod]
    public void Load_WhenArtifactIsLoadedTwice_ShouldReturnIndependentCompiledQueries()
    {
        var artifact = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .CompileArtifact<NameDto>();

        var first = MusoqApi.Load<NameDto>(artifact);
        var second = MusoqApi.Load<NameDto>(artifact);

        Assert.AreNotSame(first, second);
        Assert.AreEqual("Alice", first.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .Single().Name);
        Assert.AreEqual("Bob", second.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Bob", 41, "LA") })))
            .Single().Name);
    }

    [TestMethod]
    public void CompileArtifact_WhenUsingPublicInMemorySources_ShouldExposeCompatibilityMetadata()
    {
        var artifact = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .CompileArtifact<NameDto>();

        Assert.AreEqual(CompiledTypedQueryArtifact.CurrentArtifactVersion, artifact.ArtifactVersion);
        Assert.IsFalse(string.IsNullOrWhiteSpace(artifact.EngineVersion));
        Assert.IsFalse(string.IsNullOrWhiteSpace(artifact.RuntimeVersion));
        Assert.AreEqual(RuntimeV2Contract.ContractSignature, artifact.RuntimeContractSignature);
        Assert.IsEmpty(artifact.ParameterContracts);
        Assert.HasCount(1, artifact.SourceSlotIdentities);
        Assert.AreEqual("A", artifact.SourceSlotIdentities[0].SchemaName);
        Assert.AreEqual("entities", artifact.SourceSlotIdentities[0].SourceName);
        StringAssert.Contains(artifact.SourceSlotIdentities[0].RowTypeName, typeof(Person).FullName!);

        var row = MusoqApi
            .Load<NameDto>(artifact)
            .Run(CancellationToken.None, MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .Single();

        Assert.AreEqual("Alice", row.Name);
    }

    [TestMethod]
    public void CompileAndRun_GenericTwoSourceShorthand_ShouldMapSourcesToAAndB()
    {
        var people = new[]
        {
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA")
        };
        var cities = new[] { new OtherPerson("NY") };

        var rows = MusoqApi
            .CompileAndRun<Person, OtherPerson, NameDto>(
                "select p.Name as Name from #A.entities() p inner join #B.entities() c on p.City = c.Name",
                Chunks(people),
                Chunks(cities),
                CancellationToken.None)
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void CompileAndRun_GenericThreeSourceShorthand_ShouldMapSourcesToAThroughC()
    {
        var people = new[]
        {
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA")
        };
        var cities = new[] { new OtherPerson("NY") };
        var allowedNames = new[] { new OtherPerson("Alice") };

        var rows = MusoqApi
            .CompileAndRun<Person, OtherPerson, OtherPerson, NameDto>(
                "select p.Name as Name from #A.entities() p inner join #B.entities() c on p.City = c.Name inner join #C.entities() n on p.Name = n.Name",
                Chunks(people),
                Chunks(cities),
                Chunks(allowedNames),
                CancellationToken.None)
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void CompileAndRun_GenericFourSourceShorthand_ShouldMapSourcesToAThroughD()
    {
        var people = new[]
        {
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA")
        };
        var cities = new[] { new OtherPerson("NY") };
        var allowedNames = new[] { new OtherPerson("Alice") };
        var allowedCities = new[] { new OtherPerson("NY") };

        var rows = MusoqApi
            .CompileAndRun<Person, OtherPerson, OtherPerson, OtherPerson, NameDto>(
                "select p.Name as Name from #A.entities() p inner join #B.entities() c on p.City = c.Name inner join #C.entities() n on p.Name = n.Name inner join #D.entities() d on p.City = d.Name",
                Chunks(people),
                Chunks(cities),
                Chunks(allowedNames),
                Chunks(allowedCities),
                CancellationToken.None)
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public void Compile_WhenQueryHasParameter_ShouldUseParameterValuesForEachRun()
    {
        var compiled = MusoqApi
            .Query("param(minAge: int) select p.Name as Name from #A.entities() p where p.Age >= $minAge order by p.Name")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();
        Assert.HasCount(1, compiled.ParameterContracts);
        Assert.AreEqual("minAge", compiled.ParameterContracts[0].Name);
        Assert.AreEqual("int", compiled.ParameterContracts[0].DeclaredTypeName);
        Assert.AreEqual("int", compiled.ParameterContracts[0].CanonicalTypeName);
        Assert.AreEqual(typeof(int), compiled.ParameterContracts[0].ClrType);
        compiled.Parameters["minAge"] = 30;

        var rows = compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities",
                Chunks(new[]
                {
                    new Person("Alice", 35, "NY"),
                    new Person("Bob", 20, "LA")
                })))
            .ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("Alice", rows[0].Name);
    }

    [TestMethod]
    public async Task Compile_WhenRunsAreConcurrent_ShouldUseIndependentSourceBindings()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p where p.Age > 30")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        var firstTask = Task.Run(() => compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") })))
            .ToArray());
        var secondTask = Task.Run(() => compiled.Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Bob", 41, "LA") })))
            .ToArray());

        var results = await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);

        CollectionAssert.AreEquivalent(new[] { "Alice", "Bob" }, results.SelectMany(static rows => rows).Select(static row => row.Name).ToArray());
    }

    [TestMethod]
    public async Task Run_WithPerRunOptions_WhenRunsAreConcurrent_ShouldUseIndependentParametersAndCallbacks()
    {
        var compiled = MusoqApi
            .Query("param(minAge: int) select p.Name as Name from #A.entities() p where p.Age >= $minAge order by p.Name")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        var firstOptions = new TypedQueryRunOptions(
            CancellationToken.None,
            new Dictionary<string, object?> { ["minAge"] = 30 },
            (_, _) => Interlocked.Increment(ref firstPhaseCount));
        var secondOptions = new TypedQueryRunOptions(
            CancellationToken.None,
            new Dictionary<string, object?> { ["minAge"] = 40 },
            (_, _) => Interlocked.Increment(ref secondPhaseCount));

        var firstTask = Task.Run(() => compiled.Run(
                firstOptions,
                MusoqApi.Source("#A", "entities", Chunks(new[]
                {
                    new Person("Alice", 35, "NY"),
                    new Person("Bob", 20, "LA")
                })))
            .ToArray());
        var secondTask = Task.Run(() => compiled.Run(
                secondOptions,
                MusoqApi.Source("#A", "entities", Chunks(new[]
                {
                    new Person("Charlie", 45, "NY"),
                    new Person("Dana", 32, "LA")
                })))
            .ToArray());

        var results = await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "Alice" }, results[0].Select(static row => row.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "Charlie" }, results[1].Select(static row => row.Name).ToArray());
        Assert.IsGreaterThan(0, firstPhaseCount);
        Assert.IsGreaterThan(0, secondPhaseCount);
    }

    [TestMethod]
    public void Run_WhenCompatibilityStateChangesAfterRun_ShouldUseSnapshot()
    {
        var compiled = MusoqApi
            .Query("param(minAge: int) select p.Name as Name from #A.entities() p where p.Age >= $minAge order by p.Name")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        compiled.Parameters["minAge"] = 30;
        compiled.PhaseChanged += (_, _) => firstPhaseCount++;

        var rows = compiled.Run(
            CancellationToken.None,
            MusoqApi.Source("#A", "entities", Chunks(new[]
            {
                new Person("Alice", 35, "NY"),
                new Person("Bob", 20, "LA")
            })));
        compiled.Parameters["minAge"] = 40;
        compiled.PhaseChanged += (_, _) => secondPhaseCount++;

        var names = rows.Select(static row => row.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "Alice" }, names);
        Assert.IsGreaterThan(0, firstPhaseCount);
        Assert.AreEqual(0, secondPhaseCount);
    }

    [TestMethod]
    public void Run_WhenRowsAreMissing_ShouldRejectAtEnumeration()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        Assert.Throws<InvalidOperationException>(() => compiled.Run(CancellationToken.None).ToArray());
    }

    [TestMethod]
    public void Run_WhenRowsAreSuppliedForUndeclaredSource_ShouldRejectAtRun()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        Assert.Throws<InvalidOperationException>(() =>
            compiled.Run(CancellationToken.None, MusoqApi.Source("#A", "other", Chunks(new[] { new Person("Alice", 35, "NY") }))).ToArray());
    }

    [TestMethod]
    public void Run_WhenRowsAreSuppliedTwiceForOneSource_ShouldRejectAtRun()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        var first = MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Alice", 35, "NY") }));
        var second = MusoqApi.Source("#A", "entities", Chunks(new[] { new Person("Bob", 41, "LA") }));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            compiled.Run(CancellationToken.None, first, second).ToArray());

        StringAssert.Contains(exception.Message, "were supplied more than once");
    }

    [TestMethod]
    public void Run_WhenRowsHaveIncompatibleType_ShouldRejectAtRun()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        Assert.Throws<InvalidOperationException>(() =>
            compiled.Run(CancellationToken.None, MusoqApi.Source("#A", "entities", Chunks(new[] { new OtherPerson("Alice") }))).ToArray());
    }

    [TestMethod]
    public void Run_WhenSourceThrowsAfterFirstRow_ShouldStreamFirstTypedResult()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        using var enumerator = compiled
            .Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", new ThrowOnSecondMoveChunkEnumerable<Person>(
                    new[] { new Person("Alice", 35, "NY") })))
            .GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("Alice", enumerator.Current.Name);
    }

    [TestMethod]
    public void CompileAndRun_WhenQueryUsesDistinctOrderSkipTake_ShouldReturnTypedRows()
    {
        var people = new[]
        {
            new Person("Alice", 35, "NY"),
            new Person("Bob", 20, "LA"),
            new Person("Charlie", 28, "NY"),
            new Person("Dana", 41, "LA"),
            new Person("Eve", 32, "SF")
        };

        var rows = MusoqApi
            .Query("select distinct p.City as Name from #A.entities() p order by p.City desc skip 1 take 2")
            .Source("#A", "entities", Chunks(people))
            .CompileAndRun<NameDto>(CancellationToken.None)
            .Select(static row => row.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "NY", "LA" }, rows);
    }

    [TestMethod]
    public void Run_WhenQueryOrdersRows_ShouldReadSourceBeforeFirstTypedResult()
    {
        var compiled = MusoqApi
            .Query("select p.Name as Name from #A.entities() p order by p.Name")
            .Source<Person>("#A", "entities")
            .Compile<NameDto>();

        using var enumerator = compiled
            .Run(
                CancellationToken.None,
                MusoqApi.Source("#A", "entities", new ThrowOnSecondMoveChunkEnumerable<Person>(
                    new[] { new Person("Alice", 35, "NY") })))
            .GetEnumerator();

        var exception = Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        StringAssert.Contains(exception.Message, "Second chunk");
    }

    private sealed class EmptyInMemorySchemaProvider : global::Musoq.Schema.ISchemaProvider
    {
        public global::Musoq.Schema.ISchema GetSchema(string schema)
        {
            throw new InvalidOperationException("Schema should not be used when artifact loading fails before runnable creation.");
        }
    }
}
