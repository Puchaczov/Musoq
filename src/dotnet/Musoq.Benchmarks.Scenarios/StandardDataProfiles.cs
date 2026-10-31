namespace Musoq.Benchmarks.Scenarios;

internal enum StandardDataProfile { Default, ManyGroups, Shuffled, Fanout8 }

internal static class StandardDataProfiles
{
    internal const uint ShuffleSeed = 1729;

    internal static StandardDataProfile Get(string id) => id switch
    {
        "default" => StandardDataProfile.Default,
        "many-groups" => StandardDataProfile.ManyGroups,
        "shuffled" => StandardDataProfile.Shuffled,
        "fanout8" => StandardDataProfile.Fanout8,
        _ => throw new ArgumentException($"Unknown standard fixture profile '{id}'.", nameof(id))
    };

    internal static string Distribution(Scenario scenario) => Get(scenario.DataProfileId) switch
    {
        StandardDataProfile.Default => FixtureData.Description,
        StandardDataProfile.ManyGroups => FixtureData.Description + " Profile override: rows.Key=Id, giving N distinct main-input keys; all other sources and fields retain default values.",
        StandardDataProfile.Shuffled => FixtureData.Description + " Profile override: rows use Fisher-Yates with xorshift32 (13,17,5), seed 1729; values and other sources retain their default contents.",
        StandardDataProfile.Fanout8 => FixtureData.Description + " Profile override: twice repeats each of the three dimension rows eight times, giving 24 dimension rows and eight matches per eligible main row.",
        _ => throw new InvalidOperationException("Unregistered standard fixture profile.")
    };

    internal static FixtureRow[] Rows(int count, int offset, StandardDataProfile profile)
    {
        var rows = FixtureData.Rows(count, offset);
        if (profile == StandardDataProfile.ManyGroups)
            return rows.Select(row => row with { Key = row.Id }).ToArray();
        if (profile == StandardDataProfile.Shuffled)
            Shuffle(rows);
        return rows;
    }

    private static void Shuffle(FixtureRow[] rows)
    {
        var state = ShuffleSeed;
        for (var index = rows.Length - 1; index > 0; index--)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            var other = (int)(state % (uint)(index + 1));
            (rows[index], rows[other]) = (rows[other], rows[index]);
        }
    }
}
