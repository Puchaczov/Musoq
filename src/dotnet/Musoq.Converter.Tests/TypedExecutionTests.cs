using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

[TestClass]
public class TypedExecutionTests
{
    static TypedExecutionTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenOutputHasMatchingConstructor_ShouldReturnTypedRows()
    {
        var query = Compile<ConstructorDto>("select d.Dummy as Dummy from #system.dual() d");

        var rows = query.Run(CancellationToken.None).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("single", rows[0].Dummy);
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenOutputHasSettableProperty_ShouldReturnTypedRows()
    {
        var query = Compile<PropertyDto>("select d.Dummy as Dummy from #system.dual() d");

        var rows = query.Run(CancellationToken.None).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("single", rows[0].Dummy);
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenOutputHasPublicField_ShouldReturnTypedRows()
    {
        var query = Compile<FieldDto>("select d.Dummy as Dummy from #system.dual() d");

        var rows = query.Run(CancellationToken.None).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("single", rows[0].Dummy);
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenQueryHasParameter_ShouldUseExistingParameterBinding()
    {
        var query = Compile<PropertyDto>(
            "param(expected: string) select d.Dummy as Dummy from #system.dual() d where d.Dummy = $expected");
        query.Parameters["expected"] = "single";

        var rows = query.Run(CancellationToken.None).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("single", rows[0].Dummy);
    }

    [TestMethod]
    public void TypedRun_WithPerRunOptions_WhenParametersChangeAfterRun_ShouldUseSnapshot()
    {
        var query = Compile<PropertyDto>(
            "param(expected: string) select d.Dummy as Dummy from #system.dual() d where d.Dummy = $expected");
        var parameters = new Dictionary<string, object?> { ["expected"] = "single" };

        var rows = query.Run(new TypedQueryRunOptions(CancellationToken.None, parameters));
        parameters["expected"] = "missing";

        var materialized = rows.ToArray();

        Assert.AreEqual(1, materialized.Length);
        Assert.AreEqual("single", materialized[0].Dummy);
    }

    [TestMethod]
    public void TypedRun_WithObjectInitializerOptions_WhenParametersChangeAfterRun_ShouldUseSnapshot()
    {
        var query = Compile<PropertyDto>(
            "param(expected: string) select d.Dummy as Dummy from #system.dual() d where d.Dummy = $expected");
        var parameters = new Dictionary<string, object?> { ["expected"] = "single" };

        var rows = query.Run(new TypedQueryRunOptions { Parameters = parameters });
        parameters["expected"] = "missing";

        var materialized = rows.ToArray();

        Assert.AreEqual(1, materialized.Length);
        Assert.AreEqual("single", materialized[0].Dummy);
    }

    [TestMethod]
    public void TypedRun_WhenCompatibilityEventsChangeAfterRun_ShouldUseSnapshot()
    {
        var query = Compile<PropertyDto>(
            "select d.Dummy as Dummy from #system.dual() d order by d.Dummy");
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        query.PhaseChanged += (_, _) => firstPhaseCount++;

        var rows = query.Run(CancellationToken.None);
        query.PhaseChanged += (_, _) => secondPhaseCount++;

        Assert.AreEqual(1, rows.ToArray().Length);
        Assert.IsGreaterThan(0, firstPhaseCount);
        Assert.AreEqual(0, secondPhaseCount);
    }

    [TestMethod]
    public void TypedRun_WhenProjectionThrows_ShouldPropagateProjectionException()
    {
        var query = Compile<ThrowingConstructorDto>("select d.Dummy as Dummy from #system.dual() d");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            query.Run(new TypedQueryRunOptions(CancellationToken.None)).ToArray());

        Assert.AreEqual("Projection failed.", exception.Message);
    }

    [TestMethod]
    public void TypedRun_WhenEnumerableIsEnumeratedTwice_ShouldRejectSecondEnumeration()
    {
        var query = Compile<PropertyDto>("select d.Dummy as Dummy from #system.dual() d");
        var rows = query.Run(CancellationToken.None);

        Assert.AreEqual(1, rows.ToArray().Length);
        Assert.Throws<InvalidOperationException>(() => rows.ToArray());
    }

    [TestMethod]
    public void TypedRun_WhenTokenIsAlreadyCancelled_ShouldThrowBeforeStarting()
    {
        var query = Compile<PropertyDto>("select d.Dummy as Dummy from #system.dual() d");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => query.Run(cancellation.Token).ToArray());
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenAliasesAreDuplicated_ShouldRejectAtCompileTime()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Compile<TwoColumnConstructorDto>("select d.Dummy as Dummy, d.Dummy as Dummy from #system.dual() d"));
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenOutputMemberIsMissing_ShouldRejectAtCompileTime()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Compile<MissingMemberDto>("select d.Dummy as Dummy from #system.dual() d"));
    }

    [TestMethod]
    public void CompileForTypedExecution_WhenOutputMemberTypeIsIncompatible_ShouldRejectAtCompileTime()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Compile<IncompatibleMemberDto>("select d.Dummy as Dummy from #system.dual() d"));
    }

    private static global::Musoq.Evaluator.CompiledTypedQuery<TOut> Compile<TOut>(string query)
    {
        return InstanceCreator.CompileForTypedExecution<TOut>(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
    }

    public sealed record ConstructorDto(string Dummy);

    public sealed record TwoColumnConstructorDto(string Dummy, string Other);

    public sealed class PropertyDto
    {
        public string Dummy { get; set; } = string.Empty;
    }

    public sealed class FieldDto
    {
        public string Dummy = string.Empty;
    }

    public sealed class MissingMemberDto
    {
        public string Other { get; set; } = string.Empty;
    }

    public sealed class IncompatibleMemberDto
    {
        public int Dummy { get; set; }
    }

    public sealed class ThrowingConstructorDto
    {
        public ThrowingConstructorDto(string dummy)
        {
            throw new InvalidOperationException("Projection failed.");
        }

        public string Dummy { get; } = string.Empty;
    }
}
