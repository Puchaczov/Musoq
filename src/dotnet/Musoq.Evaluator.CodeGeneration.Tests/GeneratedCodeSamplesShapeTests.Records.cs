using System;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private sealed record ShapeBudget
    {
        public int GetColumnValue { get; init; }

        public int ConvertTableToSource { get; init; }

        public int ConvertTableToSourceWithDiscardedContexts { get; init; }

        public int TableRowSource { get; init; }

        public int ObjectResolver { get; init; }

        public int SmartForEach { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public int ContextsAccess { get; init; }

        public int DynamicDictionaryRead { get; init; }
    }

    private sealed record ShapeBudgetEntry(string Pattern, Func<ShapeBudget, int> GetCount);

    private sealed record ShapeBudgetCheck(string Pattern, int Budget, int Actual);

    private sealed record SourceScanShapeExpectation(string FileName, string Alias, string DirectUsePattern = "");

    private sealed record GeneratedCodeSampleFile(string FileName, string Category, string Content);

    private sealed record GeneratedMethod(string Name, string Body);

    private sealed record GeneratedRowCastOccurrence(
        string FileName,
        string Line,
        GeneratedRowCastCategory Category);

    private sealed record RetiredGeneratedCodePattern(string Pattern, int Budget);

    private enum GeneratedRowCastCategory
    {
        Uncategorized,
        WindowSourceRows,
        CteRows,
        ResultSortRows,
        AsOfCandidates,
        InterpretationApplyRows,
        DynamicFallback
    }
}
