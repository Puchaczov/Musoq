using System;
using System.Linq;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static string[] GetDynamicCteBackedAsOfJoinShapeFailures(string content)
    {
        var failures = GetRetiredHelperShapeFailures(DynamicCteBackedAsOfJoinSampleFileName, content).ToList();

        if (!content.Contains("private readonly struct QueryRow_", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing native struct source carrier");

        if (!content.Contains("private sealed class QueryRow_", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing native class source carrier");

        if (!content.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, int, string>", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing typed CTE ASOF index");

        if (content.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: ASOF probe still uses per-row full scan matcher");

        if (!content.Contains("_cteRowResults.Slot0", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing typed CTE ASOF probe rows");

        if (content.Contains("_tableResults[0]", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: still uses table-backed CTE storage");

        if (content.Contains("new Table(\"cte0\"", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: still builds the dynamic CTE as a Table");

        if (!content.Contains("QuerySourceInvocation<QueryRow_", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing native source invocation");

        if (!content.Contains("IQueryRowMaterializer<QueryRow_", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: missing native materializer");

        if (content.Contains("__dynamicValue", StringComparison.Ordinal))
            failures.Add($"{DynamicCteBackedAsOfJoinSampleFileName}: erased dictionary decoding returned");
        return failures.ToArray();
    }

}
