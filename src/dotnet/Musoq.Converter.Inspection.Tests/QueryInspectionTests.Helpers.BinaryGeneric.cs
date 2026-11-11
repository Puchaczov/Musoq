namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string CreateGenericBinaryInterpretQuery()
    {
        return (
            "\n"
            + "            binary GenericItem {\n"
            + "                Value: byte\n"
            + "            };\n"
            + "            binary LengthPrefixed<T> {\n"
            + "                Count: byte,\n"
            + "                Data: T[Count]\n"
            + "            };\n"
            + "            binary GenericContainer {\n"
            + "                Items: LengthPrefixed<GenericItem>\n"
            + "            };\n"
            + "            select i.Name, d.Value as ItemValue from #apply.items() i cross apply Interpret<GenericContainer>(i.Content) c cross apply c.Items.Data d"
        );
    }

    private static byte[] CreateGenericPacketContent()
    {
        return [0x03, 0x0A, 0x0B, 0x0C];
    }

    private static string CreateNestedGenericBinaryInterpretQuery()
    {
        return (
            "\n"
            + "            binary ByteValue {\n"
            + "                Value: byte\n"
            + "            };\n"
            + "            binary ShortValue {\n"
            + "                Value: short le\n"
            + "            };\n"
            + "            binary Pair<T, U> {\n"
            + "                LeftItem: T,\n"
            + "                RightItem: U\n"
            + "            };\n"
            + "            binary LengthPrefixed<T> {\n"
            + "                Count: byte,\n"
            + "                Data: T[Count]\n"
            + "            };\n"
            + "            binary NestedGenericContainer {\n"
            + "                Items: LengthPrefixed<Pair<ByteValue,ShortValue>>\n"
            + "            };\n"
            + "            select i.Name, p.LeftItem.Value as LeftValue, p.RightItem.Value as RightValue\n"
            + "            from #apply.items() i\n"
            + "            cross apply Interpret<NestedGenericContainer>(i.Content) c\n"
            + "            cross apply c.Items.Data p"
        );
    }

    private static byte[] CreateNestedGenericPacketContent()
    {
        return [0x02, 0x0A, 0x34, 0x12, 0x0B, 0x78, 0x56];
    }
}
