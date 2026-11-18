using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TypedSelectShapeOutputTests
{
    [TestMethod]
    public void CompileAndRun_WhenConstructorOutputMatchesSelectAliases_ShouldConstructTypedRowsDirectly()
    {
        var rows = new[] { new[] { new TypedInput("Ada", 37), new TypedInput("Linus", 55) } };

        var result = MusoqApi
            .CompileAndRun<TypedInput, ConstructorOutput>(
                "select Name, Age from #A.entities()",
                rows,
                CancellationToken.None
            )
            .ToArray();

        Assert.HasCount(2, result);
        Assert.AreEqual(new ConstructorOutput("Ada", 37), result[0]);
        Assert.AreEqual(new ConstructorOutput("Linus", 55), result[1]);
    }

    [TestMethod]
    public void CompileAndRun_WhenMemberOutputMatchesSelectAliases_ShouldInitializeTypedRowsDirectly()
    {
        var rows = new[] { new[] { new TypedInput("Ada", 37) } };

        var result = MusoqApi
            .CompileAndRun<TypedInput, MemberOutput>(
                "select Name, Age from #A.entities()",
                rows,
                CancellationToken.None
            )
            .Single();

        Assert.AreEqual("Ada", result.Name);
        Assert.AreEqual(37, result.Age);
    }

    [TestMethod]
    public void CompileAndRun_WhenTypedFinalShapeUsesHiddenOrderByColumn_ShouldConstructSortedRowsDirectly()
    {
        var rows = new[]
        {
            new[] { new TypedInput("Ada", 37), new TypedInput("Linus", 55), new TypedInput("Grace", 28) },
        };

        var result = MusoqApi
            .CompileAndRun<TypedInput, NameOutput>(
                "select Name from #A.entities() order by Age",
                rows,
                CancellationToken.None
            )
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { new NameOutput("Grace"), new NameOutput("Ada"), new NameOutput("Linus") },
            result
        );
    }

    [TestMethod]
    public void InspectTyped_WhenTypedFinalShapeUsesHiddenOrderByColumn_ShouldReportDirectRows()
    {
        var result = MusoqApi
            .Query("select Name from #A.entities() order by Age")
            .Source<TypedInput>("#A", "entities")
            .InspectTyped<NameOutput>();

        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, result.RowsKind);
        Assert.IsFalse(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
    }

    [TestMethod]
    public void FilteredTypedProjection_ShouldReadOutputOnlyForMatchesAndRetainDefaultStruct()
    {
        const string sql = "select Id, Payload from #A.entities() where Id >= 0";
        var rows = new[] { new[] { new GuardedInput(-1), new GuardedInput(0), new GuardedInput(2) } };
        var result = MusoqApi.CompileAndRun<GuardedInput, GuardedOutput>(sql, rows, CancellationToken.None).ToArray();
        CollectionAssert.AreEqual(new[] { default(GuardedOutput), new GuardedOutput(2, "accepted") }, result);
        var inspection = MusoqApi.Query(sql).Source<GuardedInput>("#A", "entities").InspectTyped<GuardedOutput>();
        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, inspection.RowsKind);
    }

    [TestMethod]
    public void FilteredReferenceProjection_ShouldRetainNullMembersAndSkipAnEntireRejectedChunk()
    {
        const string sql = "select Id, Payload from #A.entities() where Id >= 0";
        var rows = new[] { new[] { new GuardedInput(-7) }, new[] { new GuardedInput(0) } };
        var result = MusoqApi.CompileAndRun<GuardedInput, ReferenceOutput>(sql, rows, CancellationToken.None).ToArray();
        CollectionAssert.AreEqual(new[] { new ReferenceOutput(0, null) }, result);
    }

    [TestMethod]
    public void FilteredProjection_ShouldObserveCancellationWhileRejectingALargeChunk()
    {
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        var input = new CancelInput(() =>
        {
            if (++reads == 1)
                cancellation.Cancel();
            return 1;
        });
        var chunks = new[] { Enumerable.Repeat(input, 8192).ToArray() };
        Assert.Throws<System.OperationCanceledException>(() =>
            MusoqApi
                .CompileAndRun<CancelInput, GuardedOutput>(
                    "select Id, 'unused' as Payload from #A.entities() where Id < 0",
                    chunks,
                    cancellation.Token
                )
                .ToArray()
        );
        Assert.IsTrue(reads is > 0 and <= 1024);
    }

    public sealed class CancelInput(System.Func<int> readId)
    {
        public int Id => readId();
    }

    public sealed record TypedInput(string Name, int Age);

    public sealed record GuardedInput(int Id)
    {
        public string? Payload =>
            Id switch
            {
                < 0 => throw new System.InvalidOperationException("Projection read a rejected source row."),
                0 => null,
                _ => "accepted",
            };
    }

    public readonly record struct GuardedOutput(int Id, string? Payload);

    public sealed record ReferenceOutput(int Id, string? Payload);

    public sealed record ConstructorOutput(string Name, int Age);

    public sealed record NameOutput(string Name);

    public sealed class MemberOutput
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
