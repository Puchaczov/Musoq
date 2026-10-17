using System.Text.Json;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class CaseSetIntegrity
{
    private const int ExpectedCaseCount = 200;
    private const int ExpectedQueryCount = 204;

    public static string[] FindFailures()
    {
        var caseIds = CaseCatalog.Ids.ToArray();
        return ValidateCaseCount(caseIds)
            .Concat(ValidateCases(caseIds))
            .ToArray();
    }

    private static IEnumerable<string> ValidateCaseCount(IReadOnlyCollection<string> caseIds)
    {
        if (caseIds.Count != ExpectedCaseCount)
            yield return $"Expected {ExpectedCaseCount} cases, found {caseIds.Count}.";
    }

    private static IEnumerable<string> ValidateCases(IEnumerable<string> caseIds)
    {
        var queryCount = 0;
        foreach (var id in caseIds)
        {
            using var document = CaseCatalog.Load(id);
            var test = document.RootElement;
            var fixture = test.GetProperty("fixture").GetString()!;
            if (!File.Exists(CaseCatalog.ProjectPath(fixture)))
                yield return $"Missing fixture '{fixture}' for '{id}'.";

            foreach (var failure in ValidateAssertionMappings(id, test))
                yield return failure;

            foreach (var query in test.GetProperty("queries").EnumerateArray())
            {
                queryCount++;
                var path = query.GetProperty("path").GetString()!;
                if (!File.Exists(CaseCatalog.ProjectPath($"Cases/{id}/{path}")))
                    yield return $"Missing query '{path}' for '{id}'.";
            }
        }

        if (queryCount != ExpectedQueryCount)
            yield return $"Expected {ExpectedQueryCount} queries, found {queryCount}.";
    }

    private static IEnumerable<string> ValidateAssertionMappings(string id, JsonElement test)
    {
        var assertionCount = test.GetProperty("assertions").GetArrayLength();
        var mappedAssertionIndices = test.GetProperty("queries").EnumerateArray()
            .Where(query => query.TryGetProperty("assertionIndices", out _))
            .SelectMany(query => query.GetProperty("assertionIndices").EnumerateArray()
                .Select(index => index.GetInt32()))
            .OrderBy(index => index);
        if (!Enumerable.Range(0, assertionCount).SequenceEqual(mappedAssertionIndices))
            yield return $"Assertions are not mapped exactly once for '{id}'.";
    }
}
