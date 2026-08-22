using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;
using MusoqTypedProfileFixtures;
using Musoq.Tests.Common;
using MusoqApi = Musoq.Converter.Musoq;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;
using static Musoq.Converter.Tests.TwoModeTestFixtures;

namespace Musoq.Converter.Tests;

[TestClass]
public class TypedProfilingTests
{
    static TypedProfilingTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void RunWithProfile_WhenRowsAreFullyEnumerated_ShouldFinalizeCompleteProfile()
    {
        var result = CreateProfileQuery<NameDto>()
            .RunWithProfile(CancellationToken.None, CreatePeopleSource());

        Assert.IsTrue(result.IsSourceExecutionComplete);
        Assert.Throws<InvalidOperationException>(() => _ = result.Profile);

        var rows = result.Rows.ToArray();

        CollectionAssert.AreEqual(new[] { "Alice", "Bob" }, rows.Select(static row => row.Name).ToArray());
        Assert.IsTrue(result.IsFinalized);
        Assert.IsTrue(result.IsSourceExecutionComplete);
        Assert.IsTrue(result.IsComplete);
        Assert.IsNull(result.Exception);
        Assert.IsGreaterThan(0, result.Profile.Sources.Count);
        StringAssert.Contains(result.ProfileText, "Musoq query profile");
    }

    [TestMethod]
    public void RunWithProfile_WithQueryProgressOptions_ShouldPublishFinalSnapshot()
    {
        var snapshots = new List<QueryProgressEventArgs>();
        var result = CreateProfileQuery<NameDto>().RunWithProfile(new TypedQueryRunOptions
        {
            QueryProgress = (_, args) => snapshots.Add(args),
            QueryProgressOptions = new QueryProgressOptions
            {
                RowsPerUpdate = 1,
                MinimumInterval = TimeSpan.FromDays(1)
            }
        }, CreatePeopleSource());

        _ = result.Rows.ToArray();

        Assert.IsTrue(snapshots.Count > 0);
        Assert.IsTrue(snapshots[^1].IsFinal);
        Assert.AreEqual(2, snapshots[^1].QueryRowsProcessed);
    }

    [TestMethod]
    public void RunWithProfile_WhenTakeStopsEarly_ShouldFinalizeIncompleteProfile()
    {
        var result = CreateProfileQuery<NameDto>()
            .RunWithProfile(CancellationToken.None, CreatePeopleSource());

        Assert.IsTrue(result.IsSourceExecutionComplete);
        var rows = result.Rows.Take(1).ToArray();

        CollectionAssert.AreEqual(new[] { "Alice" }, rows.Select(static row => row.Name).ToArray());
        Assert.IsTrue(result.IsFinalized);
        Assert.IsTrue(result.IsSourceExecutionComplete);
        Assert.IsFalse(result.IsComplete);
        Assert.IsNull(result.Exception);
        Assert.IsGreaterThan(0, result.Profile.Sources.Count);
    }

    [TestMethod]
    public void RunWithProfile_WhenEnumeratorIsDisposedEarly_ShouldFinalizeIncompleteProfile()
    {
        var result = CreateProfileQuery<NameDto>()
            .RunWithProfile(CancellationToken.None, CreatePeopleSource());

        Assert.IsTrue(result.IsSourceExecutionComplete);
        using (var enumerator = result.Rows.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual("Alice", enumerator.Current.Name);
        }

        Assert.IsTrue(result.IsFinalized);
        Assert.IsTrue(result.IsSourceExecutionComplete);
        Assert.IsFalse(result.IsComplete);
        Assert.IsNull(result.Exception);
        Assert.IsGreaterThan(0, result.Profile.Sources.Count);
    }

