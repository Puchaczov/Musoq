namespace Musoq.Benchmarks.Scenarios;

internal sealed class FixtureInputs(Dictionary<string, FixtureRows> sources)
{
    internal Dictionary<string, FixtureRows> Sources { get; } = sources;

    internal static FixtureInputs Create(int count, int payloadValueOffset = 0,
        StandardDataProfile profile = StandardDataProfile.Default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _ = FixtureSchema.StructuredLimits(count);
        var lookup = FixtureData.Rows(3);
        var sources = new Dictionary<string, FixtureRows>(StringComparer.OrdinalIgnoreCase)
        {
            ["rows"] = new(StandardDataProfiles.Rows(count, payloadValueOffset, profile)),
            ["skewed"] = new(FixtureData.Rows(count, payloadValueOffset).Select(row => row with { Key = row.Id % 10 == 0 ? row.Id % 4 : 0 }).ToArray()),
            ["planned"] = new(FixtureData.Rows(count, payloadValueOffset)),
            ["lookup"] = new(lookup),
            ["reconcile"] = new([.. lookup, FixtureData.Create(4) with { Key = 4 }]),
            ["twice"] = profile == StandardDataProfile.Fanout8
                ? new(lookup.SelectMany(row => Enumerable.Repeat(row, 8)).ToArray())
                : new(lookup.SelectMany(row => new[] { row, row }).ToArray()),
            ["empty"] = new([])
        };
        var stream = FixtureData.Rows(count, payloadValueOffset).SelectMany(row => row.Payload)
            .Concat(FixtureData.Create(0, payloadValueOffset).Truncated).ToArray();
        sources.Add("stream", new FixtureRows([FixtureData.Create(0, payloadValueOffset) with { Payload = stream }]));
        return new(sources);
    }
}
