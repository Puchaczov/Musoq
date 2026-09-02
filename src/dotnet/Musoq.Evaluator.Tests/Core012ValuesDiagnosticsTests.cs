using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core012ValuesDiagnosticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void MissingValuesField_ReportsRowInsertionPointAndStructuredFacts()
    {
        const string validQuery =
            "from values { { Name: 'A', Approved: true }, { Name: 'B', Approved: false } } packages select packages.Name";
        var query = validQuery.Replace(", Approved: false", string.Empty, StringComparison.Ordinal);
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, EmptySources()));
        var rowStart = query.IndexOf("{ Name: 'B'", StringComparison.Ordinal);
        var insertion = new TextSpan(query.IndexOf('}', rowStart), 0);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ3055_InvalidValuesSource, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(insertion.Start, envelope.Offset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual("values", envelope.Arguments["sourceKind"]);
        Assert.AreEqual("missing-field", envelope.Arguments["constraint"]);
        Assert.AreEqual("2", envelope.Arguments["row"]);
        Assert.AreEqual("Approved", envelope.Arguments["field"]);
        Assert.AreEqual("Name, Approved", envelope.Arguments["expectedFields"]);
        AssertHasGuidance(exception);
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void NonStaticValuesField_ReportsOnlyTheForbiddenExpression()
    {
        const string validQuery =
            "from values { { Name: 'A' } } packages select packages.Name";
        var query = validQuery.Replace("Name: 'A'", "Name: ToUpper('A')", StringComparison.Ordinal);
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, EmptySources()));
        var expressionStart = query.IndexOf("ToUpper", StringComparison.Ordinal);
        var expressionSpan = new TextSpan(expressionStart, "ToUpper('A')".Length);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ3055_InvalidValuesSource, envelope.Code);
        Assert.AreEqual(expressionSpan.Start, envelope.Offset);
        Assert.AreEqual(expressionSpan.Length, envelope.Length);
        Assert.AreEqual("non-static-expression", envelope.Arguments["constraint"]);
        Assert.AreEqual("Name", envelope.Arguments["field"]);
        StringAssert.Contains(envelope.Message, "constant literal expression");
        AssertHasGuidance(exception);
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    [TestMethod]
    public void IncompatibleValuesColumn_ReportsOffendingExpressionAndTypes()
    {
        const string validQuery =
            "from values { { Score: 10 }, { Score: 20 } } scores select scores.Score";
        var query = validQuery.Replace("Score: 20", "Score: 'high'", StringComparison.Ordinal);
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, EmptySources()));
        var expressionStart = query.IndexOf("'high'", StringComparison.Ordinal);
        var expressionSpan = new TextSpan(expressionStart, "'high'".Length);
        var envelope = exception.PrimaryEnvelope;

        Assert.AreEqual(DiagnosticCode.MQ3055_InvalidValuesSource, envelope.Code);
        Assert.AreEqual(expressionSpan.Start, envelope.Offset);
        Assert.AreEqual(expressionSpan.Length, envelope.Length);
        Assert.AreEqual("incompatible-types", envelope.Arguments["constraint"]);
        Assert.AreEqual("Score", envelope.Arguments["field"]);
        StringAssert.Contains(envelope.Arguments["actualTypes"], "String");
        StringAssert.Contains(envelope.Message, "Score");
        AssertHasGuidance(exception);
        Assert.IsFalse(envelope.Actions.Any(action => action.TextEdit != null));
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> EmptySources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>();
    }
}
