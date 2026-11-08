using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void QueryRowSamples_WhenCompiledForInspection_ShouldPreserveSelectedCarrierAndMaterializerShape()
    {
        var declared = CompileSampleForInspection(QueryRowDeclaredEntitySampleFileName);
        var narrow = CompileSampleForInspection(QueryRowReadonlyStructSampleFileName);
        var wide = CompileSampleForInspection(QueryRowSealedClassSampleFileName);
        var empty = CompileSampleForInspection(QueryRowZeroFieldSampleFileName);
        var special = CompileSampleForInspection(QueryRowSpecialNamesSampleFileName);
        var boundary = CompileSampleForInspection(QueryRowLifetimeBoundarySampleFileName);

        foreach (var result in new[] { declared, narrow, wide, empty, special, boundary })
        {
            Assert.IsFalse(
                result.Diagnostics.Any(static diagnostic => diagnostic.IsError),
                string.Join(Environment.NewLine, result.Diagnostics));
        }

        StringAssert.Contains(declared.GeneratedCSharpCode, "SourceInvocation<Musoq.Evaluator.Tests.Schema.QueryRows.DeclaredQueryRowSample>");
        Assert.IsFalse(declared.GeneratedCSharpCode.Contains("QuerySourceInvocation<", StringComparison.Ordinal));

        StringAssert.Contains(narrow.GeneratedCSharpCode, "private readonly struct QueryRow_");
        StringAssert.Contains(narrow.GeneratedCSharpCode, "IQueryRowMaterializer<QueryRow_");
        StringAssert.Contains(narrow.GeneratedCSharpCode, "private static readonly QueryRowShape __queryRowShape_");
        Assert.AreEqual(1, CountOccurrences(narrow.GeneratedCSharpCode, "new QueryRowShape"));
        StringAssert.Contains(narrow.GeneratedCSharpCode, "reader.Read<int>(0)");
        StringAssert.Contains(narrow.GeneratedCSharpCode, "reader.Read<string>(1)");
        StringAssert.Contains(narrow.PlanningText, "lifetime=ScanLocal");

        StringAssert.Contains(wide.GeneratedCSharpCode, "private sealed class QueryRow_");
        Assert.AreEqual(5, CountOccurrences(wide.GeneratedCSharpCode, "reader.Read<Guid>("));
        StringAssert.Contains(wide.PlanningText, "sealed class carrier");

        StringAssert.Contains(empty.GeneratedCSharpCode, "private readonly struct QueryRow_");
        StringAssert.Contains(empty.GeneratedCSharpCode, "QuerySourceInvocation<");
        Assert.IsFalse(empty.GeneratedCSharpCode.Contains("reader.Read<", StringComparison.Ordinal));

        StringAssert.Contains(special.GeneratedCSharpCode, "\"display name\"");
        StringAssert.Contains(special.GeneratedCSharpCode, "\"na-me\"");
        StringAssert.Contains(special.GeneratedCSharpCode, "\"MiastoŁódź\"");
        StringAssert.Contains(special.GeneratedCSharpCode, "\"select\"");
        StringAssert.Contains(special.GeneratedCSharpCode, "reader.Read<int>(1)");

        StringAssert.Contains(boundary.GeneratedCSharpCode, "private sealed class QueryRow_");
        Assert.AreEqual(2, CountOccurrences(boundary.GeneratedCSharpCode, "QuerySourceInvocation<"));
        StringAssert.Contains(boundary.PlanningText, "lifetime=EscapesScan");
    }

    [TestMethod]
    public void QueryRowSamples_WhenExecuted_ShouldProduceRowsForEveryGeneratedPath()
    {
        (string FileName, int Count)[] expectations =
        [
            (QueryRowDeclaredEntitySampleFileName, 2),
            (QueryRowReadonlyStructSampleFileName, 2),
            (QueryRowSealedClassSampleFileName, 1),
            (QueryRowZeroFieldSampleFileName, 1),
            (QueryRowSpecialNamesSampleFileName, 1),
            (QueryRowLifetimeBoundarySampleFileName, 2)
        ];

        foreach (var expectation in expectations)
        {
            using var query = CompileSampleForExecution(expectation.FileName);
            using var table = query.Run();
            Assert.AreEqual(expectation.Count, table.Count, expectation.FileName);
        }
    }

    [TestMethod]
    public void QueryRowSamples_WhenGenerated_ShouldRemainTrackedAsDedicatedSnapshotCorpus()
    {
        var samples = ReadNamedSamples(
            QueryRowDeclaredEntitySampleFileName,
            QueryRowReadonlyStructSampleFileName,
            QueryRowSealedClassSampleFileName,
            QueryRowZeroFieldSampleFileName,
            QueryRowSpecialNamesSampleFileName,
            QueryRowLifetimeBoundarySampleFileName);

        Assert.IsTrue(samples.All(static sample => sample.Category == "QueryScopedRows"));
        Assert.IsTrue(samples.All(static sample => sample.Content.Contains(
            "// === Generated C# ===",
            StringComparison.Ordinal)));
    }
}
