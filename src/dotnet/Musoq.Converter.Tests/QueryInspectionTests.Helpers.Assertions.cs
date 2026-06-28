using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static void AssertProjectedColumnsInclude(PhysicalSchemaScanNode scan, params string[] expectedColumns)
    {
        foreach (var expectedColumn in expectedColumns)
            CollectionAssert.Contains(scan.ProjectedColumns, expectedColumn);
    }

    private QueryInspectionResult CreateApplyInspection(string query)
    {
        return Inspect(query, CreateApplyCandidateSchemaProvider());
    }

    private static void AssertTextEquals(string expected, string actual)
    {
        Assert.AreEqual(Normalize(expected), Normalize(actual));
    }

    private static void AssertExecutionPlanContains(string expected, string actual)
    {
        Assert.Contains(expected, NormalizeExecutionPlanText(actual));
    }

    private static void AssertExecutionPlanDoesNotContain(string unexpected, string actual)
    {
        Assert.IsFalse(NormalizeExecutionPlanText(actual).Contains(unexpected, StringComparison.Ordinal));
    }

    private static void AssertGeneratedCSharpDoesNotContain(string unexpected, string actual)
    {
        Assert.IsFalse(NormalizeGeneratedCSharpCode(actual).Contains(unexpected, StringComparison.Ordinal));
    }

    private static void AssertUsesExecutionBackend(QueryInspectionResult result)
    {
        Assert.Contains("ExecutionPlan [compiled]", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("ExecutionPlanUnsupported", result.ExecutionPlanText);
    }

    private static void AssertTypedAggregateContext(string actual)
    {
        var normalized = NormalizeExecutionPlanText(actual);

        Assert.Contains("CreateAggregateContext [rootGroup, group, groupsToFinalize; typed:", normalized);
        Assert.Contains("AggregateGroup [", normalized);
        Assert.Contains("TypedAggregateSet [", normalized);
    }

    private static void AssertTypedSingleKeyAggregateContext(string actual)
    {
        var normalized = NormalizeExecutionPlanText(actual);

        Assert.Contains("CreateSingleKeyAggregateContext [groups: string ->", normalized);
        Assert.Contains("AggregateGroup [", normalized);
        Assert.Contains("TypedAggregateSet [", normalized);
    }

    private static void AssertTypedValueTupleAggregateContext(string actual)
    {
        var normalized = NormalizeExecutionPlanText(actual);

        Assert.Contains("CreateValueTupleAggregateContext [groups:", normalized);
        Assert.Contains("AggregateGroup [", normalized);
        Assert.Contains("GetOrAddValueTupleAggregateGroup [", normalized);
        Assert.Contains("; typed:", normalized);
        Assert.Contains("TypedAggregateSet [", normalized);
    }

    private static void AssertTypedAggregateSet(string actual)
    {
        Assert.Contains("TypedAggregateSet [", NormalizeExecutionPlanText(actual));
    }

    private static void AssertNoLegacyAggregateRuntime(string generatedCSharp)
    {
        var normalized = NormalizeGeneratedCSharpCode(generatedCSharp);

        Assert.IsFalse(normalized.Contains("private sealed class ResultAggregateGroup : Group", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains("GroupLayout", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains("GroupSlot", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains(".GetValue<", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains(".SetValue(", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains(".Parent.__agg", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains("new ObjectsRow(new object[]", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains("IObjectResolver", StringComparison.Ordinal));
        Assert.IsFalse(normalized.Contains("resultAggregateGroup", StringComparison.Ordinal));
    }

    private static void AssertChainedApplyStreamsWithoutFirstTransition(string actual)
    {
        var normalized = NormalizeExecutionPlanText(actual);

        Assert.IsFalse(
            normalized.Contains("CreateTable [apply_0_i_nTable: apply_0_i_nRow0]", StringComparison.Ordinal),
            normalized);
        Assert.IsFalse(
            normalized.Contains("ForEach [apply_0_i_n in apply_0_i_nTable.Rows]", StringComparison.Ordinal),
            normalized);
        Assert.Contains("EnumerableSource [i.Numbers -> nRows]", normalized);
        Assert.Contains("ForEach [n in nRows]", normalized);
        Assert.Contains("EnumerableSource [i.Numbers -> mRows]", normalized);
        Assert.Contains("ForEach [m in mRows]", normalized);
    }

    private static void AssertChainedApplyDoesNotMaterializeFinalApplyTable(string actual)
    {
        var normalized = NormalizeExecutionPlanText(actual);

        Assert.IsFalse(
            normalized.Contains("CreateTable [apply_0_i_n_mTable: apply_0_i_n_mRow0]", StringComparison.Ordinal),
            normalized);
        Assert.IsFalse(
            normalized.Contains("ForEach [apply_0_i_n_m in apply_0_i_n_mTable.Rows]", StringComparison.Ordinal),
            normalized);
        Assert.IsFalse(
            normalized.Contains("Materialize [apply_0_i_n_mTable.Rows ->", StringComparison.Ordinal),
            normalized);
    }

    private static void AssertGeneratedCSharpContains(string expected, string actual)
    {
        Assert.Contains(expected, NormalizeGeneratedCSharpCode(actual));
    }

    private static string NormalizeExecutionPlanText(string text)
    {
        var normalized = Regex.Replace(text, @"\b(?:statement|cte)\d+_", string.Empty);
        normalized = Regex.Replace(normalized, @"\b[A-Za-z0-9_]+Table_([a-z][A-Za-z0-9]*Rows)\b", "$1");

        return NormalizeScopedGeneratedNames(normalized);
    }

    private static string NormalizeGeneratedCSharpCode(string text)
    {
        return NormalizeScopedGeneratedNames(
            Regex.Replace(text, @"\b(?:statement|cte)\d+_", string.Empty));
    }

    private static string NormalizeScopedGeneratedNames(string text)
    {
        return Regex.Replace(
            text,
            @"\b(?:statement|cte)\d+([A-Z][A-Za-z0-9_]*)",
            static match =>
            {
                var name = match.Groups[1].Value;
                return char.ToLowerInvariant(name[0]) + name[1..];
            });
    }
}
