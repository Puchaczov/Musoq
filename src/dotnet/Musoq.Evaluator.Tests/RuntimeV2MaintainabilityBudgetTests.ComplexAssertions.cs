using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private static readonly string[] StrictComplexResultFiles =
    [
        "src/dotnet/Musoq.Evaluator.Tests/ApplyOrdinalityTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplySelfPropertyTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OuterApplySelfPropertyTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyCteTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OuterApplyMethodCallTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyMethodCallTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OuterApplyCteTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/AsOfJoinTests.Compositions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/AsOfJoinTests.PostOperations.cs",
        "src/dotnet/Musoq.Evaluator.Tests/AsOfJoinTests.PostOperations.Compositions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossFeature.MultiSourceTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ComprehensiveJoinTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CteTests.SetOperators.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CteTests.OuterAndMultiple.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.CteSetOperators.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OuterApplyTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OuterApplyMixedTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/FeatureCombinationTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.JoinsAndNested.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.WindowRanking.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.WindowRanking.Compositions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.BasicTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.FrameTests.ImplicitDefaults.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.FrameTests.QueryComposition.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.FrameTests.Validation.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.MultipleWindowTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.ValueAccessTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SetsOperatorsTests.OptionalKeys.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ValuesFromTests.JoinsWindowsAndSets.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ThirdRoundWindowResultTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ThirdRoundSetCteResultTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ThirdRoundApplyResultTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ThirdRoundJoinAsOfValuesResultTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GeneratedCodeSamplesShapeTests.ApplyOrdinality.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GeneratedCodeSamplesShapeTests.ApplyTransitions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GeneratedCodeSamplesExecutionTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SetsOperatorsTests.ExceptIntersect.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SetsOperatorsTests.MixedUnionScenarios.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SetsOperatorsTests.Union.cs",
        "src/dotnet/Musoq.Evaluator.Tests/NullInSetOperatorsTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SetsOperatorsTests.MixedAndGroupedSources.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.IntegrationTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.FrameTests.Rows.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.FrameTests.RangeBetween.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.RowsSemanticsTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/WindowFunction.EdgeCaseTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.ComplexPatternsTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.QueriesAndJoinsTests.AggregatesAndGrouping.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Join.FullOuterJoinTests.Compositions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/UnpivotCompositionTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ComplexResultCompositionTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.ExpressionsAndAggregates.cs",
        "src/dotnet/Musoq.Evaluator.Tests/QualifyTests.CtesJoinsAndOrdering.cs",
        "src/dotnet/Musoq.Evaluator.Tests/AsOfJoinTests.Directional.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CteTests.BasicAndGrouping.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CteTests.MixedGrouping.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CteTests.ParallelAndOrdering.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.CteBasics.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GroupByTests.DistinctAggregates.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyMultiSchemaTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.RealWorldAndFeatureTests.LinesParse.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OrderByTests.CteCaseAndJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyMixedTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyUnusedAliasTests.CteApplyJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyUnusedAliasTests.InterleavedApplyJoinChains.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyUnusedAliasTests.JoinChains.cs",
        "src/dotnet/Musoq.Evaluator.Tests/MultiJoinHashJoinTests.CtesAndGeneratedCode.cs",
        "src/dotnet/Musoq.Evaluator.Tests/MultiJoinHashJoinTests.InnerJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/MultiJoinHashJoinTests.MixedAndEdgeCases.cs",
        "src/dotnet/Musoq.Evaluator.Tests/MultiJoinHashJoinTests.ResultContracts.cs",
        "src/dotnet/Musoq.Evaluator.Tests/HashOptionalSchemaComprehensiveTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/HashOptionalSchemaComprehensiveTests.GroupingPagingAndJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/HashOptionalSchemaComprehensiveTests.SetsCtesAndCases.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyUnusedAliasTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/CrossApplyUnusedAliasTests.SameSourcePropertyApplies.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.FilteringAndOrdering.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.MethodAndPropertyAccess.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.CtesDistinctAndParserLimits.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.EmptyAndAliasEdges.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.SetAndSelfPatterns.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.CrossApplyBasicsTests.GroupingJoinsAndOuterApply.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.QueriesAndJoinsTests.CrossApplyAdvanced.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.QueriesAndJoinsTests.CrossApplyCore.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.QueriesAndJoinsTests.CtesOrderingAndEdges.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.QueriesAndJoinsTests.Joins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.ComplexPatternsTests.ExpressionsAndFilters.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.ComplexPatternsTests.GroupingAndOrdering.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Exploratory.ComplexPatternsTests.ScalarFunctions.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.CteChains.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.Ctes.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.JoinsAndAggregates.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctComprehensiveTests.SetOperators.cs",
        "src/dotnet/Musoq.Evaluator.Tests/EndToEndQueryExecutionTests.JoinsAndSets.cs",
        "src/dotnet/Musoq.Evaluator.Tests/EndToEndQueryExecutionTests.PagingAndMultitable.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ReorderedSyntaxCteTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ReorderedSyntaxCteTests.EdgeCases.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ReorderedSyntaxCteTests.MixedAndNested.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ReorderedSyntaxCteTests.SetOperatorsAndJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GroupByTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GroupByTests.CountsAndUngroupedAggregates.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OrderByTests.BasicAndGrouped.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OrderByTests.AliasProjection.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctOrderByBugTests.FunctionsAndSimpleDistinct.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctOrderByBugTests.CtesAndGroupBy.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Join.IntegrationTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.CoreLanguageTests.DirectJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctOrderByBugTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/DistinctOrderByBugTests.CteOrderingAndStress.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Join.InnerJoinTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Join.OuterJoinTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GroupByTests.AccessorsAndJoins.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OrderByTests.DescendingEdgeCases.cs",
        "src/dotnet/Musoq.Evaluator.Tests/OrderByTests.FunctionAliases.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.CoreLanguageTests.AdvancedClauses.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.CoreLanguageTests.JoinsAndAggregates.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.TableCoupleTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.TableCoupleTests.AllTypes.cs",
        "src/dotnet/Musoq.Evaluator.Tests/GroupByAllTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/ImplicitBooleanConversionTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/SemanticLogicalTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/PredicateSyntaxResultContractsTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/Desc.QueryTests.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.AdvancedText.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.EdgeAndText.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.RealWorldAndFeatureTests.TextFormats.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.ComplexQueries.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.SchemaFeaturesTests.SchemaComposition.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.TypesAndExpressionsTests.ExpressionsAndCtes.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.BasicQueries.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.ComputedFields.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.MixedComposition.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.NestedSchemas.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.SchemaFeaturesTests.AggregationAndGrouping.cs",
        "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.RealWorldBinary.cs"
    ];

    [TestMethod]
    public void StrictComplexResultFamilies_ShouldNotRegressToPartialAssertions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var forbiddenPatterns = new[]
        {
            "table.Count",
            "table.Single(",
            "table.Any(",
            "table.All(",
            "table.First(",
            "table.FirstOrDefault(",
            "table.ElementAt(",
            "table.Select(",
            "table.Rows.Select(",
            "table.Rows.Where(",
            "table.Rows.Any(",
            "table.Rows.All(",
            "table.Rows.First(",
            "table.Rows.FirstOrDefault(",
            "table.Rows.ElementAt(",
            "table.Rows[",
            "table[",
            "Assert.IsNotNull(table)",
            "Convert.",
            "CollectionAssert"
        };
        var forbiddenRegexes = new[]
        {
            new Regex(@"(?s)\btable\s*(?:\r?\n\s*)?\.\s*(?:Select|Where|Any|All|First|FirstOrDefault|ElementAt)\b", RegexOptions.Compiled),
            new Regex(@"(?m)^\s*Assert\.(?:Contains|DoesNotContain)\s*\([^\r\n;]*(?:\btable\b|\brows\b|\bnames\b|\bvalues\b)", RegexOptions.Compiled)
        };

        var offenders = StrictComplexResultFiles
            .Select(relativePath => (relativePath, text: File.ReadAllText(ToAbsolutePath(repositoryRoot, relativePath))))
            .SelectMany(item =>
            {
                var issues = new List<string>();

                if (!item.text.Contains("TableMaterializationTestHelper.AssertColumns", StringComparison.Ordinal))
                    issues.Add("missing exact column assertion");

                if (!item.text.Contains("TableMaterializationTestHelper.AssertRows", StringComparison.Ordinal))
                    issues.Add("missing exact row assertion");

                issues.AddRange(forbiddenPatterns
                    .Where(pattern => item.text.Contains(pattern, StringComparison.Ordinal))
                    .Select(pattern => $"contains '{pattern}'"));

                issues.AddRange(forbiddenRegexes
                    .Where(regex => regex.IsMatch(item.text))
                    .Select(regex => $"matches partial-result pattern '{regex}'"));

                return issues.Select(issue => $"{item.relativePath}: {issue}");
            })
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Strict complex-query result families must keep complete schema and row assertions: " +
            string.Join(", ", offenders));
    }
}
