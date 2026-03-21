using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

/// <summary>
///     Tests for error position reporting (line, column, length) in diagnostics
/// </summary>
[TestClass]
public class ConverterExceptions_ErrorPositionTests
{
    [TestMethod]
    public void WhenUnknownColumn_ShouldReportCorrectPosition()
    {
        var query = "SELECT nonexistent FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenMultilineQueryWithUnknownColumn_ShouldReportCorrectLine()
    {
        var query = "SELECT\n  1 as valid,\n  nonexistent_column\nFROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(3, env.Line);
        Assert.AreEqual(3, env.Column);
        Assert.AreEqual(18, env.Length);
    }

    [TestMethod]
    public void WhenDotAccessUnknownProperty_ShouldReportPosition()
    {
        var query = "SELECT a.nonexistent FROM #system.dual() a";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsGreaterThanOrEqualTo(1, envelopes.Count,
            $"Expected at least 1 error, got {envelopes.Count}");

        var env = envelopes[0];
        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(10, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnAfterValidColumn_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 as a, bad_column FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(16, env.Column);
        Assert.AreEqual(10, env.Length);
    }

    [TestMethod]
    public void WhenTwoUnknownColumns_ShouldReportBothPositions()
    {
        var query = "SELECT bad1, bad2 FROM #system.dual()";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.HasCount(2, envelopes);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[0].Code);
        Assert.AreEqual(1, envelopes[0].Line);
        Assert.AreEqual(8, envelopes[0].Column);
        Assert.AreEqual(4, envelopes[0].Length);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelopes[1].Code);
        Assert.AreEqual(1, envelopes[1].Line);
        Assert.AreEqual(14, envelopes[1].Column);
        Assert.AreEqual(4, envelopes[1].Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInWhereClause_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy FROM #system.dual() WHERE missing = 1";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(40, env.Column);
        Assert.AreEqual(7, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInOrderBy_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy FROM #system.dual() ORDER BY nonexistent";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(43, env.Column);
        Assert.AreEqual(11, env.Length);
    }

    [TestMethod]
    public void WhenILikeUsed_ShouldReportPositionOfILikeToken()
    {
        var query = "SELECT Dummy FROM #system.dual() WHERE Dummy ILIKE 'x'";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(46, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenNotEqualOperatorUsed_ShouldReportPositionOfBangEquals()
    {
        var query = "SELECT 1 FROM #system.dual() WHERE 1 != 2";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ2019_InvalidOperator, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(38, env.Column);
        Assert.AreEqual(2, env.Length);
    }

    [TestMethod]
    public void WhenMultilineQuery_ErrorOnSecondLine_ShouldReportLine2()
    {
        var query = "SELECT\n  missing_col\nFROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(2, env.Line);
        Assert.AreEqual(3, env.Column);
    }

    [TestMethod]
    public void WhenMultilineQuery_ErrorOnLastLine_ShouldReportCorrectLine()
    {
        var query = "SELECT\n  Dummy\nFROM #system.dual()\nORDER BY bad_col";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, env.Code);
        Assert.AreEqual(4, env.Line);
        Assert.AreEqual(10, env.Column);
        Assert.AreEqual(7, env.Length);
    }

    [TestMethod]
    public void WhenDivisionByZeroLiteral_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 / 0 FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3008_DivisionByZero, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenModuloByZeroLiteral_ShouldReportCorrectPosition()
    {
        var query = "SELECT 1 % 0 FROM #system.dual()";
        var env = CompileAndGetSingleEnvelope(query);

        Assert.AreEqual(DiagnosticCode.MQ3008_DivisionByZero, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(8, env.Column);
        Assert.AreEqual(5, env.Length);
    }

    [TestMethod]
    public void WhenDuplicateAliasInJoin_ShouldReportCorrectPosition()
    {
        var query = "SELECT a.Dummy FROM #system.dual() a INNER JOIN #system.dual() a ON 1 = 1";
        var env = CompileAndGetSingleEnvelope(query);
        
        Assert.AreEqual(DiagnosticCode.MQ3021_DuplicateAlias, env.Code);
        Assert.AreEqual(1, env.Line);
        Assert.AreEqual(64, env.Column);
        Assert.AreEqual(1, env.Length);
    }

    [TestMethod]
    public void WhenUnknownColumnInHaving_ShouldReportCorrectPosition()
    {
        var query = "SELECT Dummy, 1 FROM #system.dual() GROUP BY Dummy HAVING Dummy = 'x' AND missing = 1";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsGreaterThanOrEqualTo(1, envelopes.Count,
            $"Expected at least 1 error but got {envelopes.Count}");

        var unknownColEnvelope = envelopes.FirstOrDefault(e => e.Code == DiagnosticCode.MQ3001_UnknownColumn);
        Assert.IsNotNull(unknownColEnvelope);
        Assert.AreEqual(1, unknownColEnvelope.Line);
        Assert.AreEqual(75, unknownColEnvelope.Column);
        Assert.AreEqual(7, unknownColEnvelope.Length);
    }

    [TestMethod]
    public void WhenValidQuery_ShouldProduceNoEnvelopes()
    {
        var query = "SELECT Dummy FROM #system.dual()";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsEmpty(envelopes);
    }

    [TestMethod]
    public void WhenValidQueryWithAlias_ShouldProduceNoEnvelopes()
    {
        var query = "SELECT a.Dummy FROM #system.dual() a";
        var envelopes = CompileAndGetEnvelopes(query);

        Assert.IsEmpty(envelopes);
    }

    private static MusoqErrorEnvelope CompileAndGetSingleEnvelope(string query)
    {
        var envelopes = CompileAndGetEnvelopes(query);
        Assert.HasCount(1, envelopes,
            $"Expected 1 error but got {envelopes.Count}: [{string.Join(", ", envelopes.Select(e => $"{e.CodeString}: {e.Message}"))}]");
        return envelopes[0];
    }

    private static IReadOnlyList<MusoqErrorEnvelope> CompileAndGetEnvelopes(string query)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            System.Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
        return result.ToEnvelopes();
    }
}
