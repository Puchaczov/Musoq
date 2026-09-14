using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public class PredicateQuantifierInspectionTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();
    private readonly SystemSchemaProvider _schemaProvider = new();

    [TestMethod]
    public void CompileForInspection_WhenAnyLikePredicateQuantifierIsUsed_ShouldExposeLoweredOrPatternShape()
    {
        var result = CreateInspection("select d.Dummy from #system.dual() d where any(d.Dummy, 'fallback') like 'single%'");

        Assert.Contains("Filter [(d.Dummy LIKE 'single%' OR 'fallback' LIKE 'single%')]", result.LogicalPlanText);
        Assert.Contains("PhysicalFilter [(d.Dummy LIKE 'single%' OR 'fallback' LIKE 'single%')]", result.PhysicalPlanText);
        Assert.Contains(
            "If [(STRING_MATCH(dummy, pattern='single%', needle='single', kind=Prefix, comparison=LikeIgnoreCase) OR " +
            "STRING_MATCH('fallback', pattern='single%', needle='single', kind=Prefix, comparison=LikeIgnoreCase))]",
            result.ExecutionPlanText);
        AssertNoResidualQuantifier(result);
        Assert.AreEqual(6, CountOccurrences(result.GeneratedCSharpCode, ".StartsWith("));
        Assert.Contains("STRING_MATCH", result.ExecutionPlanText);
    }

    [TestMethod]
    public void CompileForInspection_WhenAllNotRLikePredicateQuantifierIsUsed_ShouldExposeLoweredAndNegatedPatternShape()
    {
        var result = CreateInspection("select d.Dummy from #system.dual() d where all(d.Dummy, 'fallback') not rlike '^blocked'");

        Assert.Contains("Filter [(NOT d.Dummy RLIKE '^blocked' AND NOT 'fallback' RLIKE '^blocked')]", result.LogicalPlanText);
        Assert.Contains("PhysicalFilter [(NOT d.Dummy RLIKE '^blocked' AND NOT 'fallback' RLIKE '^blocked')]", result.PhysicalPlanText);
        Assert.Contains(
            "Let [__rlikeMatcher0: PreparedRLikeMatcher = PREPARE_RLIKE('^blocked', " +
            "strategy=regex, anchors=none, literal-span=none, fallback=RegexMetacharacter)]",
            result.ExecutionPlanText);
        Assert.Contains(
            "If [(NOT PREPARED_RLIKE(dummy, __rlikeMatcher0) AND NOT PREPARED_RLIKE('fallback', __rlikeMatcher0))]",
            result.ExecutionPlanText);
        AssertNoResidualQuantifier(result);
        Assert.AreEqual(1, CountOccurrences(result.GeneratedCSharpCode, "Operators.PrepareRLike("));
        Assert.AreEqual(6, CountOccurrences(result.GeneratedCSharpCode, "Operators.RLikePrepared("));
        Assert.DoesNotContain("new Musoq.Evaluator.Operators().RLike", result.GeneratedCSharpCode);
    }

    private QueryInspectionResult CreateInspection(string query)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            _loggerResolver);
    }

    private static void AssertNoResidualQuantifier(QueryInspectionResult result)
    {
        AssertNoResidualQuantifier(result.LogicalPlanText);
        AssertNoResidualQuantifier(result.PhysicalPlanText);
        AssertNoResidualQuantifier(result.ExecutionPlanText);
        AssertNoResidualQuantifier(result.GeneratedCSharpCode);
    }

    private static void AssertNoResidualQuantifier(string text)
    {
        Assert.IsFalse(text.Contains("any(", StringComparison.OrdinalIgnoreCase), text);
        Assert.IsFalse(text.Contains("all(", StringComparison.OrdinalIgnoreCase), text);
        Assert.IsFalse(text.Contains(".Any(", StringComparison.Ordinal), text);
        Assert.IsFalse(text.Contains(".All(", StringComparison.Ordinal), text);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var startIndex = 0;

        while (true)
        {
            var index = text.IndexOf(value, startIndex, StringComparison.Ordinal);
            if (index < 0)
                return count;

            count++;
            startIndex = index + value.Length;
        }
    }
}
