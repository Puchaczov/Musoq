using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SymbolResolutionDiagnosticsTests : Schema.NegativeTests.NegativeTestsBase
{
    [TestMethod]
    public void UnknownSourceInKnownSchema_ReportsUnknownSourceWithFacts()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select * from #test.missing()"));

        AssertSingleError(exception, DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind, "missing");
        AssertHasGuidance(exception);
        Assert.AreEqual("#test", exception.PrimaryEnvelope.Arguments["schema"]);
        Assert.AreEqual("missing", exception.PrimaryEnvelope.Arguments["source"]);
    }

    [TestMethod]
    public void UnknownSchema_RemainsSchemaResolutionError()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select * from #absent.people()"));

        AssertSingleError(exception, DiagnosticCode.MQ3010_UnknownSchema, DiagnosticPhase.Bind, "#absent");
    }

    [TestMethod]
    public void UndefinedCteReference_ReportsTableDefinitionError()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select * from MissingCte"));

        AssertSingleError(exception, DiagnosticCode.MQ3023_TableNotDefined, DiagnosticPhase.Bind, "MissingCte");
    }

    [TestMethod]
    public void UnknownQualifiedAlias_ReportsAliasErrorOnly()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select missing.Name from #test.people() people"));

        AssertSingleError(exception, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "missing");
    }

    [TestMethod]
    public void UnknownQualifiedAlias_ReportsRepairableSuggestionAtAliasToken()
    {
        const string validQuery = "select people.Name from #test.people() people";
        var query = validQuery.Replace("people.Name", "peple.Name", StringComparison.Ordinal);

        var exception = Assert.Throws<MusoqQueryException>(() => CompileQuery(query));
        var envelope = exception.PrimaryEnvelope;
        var aliasStart = query.IndexOf("peple", StringComparison.Ordinal);
        var aliasSpan = new TextSpan(aliasStart, "peple".Length);

        AssertSingleError(exception, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "Did you mean 'people'?");
        Assert.AreEqual(aliasStart, envelope.Offset);
        Assert.AreEqual(aliasSpan.Length, envelope.Length);
        Assert.AreEqual("peple", query.Substring(envelope.Offset!.Value, envelope.Length!.Value));
        Assert.AreEqual("peple", envelope.Arguments["alias"]);
        Assert.AreEqual("people", envelope.Arguments["suggestion"]);
        Assert.AreEqual("people", envelope.Arguments["availableAliases"]);

        Assert.HasCount(1, envelope.Actions);
        var action = envelope.Actions[0];
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual(aliasSpan, action.TextEdit!.Span);
        Assert.AreEqual("people", action.TextEdit.NewText);

        var repairedQuery = query.Remove(action.TextEdit.Span.Start, action.TextEdit.Span.Length)
            .Insert(action.TextEdit.Span.Start, action.TextEdit.NewText);
        CompileQuery(repairedQuery);
    }

    [TestMethod]
    public void UnknownColumnOnKnownAlias_ReportsColumnError()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select people.Amount from #test.people() people"));

        AssertSingleError(exception, DiagnosticCode.MQ3001_UnknownColumn, DiagnosticPhase.Bind, "Amount");
    }

    [TestMethod]
    public void UnknownPropertyOnKnownObject_ReportsPropertyError()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select Info.Missing from #test.nested()"));

        AssertSingleError(exception, DiagnosticCode.MQ3028_UnknownProperty, DiagnosticPhase.Bind, "Missing");
    }

    [TestMethod]
    public void UnknownSource_DoesNotCreateDependentColumnCascade()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CompileQuery("select source.Name from #test.missing() source"));

        AssertSingleError(exception, DiagnosticCode.MQ3085_UnknownSource, DiagnosticPhase.Bind, "missing");
        Assert.IsFalse(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ3027_InvalidExpressionType));
    }

    [TestMethod]
    public void CteNameInNestedQuery_IsBoundInItsOwnScope()
    {
        var query = "with people_cte as (select Name from #test.people()) " +
                    "select people_cte.Name from people_cte";

        var compiled = CompileQuery(query);
        var result = compiled.Run(TokenSource.Token);

        Assert.IsGreaterThan(0, result.Count);
    }
}

[TestClass]
public sealed class MultiSchemaSymbolResolutionDiagnosticsTests : Schema.Multi.MultiSchemaTestBase
{
    [TestMethod]
    public void AmbiguousUnqualifiedColumn_ReportsAmbiguousColumn()
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                "select FirstItem from #schema.first() first inner join #schema.second() second on first.FirstItem = second.FirstItem",
                [new Schema.Multi.First.FirstEntity()],
                [new Schema.Multi.Second.SecondEntity()]));

        AssertSingleError(exception, DiagnosticCode.MQ3002_AmbiguousColumn, DiagnosticPhase.Bind, "FirstItem");
    }
}
