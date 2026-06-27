namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string CreateGenericBinaryInterpretQuery()
    {
        return @"
            binary GenericItem {
                Value: byte
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary GenericContainer {
                Items: LengthPrefixed<GenericItem>
            };
            select i.Name, d.Value as ItemValue from #apply.items() i cross apply Interpret<GenericContainer>(i.Content) c cross apply c.Items.Data d";
    }

    private static byte[] CreateGenericPacketContent()
    {
        return [0x03, 0x0A, 0x0B, 0x0C];
    }

    private static string CreateNestedGenericBinaryInterpretQuery()
    {
        return @"
            binary ByteValue {
                Value: byte
            };
            binary ShortValue {
                Value: short le
            };
            binary Pair<T, U> {
                LeftItem: T,
                RightItem: U
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary NestedGenericContainer {
                Items: LengthPrefixed<Pair<ByteValue,ShortValue>>
            };
            select i.Name, p.LeftItem.Value as LeftValue, p.RightItem.Value as RightValue
            from #apply.items() i
            cross apply Interpret<NestedGenericContainer>(i.Content) c
            cross apply c.Items.Data p";
    }

    private static byte[] CreateNestedGenericPacketContent()
    {
        return [0x02, 0x0A, 0x34, 0x12, 0x0B, 0x78, 0x56];
    }
}
