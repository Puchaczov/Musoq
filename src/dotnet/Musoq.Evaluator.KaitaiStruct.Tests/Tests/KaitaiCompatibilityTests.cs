using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

[assembly: DoNotParallelize]

namespace Musoq.Evaluator.KaitaiStruct.Tests.Tests;

[TestClass]
public sealed class KaitaiCompatibilityTests
{
    public static IEnumerable<object[]> Cases => CaseCatalog.Queries;

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Fixture_matches_expected_assertions(string id, string queryName)
    {
        using var document = CaseCatalog.Load(id);
        var test = document.RootElement;
        var specification = test.GetProperty("queries").EnumerateArray().Single(query =>
            query.GetProperty("name").GetString() == queryName);
        var bytes = File.ReadAllBytes(CaseCatalog.ProjectPath(test.GetProperty("fixture").GetString()!));
        var queryText = File.ReadAllText(CaseCatalog.ProjectPath($"Cases/{id}/" + specification.GetProperty("path").GetString()));
        using var query = InstanceCreator.CompileForExecution(queryText, "Kaitai_" + id + "_" + queryName,
            new BinaryFixtureSource(bytes), new FixtureLogger(), new CompilationOptions(usePrimitiveTypeValidation: false));
        if (queryName == "main" && test.TryGetProperty("expectedFailure", out var failure))
        {
            AssertParsingFailure(query, failure, id);
            return;
        }
        using var result = query.Run();
        Assert.AreEqual(1, result.Count, id);
        var assertions = CaseCatalog.Assertions(test, specification);
        Assert.AreEqual(AssertionValues.ColumnCount(assertions), result.Columns.Count(), id);
        for (var index = 0; index < assertions.Length; index++)
            AssertionComparer.Compare(assertions[index], AssertionValues.Read(assertions[index], result[0], index),
                $"{id}: assertion {index} ({assertions[index].GetProperty("actual").GetString()})");
    }

    private static void AssertParsingFailure(CompiledQuery query, JsonElement failure, string id)
    {
        ParsingFailureAssertion.Compare(() =>
        {
            using var table = query.Run();
            _ = table.Count;
        }, failure, id);
    }
}
