using System;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string CreatePrimitiveArrayBinaryInterpretQuery()
    {
        return @"
            binary ArrayPacket {
                Count: byte,
                Values: short[Count] le
            };
            select i.Name, p.Count, v.Value as Number from #apply.items() i cross apply Interpret<ArrayPacket>(i.Content) p cross apply p.Values v";
    }

    private static byte[] CreatePrimitiveArrayPacketContent()
    {
        var content = new byte[sizeof(byte) + sizeof(short) * 3];
        content[0] = 3;
        BitConverter.GetBytes((short)1).CopyTo(content, sizeof(byte));
        BitConverter.GetBytes((short)2).CopyTo(content, sizeof(byte) + sizeof(short));
        BitConverter.GetBytes((short)3).CopyTo(content, sizeof(byte) + sizeof(short) * 2);
        return content;
    }

    private static string CreateStringArrayBinaryInterpretQuery()
    {
        return @"
            binary StringArrayPacket {
                Count: byte,
                Names: string[3] ascii[Count]
            };
            select i.Name, p.Count, n.Value as Text from #apply.items() i cross apply Interpret<StringArrayPacket>(i.Content) p cross apply p.Names n";
    }

    private static byte[] CreateStringArrayPacketContent()
    {
        return
        [
            3,
            (byte)'A',
            (byte)'d',
            (byte)'a',
            (byte)'B',
            (byte)'e',
            (byte)'a',
            (byte)'C',
            (byte)'a',
            (byte)'l'
        ];
    }

    private static string CreateInlineSchemaArrayBinaryInterpretQuery()
    {
        return @"
            binary InlineArrayPacket {
                Count: byte,
                Items: { Tag: byte, Value: short le }[Count]
            };
            select i.Name, p.Count, it.Tag, it.Value from #apply.items() i cross apply Interpret<InlineArrayPacket>(i.Content) p cross apply p.Items it";
    }

    private static byte[] CreateInlineSchemaArrayPacketContent()
    {
        var content = new byte[sizeof(byte) + (sizeof(byte) + sizeof(short)) * 2];
        content[0] = 2;
        content[1] = 0xA1;
        BitConverter.GetBytes((short)258).CopyTo(content, 2);
        content[4] = 0xB2;
        BitConverter.GetBytes((short)772).CopyTo(content, 5);
        return content;
    }

    private static string CreatePrimitiveRepeatUntilBinaryInterpretQuery()
    {
        return @"
            binary PrimitiveRepeatPacket {
                Values: byte repeat until Values = 0
            };
            select i.Name, v.Value as Number from #apply.items() i cross apply Interpret<PrimitiveRepeatPacket>(i.Content) p cross apply p.Values v";
    }

    private static byte[] CreatePrimitiveRepeatUntilPacketContent()
    {
        return [1, 2, 3, 0];
    }

    private static string CreateBitsRepeatUntilBinaryInterpretQuery()
    {
        return @"
            binary BitsRepeatPacket {
                Flags: bits[1] repeat until Flags = 0
            };
            select i.Name, f.Value as FlagValue from #apply.items() i cross apply Interpret<BitsRepeatPacket>(i.Content) p cross apply p.Flags f";
    }

    private static byte[] CreateBitsRepeatUntilPacketContent()
    {
        return [0x01];
    }

    private static string CreateStringRepeatUntilBinaryInterpretQuery()
    {
        return @"
            binary StringRepeatPacket {
                Names: string[3] ascii repeat until Names = 'END'
            };
            select i.Name, n.Value as Text from #apply.items() i cross apply Interpret<StringRepeatPacket>(i.Content) p cross apply p.Names n";
    }

    private static byte[] CreateStringRepeatUntilPacketContent()
    {
        return "AdaBenEND"u8.ToArray();
    }

    private static string CreateInlineSchemaRepeatUntilBinaryInterpretQuery()
    {
        return @"
            binary InlineRepeatPacket {
                Items: { Tag: byte, Value: short le } repeat until Items.Tag = 0
            };
            select i.Name, it.Tag, it.Value from #apply.items() i cross apply Interpret<InlineRepeatPacket>(i.Content) p cross apply p.Items it";
    }

    private static byte[] CreateInlineSchemaRepeatUntilPacketContent()
    {
        var content = new byte[(sizeof(byte) + sizeof(short)) * 3];
        content[0] = 0xA1;
        BitConverter.GetBytes((short)258).CopyTo(content, 1);
        content[3] = 0xB2;
        BitConverter.GetBytes((short)772).CopyTo(content, 4);
        content[6] = 0x00;
        BitConverter.GetBytes((short)1029).CopyTo(content, 7);
        return content;
    }

    private static string CreateSchemaReferenceArrayBinaryInterpretQuery()
    {
        return @"
            binary Item {
                Value: byte
            };
            binary SchemaArrayPacket {
                Count: byte,
                Items: Item[Count]
            };
            select i.Name, p.Count, it.Value as ItemValue from #apply.items() i cross apply Interpret<SchemaArrayPacket>(i.Content) p cross apply p.Items it";
    }

    private static byte[] CreateSchemaReferenceArrayPacketContent()
    {
        var content = new byte[sizeof(byte) + 3];
        content[0] = 3;
        content[sizeof(byte)] = 0xAA;
        content[sizeof(byte) + 1] = 0xBB;
        content[sizeof(byte) + 2] = 0xCC;
        return content;
    }

    private static string CreateSchemaReferenceRepeatUntilBinaryInterpretQuery()
    {
        return @"
            binary RepeatItem {
                Value: byte
            };
            binary SchemaRepeatPacket {
                Items: RepeatItem repeat until Items.Value = 0
            };
            select i.Name, it.Value as ItemValue from #apply.items() i cross apply Interpret<SchemaRepeatPacket>(i.Content) p cross apply p.Items it";
    }

    private static byte[] CreateSchemaReferenceRepeatUntilPacketContent()
    {
        return [0xAA, 0xBB, 0x00];
    }
}