    [TestMethod]
    public void RunWithProfile_WhenProjectionThrows_ShouldFinalizeWithException()
    {
        var result = CreateProfileQuery<ThrowingDto>()
            .RunWithProfile(CancellationToken.None, CreatePeopleSource());

        Assert.IsTrue(result.IsSourceExecutionComplete);
        var exception = Assert.Throws<InvalidOperationException>(() => result.Rows.ToArray());

        Assert.AreEqual("Projection failed.", exception.Message);
        Assert.IsTrue(result.IsFinalized);
        Assert.IsTrue(result.IsSourceExecutionComplete);
        Assert.IsFalse(result.IsComplete);
        Assert.AreSame(exception, result.Exception);
        Assert.IsGreaterThan(0, result.Profile.Sources.Count);
    }

    [TestMethod]
    public void RunWithProfile_WhenSourceThrows_ShouldPropagateSourceException()
    {
        var source = MusoqApi.Source("#A", "entities", new ThrowOnSecondMoveEnumerable<ProfilePerson>(
            new[] { new ProfilePerson("Alice", 35) }));

        Assert.Throws<InvalidOperationException>(() => CreateProfileQuery<NameDto>()
            .RunWithProfile(CancellationToken.None, source));
    }

    [TestMethod]
    public void RunWithProfile_WhenTokenIsAlreadyCancelled_ShouldThrowBeforeStarting()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => CreateProfileQuery<NameDto>()
            .RunWithProfile(cancellation.Token, CreatePeopleSource()));
    }

    [TestMethod]
    public void RunWithProfile_WhenReusableQueryRunsTwice_ShouldKeepOneCompiledRunnableFactory()
    {
        var query = CreateProfileQuery<NameDto>();
        var diagnostics = query.Diagnostics;
        var runnableType = diagnostics.RunnableType;

        _ = query.RunWithProfile(CancellationToken.None, CreatePeopleSource()).Rows.ToArray();
        _ = query.RunWithProfile(CancellationToken.None, CreatePeopleSource()).Rows.ToArray();

        Assert.AreSame(runnableType, query.Diagnostics.RunnableType);
        Assert.AreEqual(QueryResultMode.Table, diagnostics.ResultMode);
        Assert.AreEqual(FinalResultSinkKind.TableRowsMaterialized, diagnostics.SelectedResultSinkKind);
        Assert.AreEqual(QueryResultRowPathKind.DirectRows, diagnostics.RowPathKind);
        Assert.IsTrue(diagnostics.IsProfiled);
        Assert.AreEqual(TypedQueryProfileMode.TableBacked, diagnostics.ProfileMode);
    }

    [TestMethod]
    public void RunWithProfile_WhenReusableQueryGetsDifferentRows_ShouldUseRowsFromEachRun()
    {
        var query = CreateProfileQuery<NameDto>();

        var first = query
            .RunWithProfile(CancellationToken.None, CreatePeopleSource(new ProfilePerson("Alice", 35)))
            .Rows
            .Select(static row => row.Name)
            .ToArray();
        var second = query
            .RunWithProfile(CancellationToken.None, CreatePeopleSource(new ProfilePerson("Bob", 20)))
            .Rows
            .Select(static row => row.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "Alice" }, first);
        CollectionAssert.AreEqual(new[] { "Bob" }, second);
    }

    private static ICompiledTypedProfileQuery<TOut> CreateProfileQuery<TOut>()
    {
        return MusoqApi
            .Query("select p.Name as Name from #A.entities() p order by p.Name")
            .Source<ProfilePerson>("#A", "entities")
            .CompileForProfile<TOut>();
    }

    private static MusoqSourceRows CreatePeopleSource()
    {
        return CreatePeopleSource(
            new ProfilePerson("Bob", 20),
            new ProfilePerson("Alice", 35));
    }

    private static MusoqSourceRows CreatePeopleSource(params ProfilePerson[] people)
    {
        return MusoqApi.Source("#A", "entities", Chunks(people));
    }

    public sealed class ThrowingDto
    {
        public ThrowingDto(string name)
        {
            throw new InvalidOperationException("Projection failed.");
        }
    }

    private sealed class ThrowOnSecondMoveEnumerable<T>(IReadOnlyList<T> first) : IEnumerable<IReadOnlyList<T>>
    {
        public IEnumerator<IReadOnlyList<T>> GetEnumerator()
        {
            yield return first;
            throw new InvalidOperationException("Second chunk failed.");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
