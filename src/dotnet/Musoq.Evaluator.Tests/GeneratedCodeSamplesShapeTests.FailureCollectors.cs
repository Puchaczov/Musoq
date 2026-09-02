using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static void AssertTableApplyCompiledForInspection(string fileName)
    {
        var result = CompileSampleForInspection(fileName);
        var failures = GetTableApplyShapeFailures(fileName, result.ExecutionPlanText, result.GeneratedCSharpCode);
        Assert.IsEmpty(failures, $"{fileName} has stale table-apply shape: {string.Join(", ", failures)}");
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    private static void AssertCheckedInTableApplySample(string fileName)
    {
        var sample = ReadSample(fileName);
        var failures = GetTableApplyShapeFailures(sample.FileName, sample.Content, sample.Content);
        Assert.IsEmpty(failures, $"{fileName} has stale table-apply shape: {string.Join(", ", failures)}");
    }

    private static string[] GetTableApplyShapeFailures(string fileName, string planText, string generatedCode)
    {
        var failures = new List<string>();
        if (planText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal))
            failures.Add($"{fileName}: execution IR fallback was used");

        if (planText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still stores APPLY output in statement0");

        if (generatedCode.Contains("_tableResults[0]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still reads or writes _tableResults[0]");

        if (generatedCode.Contains("statement0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still declares statement0");

        if (generatedCode.Contains("Statement0Row0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still emits statement transition row");

        if (!generatedCode.Contains("var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>", StringComparison.Ordinal) ||
            !generatedCode.Contains("var bRows = ", StringComparison.Ordinal) || !generatedCode.Contains("bRowsSource.Chunks", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing scoped right-side generic source rows");

        var bRowsIndex = generatedCode.IndexOf("var bRows = ", StringComparison.Ordinal);
        var leftLoopIndex = generatedCode.IndexOf("foreach (var aChunk in aRows)", StringComparison.Ordinal);
        var rightLoopIndex = generatedCode.IndexOf("foreach (var bChunk in bRows)", StringComparison.Ordinal);
        if (bRowsIndex < 0 || leftLoopIndex < 0 || rightLoopIndex < 0 || bRowsIndex < leftLoopIndex || bRowsIndex > rightLoopIndex)
            failures.Add($"{fileName}: right-side scan is not scoped inside the left APPLY loop before right iteration");

        if (!Regex.IsMatch(generatedCode, @"foreach \(var bChunk in [A-Za-z0-9_]*bRows\)"))
            failures.Add($"{fileName}: missing direct right typed-row loop");

        if (fileName == OuterApplySampleFileName)
        {
            if (!generatedCode.Contains("bool bHasMatch = false;", StringComparison.Ordinal))
                failures.Add($"{fileName}: missing outer-apply match tracker");

            if (!generatedCode.Contains("bHasMatch = true;", StringComparison.Ordinal))
                failures.Add($"{fileName}: missing outer-apply matched-row marker");

            if (!Regex.IsMatch(generatedCode, @"new ResultShape0\([A-Za-z_][A-Za-z0-9_]*,\s*null\)", RegexOptions.CultureInvariant))
                failures.Add($"{fileName}: missing outer-apply null extension");
        }

        return failures.ToArray();
    }

    private static string[] GetDirectInterpretationProjectionShapeFailures(
        string fileName,
        string planText,
        string generatedCode)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, generatedCode).ToList();

        if (planText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal))
            failures.Add($"{fileName}: execution IR fallback was used");

        if (!planText.Contains("PhaseBoundary [Begin:cte0]", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing related CTE phase marker for fused interpretation statement");

        if (!planText.Contains("InterpretSource [", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing direct interpretation source");

        if (planText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still stores direct interpretation statement0");

        if (generatedCode.Contains("private Table[] _tableResults", StringComparison.Ordinal))
            failures.Add($"{fileName}: still allocates table-results storage for direct interpretation");

        if (generatedCode.Contains("_tableResults[0]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still reads or writes _tableResults[0]");

        if (generatedCode.Contains("Statement0Row0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still emits direct interpretation transition row");

        if (generatedCode.Contains("BuildCte0(", StringComparison.Ordinal))
            failures.Add($"{fileName}: still emits direct interpretation helper table builder");

        return failures.ToArray();
    }

    private static string[] GetNestedInterpretationExpansionShapeFailures(
        string fileName,
        string planText,
        string generatedCode)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, generatedCode).ToList();

        if (planText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal))
            failures.Add($"{fileName}: execution IR fallback was used");

        if (!planText.Contains("StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing first interpretation materialization boundary");

        if (!planText.Contains("PhaseBoundary [Begin:cte1]", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing related CTE phase marker for fused expansion statement");

        if (planText.Contains("StoreTable [statement1 -> _tableResults[1]]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still stores nested expansion statement1");

        if (generatedCode.Contains("_tableResults[1]", StringComparison.Ordinal))
            failures.Add($"{fileName}: still reads or writes _tableResults[1]");

        if (generatedCode.Contains("Statement1Row0", StringComparison.Ordinal))
            failures.Add($"{fileName}: still emits nested expansion transition row");

        if (generatedCode.Contains("BuildCte1(", StringComparison.Ordinal))
            failures.Add($"{fileName}: still emits nested expansion helper table builder");

        return failures.ToArray();
    }

    private static string[] GetAccessMethodApplyShapeFailures(string fileName, string content)
    {
        var failures = new List<string>();

        if (!content.Contains("EvaluationHelper.ConvertEnumerableOutputToChunks<string>", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing direct scalar chunk conversion");

        if (!Regex.IsMatch(content, @"foreach \(var sChunk in [A-Za-z0-9_]*sRows\)") ||
            !content.Contains("var s = sChunk[sIndex];", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing direct typed chunk loop");

        if (content.Contains("EvaluationHelper.ConvertScalarEnumerableToTypedChunks<string>", StringComparison.Ordinal))
            failures.Add($"{fileName}: scalar enumerable still allocates PrimitiveTypeEntity wrappers");

        if (content.Contains("PrimitiveTypeEntity<string>", StringComparison.Ordinal))
            failures.Add($"{fileName}: scalar enumerable still exposes wrapper entities");

        if (content.Contains("Statement0Row0(i.Name, s.Value", StringComparison.Ordinal))
            failures.Add($"{fileName}: scalar apply projection still reads wrapper Value");

        if (content.Contains("(string[])(string[])", StringComparison.Ordinal))
            failures.Add($"{fileName}: access-method source still has duplicate enumerable casts");

        if (content.Contains("sRows.Rows", StringComparison.Ordinal))
            failures.Add($"{fileName}: access-method rows still iterate through RowSource.Rows");

        if (content.Contains("EvaluationHelper.ConvertEnumerableToTypedChunks(", StringComparison.Ordinal))
            failures.Add($"{fileName}: access-method rows still use typed entity chunk conversion for scalar rows");

        return failures.ToArray();
    }

    private static string[] GetOuterAccessMethodApplyShapeFailures(string content)
    {
        var failures = new List<string>();

        if (!content.Contains("bool sHasMatch = false;", StringComparison.Ordinal))
            failures.Add("missing access-method match tracker");

        if (!content.Contains("sHasMatch = true;", StringComparison.Ordinal))
            failures.Add("missing matched-row marker");

        if (!content.Contains("if ((!sHasMatch))", StringComparison.Ordinal))
            failures.Add("missing unmatched-row branch");

        if (!Regex.IsMatch(content, @"new ResultShape0\([^,\r\n]+,\s*null\)", RegexOptions.CultureInvariant))
            failures.Add("missing null-extended right-side projection");

        if (content.Contains("Statement0Row0", StringComparison.Ordinal))
            failures.Add("outer access-method apply still emits statement transition row");

        if (content.Contains("_tableResults[0]", StringComparison.Ordinal))
            failures.Add("outer access-method apply still reads or writes _tableResults[0]");

        return failures.ToArray();
    }

    private static string[] GetGroupedSetOperationShapeFailures(string fileName, string content)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, content).ToList();

        if (!content.Contains(", HashSet]", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing hash-set set-operation strategy");

        if (!content.Contains("new HashSet<string>(", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing typed string set keys");

        if (content.Contains(".AddUnchecked(", StringComparison.Ordinal))
            failures.Add($"{fileName}: still routes generated set output through pending-row append");

        if (!content.Contains("__musoqFinalShapeRows.Add(new", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing final shape set output append");

        if (content.Contains("Union(", StringComparison.Ordinal) ||
            content.Contains("Except(", StringComparison.Ordinal) ||
            content.Contains("Intersect(", StringComparison.Ordinal) ||
            content.Contains("UnionAll(", StringComparison.Ordinal))
        {
            failures.Add($"{fileName}: still uses helper set-operation call");
        }

        return failures.ToArray();
    }

    private static string[] GetSimpleOuterHashJoinShapeFailures(string fileName, string content)
    {
        var failures = GetRetiredHelperShapeFailures(fileName, content).ToList();

        if (!content.Contains("TryGetValue", StringComparison.Ordinal))
            failures.Add($"{fileName}: missing hash probe TryGetValue");

        if (!Regex.IsMatch(content, @"bool \w+HashHasMatch = false;"))
            failures.Add($"{fileName}: missing hash-join match tracker");

        if (!Regex.IsMatch(content, @"\w+HashHasMatch = true;"))
            failures.Add($"{fileName}: missing hash-join matched-row marker");

        if (!Regex.IsMatch(content, @"if \(!\w+HashHasMatch\)"))
            failures.Add($"{fileName}: missing null-extended no-match branch");

        return failures.ToArray();
    }

    private static string[] GetAsOfJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(AsOfJoinSampleFileName, content).ToList();

        if (!content.Contains("EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal>", StringComparison.Ordinal))
            failures.Add($"{AsOfJoinSampleFileName}: missing typed ASOF index");

        if (content.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal))
            failures.Add($"{AsOfJoinSampleFileName}: ASOF probe still uses per-row full scan matcher");

        if (!ContainsDirectChunkedRowLoop(content, "a", "aRows"))
            failures.Add($"{AsOfJoinSampleFileName}: missing direct left typed-row loop");

        if (content.Contains("bRows.Rows", StringComparison.Ordinal))
            failures.Add($"{AsOfJoinSampleFileName}: ASOF probe still scans resolver-backed right rows");

        return failures.ToArray();
    }

    private static string[] GetAsOfTieBreakShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(AsOfTieBreakSampleFileName, content).ToList();

        if (!content.Contains(
                "EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal, decimal>",
                StringComparison.Ordinal))
            failures.Add($"{AsOfTieBreakSampleFileName}: missing typed ASOF tie-break index");

        if (!content.Contains("Musoq.Evaluator.IR.Bindings.NullOrdering.Last", StringComparison.Ordinal))
            failures.Add($"{AsOfTieBreakSampleFileName}: missing explicit tie-break null ordering");

        if (content.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal))
            failures.Add($"{AsOfTieBreakSampleFileName}: ASOF tie-break probe still uses per-row full scan matcher");

        if (!ContainsDirectChunkedRowLoop(content, "a", "aRows"))
            failures.Add($"{AsOfTieBreakSampleFileName}: missing direct left typed-row loop");

        if (content.Contains("bRows.Rows", StringComparison.Ordinal))
            failures.Add($"{AsOfTieBreakSampleFileName}: ASOF tie-break probe still scans resolver-backed right rows");

        return failures.ToArray();
    }

    private static string[] GetCteBackedAsOfJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(CteBackedAsOfJoinSampleFileName, content).ToList();

        if (!content.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, decimal>", StringComparison.Ordinal))
            failures.Add($"{CteBackedAsOfJoinSampleFileName}: missing typed ASOF index");

        if (content.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal))
            failures.Add($"{CteBackedAsOfJoinSampleFileName}: ASOF probe still uses per-row full scan matcher");

        if (!content.Contains("_cteRowResults.Slot0", StringComparison.Ordinal))
            failures.Add($"{CteBackedAsOfJoinSampleFileName}: missing typed table-backed ASOF probe rows");

        if (!ContainsDirectChunkedRowLoop(content, "a", "aRows"))
            failures.Add($"{CteBackedAsOfJoinSampleFileName}: missing direct left typed-row loop");

        return failures.ToArray();
    }

    private static bool ContainsDirectChunkedRowLoop(string content, string rowVariable, string rowsVariable)
    {
        var escapedRowVariable = Regex.Escape(rowVariable);
        var escapedRowsVariable = Regex.Escape(rowsVariable);

        if (!Regex.IsMatch(
            content,
            $@"foreach \(var {escapedRowVariable}Chunk in [A-Za-z0-9_]*{escapedRowsVariable}\)\s+\{{",
            RegexOptions.Singleline))
        {
            return false;
        }
        string[] rowLoadPatterns =
        [
            $@"var {escapedRowVariable} = {escapedRowVariable}ChunkViewArray\[{escapedRowVariable}ChunkViewOffset \+ {escapedRowVariable}Index\];",
            $@"var {escapedRowVariable} = {escapedRowVariable}ChunkViewList\[{escapedRowVariable}ChunkViewOffset \+ {escapedRowVariable}Index\];",
            $@"var {escapedRowVariable} = {escapedRowVariable}Chunk\[{escapedRowVariable}Index\];"
        ];

        return rowLoadPatterns.Any(pattern => Regex.IsMatch(content, pattern, RegexOptions.Singleline));
    }

    private static string[] GetDynamicCteBackedAsOfJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(DynamicCteBackedAsOfJoinSampleFileName, content).ToList();

        if (!content.Contains("private sealed class dDynamicRow0", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing right-side dynamic adapter row");

        if (!content.Contains("private sealed class lDynamicRow0", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing left-side dynamic adapter row");

        if (!content.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, int>", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing typed CTE ASOF index");

        if (content.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: ASOF probe still uses per-row full scan matcher");

        if (!content.Contains("_cteRowResults.Slot0", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing typed CTE ASOF probe rows");

        if (content.Contains("_tableResults[0]", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: still uses table-backed CTE storage");

        if (content.Contains("new Table(\"cte0\"", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: still builds the dynamic CTE as a Table");

        if (!content.Contains("new dDynamicRow0(dResolver.TryGetValue(\"Team\", out var __dynamicValue", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing right-side dynamic boundary adapter");

        if (!content.Contains("new lDynamicRow0(lResolver.TryGetValue(\"Team\", out var __dynamicValue", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing left-side dynamic boundary adapter");

        return failures.ToArray();
    }

}
