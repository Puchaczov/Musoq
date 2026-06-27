using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RuntimeV2CastGroupingFeatureSamples_WhenCheckedIn_ShouldExistAsContiguousBlock()
    {
        var featureFiles = ReadSamples()
            .Where(static sample => sample.Category == "RuntimeV2CastGrouping")
            .Select(static sample => sample.FileName)
            .OrderBy(static fileName => fileName)
            .ToArray();

        CollectionAssert.AreEqual(RuntimeV2CastGroupingFeatureSampleFileNames, featureFiles);
    }

    [TestMethod]
    public void RuntimeV2CastGroupingFeatureSamples_WhenCheckedIn_ShouldAvoidReflectionAndGenericCastFallbacks()
    {
        var samples = ReadRuntimeV2CastGroupingFeatureSamples();
        var forbiddenPatterns = new[]
        {
            "StrictCastRuntime",
            "MethodInfo",
            "GetMethod",
            "Convert.ChangeType",
            "FieldLink",
            "SourceScan [ko3iko: object]",
            "GetRowSource<object>",
            "EvaluationHelper.GetNestedValueAccessor",
            "EvaluationHelper.GetNestedValue(",
            "int.Parse(",
            "decimal.Parse(",
            "Guid.Parse(",
            "System.Convert.To",
            "(object)(ko3iko.Population == null",
            "(object)(ko3iko.Amount == null",
            "__agg0Input = (object)"
        };

        foreach (var sample in samples.Values)
        {
            AssertNoLibraryBaseAllocationInsideForeach(sample);
            foreach (var pattern in forbiddenPatterns)
            {
                Assert.IsFalse(
                    sample.Contains(pattern, StringComparison.Ordinal),
                    $"{pattern} leaked into {sample[..Math.Min(sample.Length, 80)]}");
            }
        }
    }

    [TestMethod]
    public void RuntimeV2CastSamples_WhenCheckedIn_ShouldRenderLibraryBackedCasts()
    {
        var samples = ReadRuntimeV2CastGroupingFeatureSamples();
        var projection = samples[RuntimeV2CastProjectionSampleFileName];
        var expressions = samples[RuntimeV2CastExpressionsSampleFileName];
        var aggregate = samples[RuntimeV2CastAggregateGroupingSampleFileName];

        Assert.Contains("SELECT Population::Int32 as PopulationInt", projection);
        Assert.Contains("SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows", projection);
        Assert.Contains("GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>", projection);
        Assert.Contains("CreateObject [__resultLibraryBase0: LibraryBase]", projection);
        Assert.Contains("__resultLibraryBase0.ToInt32(ko3iko.Population)", projection);
        Assert.Contains("__resultLibraryBase0.ToDecimal(ko3iko.Amount)", projection);
        Assert.Contains("__resultLibraryBase0.ToGuid(ko3iko.Id)", projection);

        Assert.Contains("SELECT (Quantity + 1)::Int64 as QuantityNext", expressions);
        Assert.Contains("CreateObject [__resultOrderRecordsLibraryBase0: LibraryBase]", expressions);
        Assert.Contains("__resultOrderRecordsLibraryBase0.ToInt64((ko3iko.Quantity + 1))", expressions);
        Assert.Contains("Let [populationInt32: int? = population::Int32]", expressions);
        Assert.Contains("int? populationInt32 = __resultOrderRecordsLibraryBase0.ToInt32(population);", expressions);
        Assert.Contains("__resultOrderRecordsLibraryBase0.ToString(populationInt32)", expressions);
        Assert.DoesNotContain("__resultOrderRecordsLibraryBase0.ToString(__resultOrderRecordsLibraryBase0.ToInt32(", expressions);
        Assert.AreEqual(3, CountOccurrences(expressions, "__resultOrderRecordsLibraryBase0.ToInt32("));
        Assert.Contains("__resultOrderRecordsLibraryBase0.ToDateTimeOffset(ko3iko.CreatedAt)", expressions);
        Assert.Contains("__resultOrderRecordsLibraryBase0.ToDecimal(ko3iko.Amount)", expressions);

        Assert.Contains("Sum(Amount::Decimal) as TotalAmount", aggregate);
        Assert.Contains("CreateAggregateLibrary [libraryBase0: LibraryBase]", aggregate);
        Assert.Contains("Let [amountDecimal: decimal? = amount::Decimal]", aggregate);
        Assert.Contains("TypedAggregateSet [Set(group.__agg0, amountDecimal)]", aggregate);
        Assert.Contains("decimal? amountDecimal = libraryBase0.ToDecimal(amount);", aggregate);
        Assert.Contains("var __agg0Input = (decimal?)amountDecimal;", aggregate);
        Assert.AreEqual(1, CountOccurrences(aggregate, "libraryBase0.ToDecimal(amount)"));
        Assert.Contains("libraryBase0.ToDecimal(\"10.00\")", aggregate);
    }

    [TestMethod]
    public void RuntimeV2GroupingSamples_WhenCheckedIn_ShouldNormalizeOrdinalsAliasesAndAllBeforeCodegen()
    {
        var samples = ReadRuntimeV2CastGroupingFeatureSamples();
        var ordinal = samples[RuntimeV2GroupByOrdinalSampleFileName];
        var groupByAll = samples[RuntimeV2GroupByAllCastsSampleFileName];
        var alias = samples[RuntimeV2AliasWhereGroupBySampleFileName];
        var havingAlias = samples[RuntimeV2HavingAggregateAliasSampleFileName];
        var conflict = samples[RuntimeV2AliasSourceConflictSampleFileName];
        var combined = samples[RuntimeV2CombinedGroupingSampleFileName];

        Assert.Contains("GROUP BY 1, 2", ordinal);
        Assert.Contains("Aggregate [keys: ko3iko.City, ko3iko.Department] [aggs: Count(*)]", ordinal);
        Assert.Contains("CreateValueTupleAggregateContext [groups: (string, string) -> ResultAggregateGroup]", ordinal);
        Assert.Contains("groups[(ko3iko.City, ko3iko.Department)]", ordinal);

        Assert.Contains("GROUP BY ALL", groupByAll);
        Assert.Contains("Aggregate [keys: ko3iko.City, ko3iko.Population::Int32] [aggs: Count(*)]", groupByAll);
        Assert.Contains("CreateValueTupleAggregateContext [groups: (string, int?) -> ResultAggregateGroup]", groupByAll);
        Assert.Contains("int? groupKey1 = __resultLibraryBase0.ToInt32(ko3iko.Population);", groupByAll);

        Assert.Contains("WHERE c <> ''", alias);
        Assert.Contains("Filter [(ko3iko.City <> '')]", alias);
        Assert.Contains("GetOrAddSingleKeyAggregateGroup [group = groups[city] by c; typed: ResultAggregateGroup]", alias);
        Assert.Contains("string city = ko3iko.City;", alias);

        Assert.Contains("HAVING cnt > 1", havingAlias);
        Assert.Contains("Having [(AggRef(ko3iko.Count(*)) > 1)]", havingAlias);
        Assert.Contains("if ((finalGroup.__agg0.Count > 1))", havingAlias);

        Assert.Contains("SELECT City as Department, Department as SourceDepartment", conflict);
        Assert.Contains("GetOrAddValueTupleAggregateGroup [group = groups[(ko3iko.Department, ko3iko.City)] by Department, City; typed: ResultAggregateGroup]", conflict);
        Assert.Contains("string groupKey0 = ko3iko.Department;", conflict);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key1, finalGroup.__key0, finalGroup.__agg0.Count));", conflict);

        Assert.Contains("GROUP BY ALL", combined);
        Assert.Contains("HAVING cnt > 1 AND total > '10.00'::Decimal", combined);
        Assert.Contains("CreateValueTupleAggregateContext [groups: (string, int?) -> ResultAggregateGroup]", combined);
        Assert.Contains("Dictionary<(string, int?), ResultAggregateGroup>", combined);
        Assert.Contains("int? populationInt32 = libraryBase0.ToInt32(population);", combined);
        Assert.Contains("int? groupKey1 = populationInt32;", combined);
        Assert.AreEqual(3, CountOccurrences(combined, "libraryBase0.ToInt32(population)"));
        Assert.Contains("finalGroup.__agg1.Count > 1", combined);
        Assert.Contains("result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__key1, finalGroup.__agg1.Count", combined);
        Assert.Contains("__musoqFinalShapeRows.Add(resultSortedRowsRow);", combined);
    }

    private static System.Collections.Generic.IReadOnlyDictionary<string, string> ReadRuntimeV2CastGroupingFeatureSamples()
    {
        var samples = ReadSamples()
            .Where(static sample => RuntimeV2CastGroupingFeatureSampleFileNames.Contains(sample.FileName, StringComparer.Ordinal))
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        foreach (var fileName in RuntimeV2CastGroupingFeatureSampleFileNames)
            Assert.IsTrue(samples.ContainsKey(fileName), fileName);

        return samples;
    }

    private static void AssertNoLibraryBaseAllocationInsideForeach(string sample)
    {
        var loopDepths = new System.Collections.Generic.Stack<int>();
        var depth = 0;
        foreach (var line in sample.Split('\n'))
        {
            if (line.TrimStart().StartsWith("foreach (", StringComparison.Ordinal))
                loopDepths.Push(depth);
            Assert.IsFalse(loopDepths.Count > 0 && line.Contains("new Musoq.Plugins.LibraryBase()", StringComparison.Ordinal), "LibraryBase allocation leaked inside a foreach loop.");
            depth += line.Count(static character => character == '{') - line.Count(static character => character == '}');
            while (loopDepths.Count > 0 && depth <= loopDepths.Peek())
                loopDepths.Pop();
        }
    }
}
