using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

internal static partial class EvaluatorSampleFixtures
{
    private static GeneratedCodeSample[] CreateLibraryExecutionCoverageSamples() =>
    [
        CoverageSample("K76_NativeJoinedFramesQualify", LibraryExecutionCoverageQueryTestsFixtures.JoinedFrameQuery),
        CoverageSample("K77_NativeRecursiveFrames", LibraryExecutionCoverageQueryTestsFixtures.RecursiveFrameQuery),
        CoverageSample("K78_NativeSetAggregateWindow", LibraryExecutionCoverageQueryTestsFixtures.SetAggregateWindowQuery),
        CoverageSample("K79_NativeNumericRangeOffsets",
            "select Position, Sum(Value) over (order by Value range between 2 preceding and 1 following) as Total, " +
            "Count(Value) over (order by Value range between 2 preceding and 1 following) as Present " +
            "from #schema.first() order by Position"),
        new GeneratedCodeSample
        {
            Name = "K80_NativeFloatingDecimalComparison", FileName = "K80_NativeFloatingDecimalComparison.cs",
            Query = "select Position, Floating from #schema.first() where Floating > Amount order by Position",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider()
        },
        NativeMembershipSample("K81_NativeDynamicSqlMembership", "in"),
        NativeMembershipSample("K82_NativeDynamicLiteralContains", "contains"),
        new GeneratedCodeSample
        {
            Name = "K83_NativeCaseDistinctCteGrouping", FileName = "K83_NativeCaseDistinctCteGrouping.cs",
            Query = "with data as (select Position, Name, name from #schema.first()) " +
                "select Name, name, Count(Position) as Present from data group by Name, name order by name",
            Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
            CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
            CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(new[]
            {
                new LibraryExecutionProviderQueryTestsFixtures.CaseDistinctPlanningRow(1, "first", 2),
                new LibraryExecutionProviderQueryTestsFixtures.CaseDistinctPlanningRow(2, "second", 1)
            })
        },
        OuterMembershipSample("K84_NativeOuterJoinConstantMembership", LibraryExecutionProviderQueryTestsFixtures.OuterContainsQuery),
        OuterMembershipSample("K85_NativeFilteredNullMembership", LibraryExecutionProviderQueryTestsFixtures.FilteredNotInQuery),
        OuterMembershipSample("K86_NativeAggregateMembershipInput", LibraryExecutionProviderQueryTestsFixtures.AggregateMembershipInputQuery)
    ];

    private static GeneratedCodeSample OuterMembershipSample(string name, string query) => new()
    {
        Name = name, FileName = name + ".cs", Query = query,
        Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
        CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(
            LibraryExecutionProviderQueryTestsFixtures.CreateOuterMembershipRows())
    };

    private static GeneratedCodeSample NativeMembershipSample(string name, string operation) => new()
    {
        Name = name, FileName = name + ".cs",
        Query = $"select Position, Integral {operation} (Position, Position + 1) as Matched from #schema.first() order by Position",
        Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
        CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider()
    };

    private static GeneratedCodeSample CoverageSample(string name, string query) => new()
    {
        Name = name, FileName = name + ".cs", Query = query,
        Category = "LibraryExecution", Format = GeneratedCodeSampleFormat.GeneratedCodeOnly,
        CompilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false),
        CreateSchemaProvider = static () => LibraryExecutionFixture.CreateProvider(LibraryExecutionCoverageQueryTestsFixtures.CreateRows(17))
    };
}
