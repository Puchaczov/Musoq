using System;
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

        var result = MusoqApi.CompileAndRun<TypedInput, ConstructorOutput>(
                "select Name, Age from #A.entities()",
                rows,
                CancellationToken.None)
            .ToArray();

        Assert.HasCount(2, result);
        Assert.AreEqual(new ConstructorOutput("Ada", 37), result[0]);
        Assert.AreEqual(new ConstructorOutput("Linus", 55), result[1]);
    }

    [TestMethod]
    public void CompileAndRun_WhenMemberOutputMatchesSelectAliases_ShouldInitializeTypedRowsDirectly()
    {
        var rows = new[] { new[] { new TypedInput("Ada", 37) } };

        var result = MusoqApi.CompileAndRun<TypedInput, MemberOutput>(
                "select Name, Age from #A.entities()",
                rows,
                CancellationToken.None)
            .Single();

        Assert.AreEqual("Ada", result.Name);
        Assert.AreEqual(37, result.Age);
    }

    [TestMethod]
    public void CompileAndRun_WhenTypedFinalShapeUsesHiddenOrderByColumn_ShouldConstructSortedRowsDirectly()
    {
        var rows = new[] { new[] { new TypedInput("Ada", 37), new TypedInput("Linus", 55), new TypedInput("Grace", 28) } };

        var result = MusoqApi.CompileAndRun<TypedInput, NameOutput>(
                "select Name from #A.entities() order by Age",
                rows,
                CancellationToken.None)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { new NameOutput("Grace"), new NameOutput("Ada"), new NameOutput("Linus") },
            result);
    }

    [TestMethod]
    public void InspectTyped_WhenTypedFinalShapeUsesHiddenOrderByColumn_ShouldReportDirectRows()
    {
        var result = MusoqApi.Query("select Name from #A.entities() order by Age")
            .Source<TypedInput>("#A", "entities")
            .InspectTyped<NameOutput>();

        Assert.AreEqual(TypedGeneratedRowsKind.DirectRows, result.RowsKind);
        Assert.IsFalse(result.HasOutputBindingDiagnostics);
        Assert.IsFalse(result.HasFinalSinkRejectionDiagnostics);
    }

    public sealed record TypedInput(string Name, int Age);

    public sealed record ConstructorOutput(string Name, int Age);

    public sealed record NameOutput(string Name);

    public sealed class MemberOutput
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
