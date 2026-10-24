namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static GeneratedCodeSample[] CreateSqlConcatenationSamples() =>
    [
        Basic("S01_SqlConcatenationShort", "StringConcatenation",
            "select Name || City as A, Name || City as B from #A.entities()"),
        Basic("S02_SqlConcatenationLong", "StringConcatenation",
            "select Name || City || Name || City || Name || City || Name || City as A from #A.entities()"),
        Basic("S03_SqlConcatenationImplicitNull", "StringConcatenation",
            "select (case when Population > 0 then Name end) || City || Name || City || Name as A from #A.entities()")
    ];
}
