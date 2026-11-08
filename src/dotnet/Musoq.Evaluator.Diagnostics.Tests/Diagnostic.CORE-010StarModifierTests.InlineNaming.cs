using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class DiagnosticCore010StarModifierTests
{
    private const string ReplacementRenameCandidateQuery =
        "select * exclude (Drop) replace (Keep + 1i as Keep) rename (Keep as Kept) from values { (Keep: 10i, Drop: 99i, Other: 'x') } v";

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P2-0118")]
    public void Current_P2_0118_StarModifiersRenameReplacement()
    {
        AssertInlineModifierResult(ReplacementRenameCandidateQuery,
            [("Kept", typeof(int)), ("Other", typeof(string))], [11, "x"]);
    }

    [TestMethod]
    public void InlineReplacementRename_ShouldPreserveDeclaredAndEstablishedNames()
    {
        AssertInlineModifierResult(ReplacementRenameCandidateQuery,
            [("Kept", typeof(int)), ("Other", typeof(string))], [11, "x"]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0199")]
    public void InlineModifierChainComposesWithUnqualifiedUnchangedOutput()
    {
        const string query = "select * exclude (drop) replace (NextValue * 2 as nextvalue) rename (NEXTVALUE as Renamed) from values { (Keep: 7, Drop: 9, NextValue: 3) } p";
        AssertInlineModifierResult(query, [("Keep", typeof(int)), ("Renamed", typeof(int))], [7, 6]);
    }

    [TestMethod]
    public void InlineModifierChainWithExplicitRename_ShouldPreserveOrderAndValues()
    {
        const string query = "select * exclude (drop) replace (NextValue * 2 as nextvalue) rename (Keep as Keep, NEXTVALUE as Renamed) from values { (Keep: 7, Drop: 9, NextValue: 3) } p";
        AssertInlineModifierResult(query, [("Keep", typeof(int)), ("Renamed", typeof(int))], [7, 6]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0198")]
    public void InlineNotLikeMatchesCaseInsensitivelyWithUnqualifiedOutput()
    {
        const string query = "select * not like '%k%' from values { (Keep: 7, Drop: 9) } p";
        AssertInlineModifierResult(query, [("Drop", typeof(int))], [9]);
    }

    [TestMethod]
    public void InlineNotLikeWithExplicitRename_ShouldRemoveMixedCasePatternMatch()
    {
        const string query = "select * not like '%k%' rename (Drop as Drop) from values { (Keep: 7, Drop: 9) } p";
        AssertInlineModifierResult(query, [("Drop", typeof(int))], [9]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0196")]
    public void InlineReplacePreservesPositionWithUnqualifiedOutputNames()
    {
        const string query = "select * replace (NextValue * 2 as NextValue) from values { (FirstValue: 7, NextValue: 9) } p";
        AssertInlineModifierResult(query, [("FirstValue", typeof(int)), ("NextValue", typeof(int))], [7, 18]);
    }

    [TestMethod]
    public void InlineReplaceWithExplicitRename_ShouldPreserveProjectionPositions()
    {
        const string query = "select * replace (NextValue * 2 as NextValue) rename (FirstValue as FirstValue, NextValue as NextValue) from values { (FirstValue: 7, NextValue: 9) } p";
        AssertInlineModifierResult(query, [("FirstValue", typeof(int)), ("NextValue", typeof(int))], [7, 18]);
    }

    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P1-0194")]
    public void InlineExcludeMatchesCaseInsensitivelyWithUnqualifiedOutput()
    {
        const string query = "select * exclude (dRoP) from values { (Keep: 7, Drop: 9) } p";
        AssertInlineModifierResult(query, [("Keep", typeof(int))], [7]);
    }

    [TestMethod]
    public void InlineExcludeWithExplicitRename_ShouldRemoveMixedCaseTarget()
    {
        const string query = "select * exclude (dRoP) rename (Keep as Keep) from values { (Keep: 7, Drop: 9) } p";
        AssertInlineModifierResult(query, [("Keep", typeof(int))], [7]);
    }

    private void AssertInlineModifierResult(string query, (string Name, Type Type)[] columns, object?[] values)
    {
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(result, columns);
        TableMaterializationTestHelper.AssertRowsUnordered(result, values);
    }
}
