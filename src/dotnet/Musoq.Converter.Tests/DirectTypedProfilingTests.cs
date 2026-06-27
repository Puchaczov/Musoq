using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Tests.Common;
using NameDto = Musoq.Converter.Tests.TwoModeTestFixtures.NameDto;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DirectTypedProfilingTests
{
    static DirectTypedProfilingTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void RunWithProfile_WhenDirectQueryRunsRepeatedly_ShouldUseIndependentParameters()
    {
        var query = Compile(
            "param(expected: string) select d.Dummy as Name from #system.dual() d where d.Dummy = $expected");

        var first = query
            .RunWithProfile(new TypedQueryRunOptions(
                CancellationToken.None,
                new Dictionary<string, object?> { ["expected"] = "single" }))
            .Rows
            .Select(static row => row.Name)
            .ToArray();
        var second = query
            .RunWithProfile(new TypedQueryRunOptions(
                CancellationToken.None,
                new Dictionary<string, object?> { ["expected"] = "missing" }))
            .Rows
            .Select(static row => row.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "single" }, first);
        Assert.AreEqual(0, second.Length);
    }

    [TestMethod]
    public async Task RunWithProfile_WhenDirectQueryRunsConcurrently_ShouldUseIndependentCallbacks()
    {
        var query = Compile(
            "param(expected: string) select d.Dummy as Name from #system.dual() d where d.Dummy = $expected order by d.Dummy");
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;

        var firstTask = Task.Run(() => query
            .RunWithProfile(new TypedQueryRunOptions(
                CancellationToken.None,
                new Dictionary<string, object?> { ["expected"] = "single" },
                (_, _) => Interlocked.Increment(ref firstPhaseCount)))
            .Rows
            .ToArray());
        var secondTask = Task.Run(() => query
            .RunWithProfile(new TypedQueryRunOptions(
                CancellationToken.None,
                new Dictionary<string, object?> { ["expected"] = "missing" },
                (_, _) => Interlocked.Increment(ref secondPhaseCount)))
            .Rows
            .ToArray());

        var results = await Task.WhenAll(firstTask, secondTask).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "single" }, results[0].Select(static row => row.Name).ToArray());
        Assert.AreEqual(0, results[1].Length);
        Assert.IsGreaterThan(0, firstPhaseCount);
        Assert.IsGreaterThan(0, secondPhaseCount);
    }

    [TestMethod]
    public void RunWithProfile_WhenOptionsInputChangesAfterCapture_ShouldUseSnapshot()
    {
        var query = Compile(
            "param(expected: string) select d.Dummy as Name from #system.dual() d where d.Dummy = $expected order by d.Dummy");
        var parameters = new Dictionary<string, object?> { ["expected"] = "single" };
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        var options = new TypedQueryRunOptions(
            CancellationToken.None,
            parameters,
            (_, _) => Interlocked.Increment(ref firstPhaseCount));
        parameters["expected"] = "missing";

        var result = query.RunWithProfile(options);
        query.PhaseChanged += (_, _) => Interlocked.Increment(ref secondPhaseCount);

        var rows = result.Rows.Select(static row => row.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "single" }, rows);
        Assert.IsGreaterThan(0, firstPhaseCount);
        Assert.AreEqual(0, secondPhaseCount);
    }

    [TestMethod]
    public void RunWithProfile_WhenObjectInitializerOptionsInputChangesAfterCapture_ShouldUseSnapshot()
    {
        var query = Compile(
            "param(expected: string) select d.Dummy as Name from #system.dual() d where d.Dummy = $expected");
        var parameters = new Dictionary<string, object?> { ["expected"] = "single" };
        var options = new TypedQueryRunOptions { Parameters = parameters };
        parameters["expected"] = "missing";

        var rows = query
            .RunWithProfile(options)
            .Rows
            .Select(static row => row.Name)
            .ToArray();

        CollectionAssert.AreEqual(new[] { "single" }, rows);
    }

    private static CompiledTypedProfileQuery<NameDto> Compile(string query)
    {
        return InstanceCreator.CompileForTypedProfile<NameDto>(
            query,
            Guid.NewGuid().ToString("N"),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
    }

}
