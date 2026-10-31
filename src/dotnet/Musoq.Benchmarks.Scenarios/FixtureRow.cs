namespace Musoq.Benchmarks.Scenarios;

[Flags]
public enum JobState { Queued = 1, Running = 2 }

public sealed record FixtureRow(int Id, int Key, decimal Amount, string Name, int? Optional,
    int[] Items, byte[] Payload, string Text, DateTime Time, JobState State, byte[] Truncated);

public static class FixtureData
{
    public const int TestSize = 4;
    public const string Description = "Ids 0..N-1; Key=Id%4; Amount=Id+1; Optional NULL every third row; dimension keys 0,1,2; reconciliation adds right-only key 4; skewed source Key=0 except every tenth row retains Id%4; duplicate dimension fan-out 2; two collection items; fixed UTC seconds from 2020-01-01; 8-byte little-endian payload with int Value=Id+1 (payload offset defaults to zero). Stream contains N complete packets plus a 3-byte malformed tail.";

    public static FixtureRow[] Rows(int count, int payloadValueOffset = 0) => Enumerable.Range(0, count)
        .Select(index => Create(index, payloadValueOffset)).ToArray();

    public static FixtureRow Create(int index, int payloadValueOffset = 0)
    {
        var payload = new byte[8];
        payload[0] = (byte)(index % 4);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(1), checked(index + 1 + payloadValueOffset));
        payload[5] = 2;
        payload[6] = 10;
        payload[7] = 11;
        return new FixtureRow(index, index % 4, index + 1, $"item-{index % 4}",
            index % 3 == 0 ? null : index, [index, index + 1], payload,
            $"INFO|{index}|item-{index % 4}", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(index),
            index % 2 == 0 ? JobState.Queued : JobState.Running, payload[..3]);
    }
}
