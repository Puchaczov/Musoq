using System.Buffers.Binary;
using System.Globalization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

public sealed record PerformanceRow(int Id, string StringKey, int Partition, int? Subkey,
    decimal Amount, string GroupKey, int Bucket, string Text, int[] Items, byte[] Payload);

internal static class PerformanceFixture
{
    internal static FixtureRequirements Requirements(Scenario scenario, int count)
    {
        ValidateProfile(scenario.DataProfileId);
        return new(scenario.ValidatePrimitiveTypes, null, StructuralInputLimits.Default);
    }

    internal static PreparedFixture Prepare(Scenario scenario, int count)
    {
        var requirements = Requirements(scenario, count);
        var rows = Enumerable.Range(0, count).Select(index => CreateRow(index, scenario.DataProfileId)).ToArray();
        var right = rows.ToArray();
        StandardDataProfiles.Shuffle(right);
        return new(scenario, count, requirements,
            () => new PerformanceProvider(rows, right, scenario.DataProfileId));
    }

    internal static string Distribution(Scenario scenario)
    {
        ValidateProfile(scenario.DataProfileId);
        return "Id=0..N-1; both sources contain N rows; right is Fisher-Yates/xorshift32 seed 1729; unique key-NNNNNNNN strings; " +
            "Partition=Id%64; Subkey=NULL every fifth row otherwise Id/64; Amount=Id+1; GroupKey=group-(Id/100); Bucket=Id%4; " +
            "eight items 0..7; default Text is 64 ASCII characters. Profile=" + scenario.DataProfileId +
            "; scalar-serial denies concurrent source permissions; binary-4k uses a 4096-byte little-endian integer/tail packet; " +
            "text-1k uses a 1024-character INFO|Id|label record. InputCount is N per source.";
    }

    private static void ValidateProfile(string profile)
    {
        if (profile is not ("default" or "scalar-serial" or "binary-4k" or "text-1k"))
            throw new ArgumentException($"Unknown performance fixture profile '{profile}'.", nameof(profile));
    }

    private static PerformanceRow CreateRow(int index, string profile)
    {
        var id = index.ToString("D8", CultureInfo.InvariantCulture);
        var text = ("row-" + id).PadRight(64, 'x');
        if (profile == "text-1k")
        {
            var prefix = "INFO|" + index.ToString(CultureInfo.InvariantCulture) + "|";
            text = prefix + new string('x', 1024 - prefix.Length);
        }
        byte[] payload = [];
        if (profile == "binary-4k")
        {
            payload = new byte[4096];
            BinaryPrimitives.WriteInt32LittleEndian(payload, index + 1);
            payload.AsSpan(4).Fill(1);
        }
        return new(index, "key-" + id, index % 64, index % 5 == 0 ? null : index / 64,
            index + 1, "group-" + (index / 100).ToString("D8", CultureInfo.InvariantCulture), index % 4,
            text, [0, 1, 2, 3, 4, 5, 6, 7], payload);
    }
}
