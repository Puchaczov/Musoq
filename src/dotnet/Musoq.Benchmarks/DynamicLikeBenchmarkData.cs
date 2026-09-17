using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

internal static class DynamicLikeBenchmarkData
{
    public static ProfileEntity[] CreateRows(
        int rowCount,
        int patternCardinality,
        int inputLength,
        DynamicLikeBenchmarkScenario scenario)
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

    public static string ComputeResultHash(Table table)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var row in table)
        {
            foreach (var value in row.Values)
            {
                var formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
                Append(hash, formatted.Length.ToString(CultureInfo.InvariantCulture));
                Append(hash, ":");
                Append(hash, formatted);
                Append(hash, "|");
            }

            Append(hash, "\n");
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static ProfileEntity CreateRow(
        int patternIndex,
        int inputLength,
        DynamicLikeBenchmarkScenario scenario)
    {
        var (input, pattern) = scenario switch
        {
            DynamicLikeBenchmarkScenario.Ascii => CreateExactPair(patternIndex, inputLength, 'a'),
            DynamicLikeBenchmarkScenario.Unicode => CreateExactPair(patternIndex, inputLength, '\u0142'),
            DynamicLikeBenchmarkScenario.Wildcard => CreateWildcardPair(patternIndex, inputLength),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

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

    private static (string Input, string Pattern) CreateExactPair(int index, int length, char fill)
    {
        var suffix = index.ToString("X", CultureInfo.InvariantCulture);
        if (suffix.Length > length)
            throw new ArgumentOutOfRangeException(nameof(length));

        var input = new string(fill, length - suffix.Length) + suffix;
        return (input, input);
    }

    private static (string Input, string Pattern) CreateWildcardPair(int index, int length)
    {
        var identity = EncodeIdentity(index);
        if (identity.Length >= length)
            throw new ArgumentOutOfRangeException(nameof(length));

        var input = identity + new string('m', length - identity.Length);
        var pattern = identity + "%";
        return (input, pattern);
    }

    private static string EncodeIdentity(int value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#";
        Span<char> buffer = stackalloc char[8];
        var position = buffer.Length;
        do
        {
            buffer[--position] = alphabet[value % alphabet.Length];
            value /= alphabet.Length;
        } while (value > 0);

        return new string(buffer[position..]);
    }

    private static void Append(IncrementalHash hash, string value)
    {
        hash.AppendData(Encoding.UTF8.GetBytes(value));
    }
}
