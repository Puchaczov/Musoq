using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class ConstantFoldingTests
{
    #region Tautological condition warnings (MQ5010)

    [TestMethod]
    public void WhenWhereIsAlwaysTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded, "Query should compile successfully despite tautological warning.");
        Assert.IsTrue(
            result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition),
            $"Expected MQ5010 warning but found: [{string.Join(", ", result.Warnings.Select(w => w.Code))}]");
    }

    [TestMethod]
    public void WhenWhereIsFoldedToTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenWhereOrFoldsToTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where false or true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenHavingIsAlwaysTrue_ShouldEmitTautologicalWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name, Count(Name) from #schema.first() group by Name having true", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void WhenTautologicalWarningMessage_ShouldMentionWhereClause()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where true", SingleEntitySource);

        var warning = result.Warnings.First(w => w.Code == DiagnosticCode.MQ5010_TautologicalCondition);
        StringAssert.Contains(warning.Message, "WHERE");
    }

    #endregion

    #region Contradictory condition warnings (MQ5011)

    [TestMethod]
    public void WhenWhereIsAlwaysFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded, "Query should compile successfully despite contradictory warning.");
        Assert.IsTrue(
            result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            $"Expected MQ5011 warning but found: [{string.Join(", ", result.Warnings.Select(w => w.Code))}]");
    }

    [TestMethod]
    public void WhenWhereIsFoldedToFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenWhereOrOrFoldsToFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where false or false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenHavingIsAlwaysFalse_ShouldEmitContradictoryWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name, Count(Name) from #schema.first() group by Name having false", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Warnings.Any(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition));
    }

    [TestMethod]
    public void WhenContradictoryWarningMessage_ShouldMentionNoRows()
    {
        var result = CompileWithDiagnostics("select Name from #schema.first() where false", SingleEntitySource);

        var warning = result.Warnings.First(w => w.Code == DiagnosticCode.MQ5011_ContradictoryCondition);
        StringAssert.Contains(warning.Message, "no rows");
    }

    #endregion

    #region No false-positive warnings

    [TestMethod]
    public void WhenWhereHasColumnReference_ShouldNotEmitWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where Value > 0", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(
            result.Warnings.Any(w =>
                w.Code == DiagnosticCode.MQ5010_TautologicalCondition ||
                w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            "No tautological/contradictory warnings expected for column-based conditions.");
    }

    [TestMethod]
    public void WhenWhereMixesColumnAndConstant_ShouldNotEmitWarning()
    {
        var result = CompileWithDiagnostics(
            "select Name from #schema.first() where true and Value > 0", SingleEntitySource);

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(
            result.Warnings.Any(w =>
                w.Code == DiagnosticCode.MQ5010_TautologicalCondition ||
                w.Code == DiagnosticCode.MQ5011_ContradictoryCondition),
            "No tautological/contradictory warnings when column references remain.");
    }

    #endregion
}
