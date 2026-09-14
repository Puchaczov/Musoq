using System.Globalization;
using Musoq.Benchmarks.Schema.Profiles;

namespace Musoq.Benchmarks;

internal static class RLikeBenchmarkData
{
    public static ProfileEntity[] CreateRows(
        int rowCount,
        int patternCardinality,
        int inputLength,
        RLikeBenchmarkScenario scenario)
    {
        if (rowCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (patternCardinality <= 0 || patternCardinality > rowCount)
            throw new ArgumentOutOfRangeException(nameof(patternCardinality));
        if (inputLength < 4)
            throw new ArgumentOutOfRangeException(nameof(inputLength));

        return Enumerable.Range(0, rowCount)
            .Select(index => CreateRow(
                (int)((long)index * patternCardinality / rowCount),
                inputLength,
                scenario))
            .ToArray();
    }

    public static ProfileEntity[] CreateConstantRows(int rowCount, RLikeBenchmarkScenario scenario)
    {
        var input = scenario switch
        {
            RLikeBenchmarkScenario.Literal => "before-target-after",
            RLikeBenchmarkScenario.AnchoredLiteral => "target",
            RLikeBenchmarkScenario.Complex => "target-12345",
            RLikeBenchmarkScenario.Unicode => "Zażółć-gęślą",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        return Enumerable.Range(0, rowCount)
            .Select(_ => CreateProfile(input, string.Empty))
            .ToArray();
    }

    public static string ConstantPattern(RLikeBenchmarkScenario scenario) => scenario switch
    {
        RLikeBenchmarkScenario.Literal => "target",
        RLikeBenchmarkScenario.AnchoredLiteral => @"\Atarget\z",
        RLikeBenchmarkScenario.Complex => @"\A(?:target|alternate)-[0-9]+\z",
        RLikeBenchmarkScenario.Unicode => "gęślą",
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    private static ProfileEntity CreateRow(int patternIndex, int inputLength, RLikeBenchmarkScenario scenario)
    {
        var identity = patternIndex.ToString("X", CultureInfo.InvariantCulture);
        var unicodePrefix = scenario == RLikeBenchmarkScenario.Unicode ? "Ł" : "A";
        var suffixLength = inputLength - unicodePrefix.Length - identity.Length;
        if (suffixLength < 0)
            throw new ArgumentOutOfRangeException(nameof(inputLength));

        var input = string.Concat(unicodePrefix, new string('m', suffixLength), identity);
        var pattern = scenario switch
        {
            RLikeBenchmarkScenario.Literal => input,
            RLikeBenchmarkScenario.AnchoredLiteral => string.Concat(@"\A", input, @"\z"),
            RLikeBenchmarkScenario.Complex => string.Concat(@"\A(?:", input, "|never)[0-9]*", @"\z"),
            RLikeBenchmarkScenario.Unicode => string.Concat(@"\A", input, @"\z"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        return CreateProfile(input, pattern);
    }

    private static ProfileEntity CreateProfile(string input, string pattern)
    {
        return new ProfileEntity(
            pattern,
            "Last",
            input,
            "Unknown",
            "127.0.0.1",
            "2026-01-01",
            "image",
            "animal",
            "avatar")
        {
            FirstName = pattern,
            LastName = "Last",
            Email = input,
            Gender = "Unknown",
            IpAddress = "127.0.0.1",
            Date = "2026-01-01",
            Image = "image",
            Animal = "animal",
            Avatar = "avatar"
        };
    }
}
