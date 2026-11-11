using System;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static string CreateConditionalBinaryInterpretQuery()
    {
        return @"
            binary OptionalPacket {
                HasValue: byte,
                Value: int le when HasValue = 1
            };
            select i.Name, p.HasValue, p.Value from #apply.items() i cross apply Interpret<OptionalPacket>(i.Content) p";
    }

    private static byte[] CreateOptionalPacketContent(int? value)
    {
        if (!value.HasValue)
            return [0];

        var content = new byte[sizeof(byte) + sizeof(int)];
        content[0] = 1;
        BitConverter.GetBytes(value.Value).CopyTo(content, sizeof(byte));
        return content;
    }

    private static string CreateConstrainedOffsetBinaryInterpretQuery()
    {
        return @"
            binary IndexedPacket {
                Magic: int le check Magic = 0x46495845,
                HeaderSize: int le,
                DataOffset: int le,
                Data: int le at DataOffset
            };
            select i.Name, p.HeaderSize, p.DataOffset, p.Data from #apply.items() i cross apply Interpret<IndexedPacket>(i.Content) p";
    }

    private const int IndexedPacketHeaderSize = 12;
    private const int IndexedPacketDataOffset = 16;

    private static byte[] CreateIndexedPacketContent(int data)
    {
        const int magic = 0x46495845;

        var content = new byte[IndexedPacketDataOffset + sizeof(int)];
        BitConverter.GetBytes(magic).CopyTo(content, 0);
        BitConverter.GetBytes(IndexedPacketHeaderSize).CopyTo(content, sizeof(int));
        BitConverter.GetBytes(IndexedPacketDataOffset).CopyTo(content, sizeof(int) * 2);
        BitConverter.GetBytes(data).CopyTo(content, IndexedPacketDataOffset);
        return content;
    }

    private static string CreateComputedBinaryInterpretQuery()
    {
        return @"
            binary Rectangle {
                Width: int le,
                Height: int le,
                Area: Width * Height
            };
            select i.Name, r.Width, r.Height, r.Area from #apply.items() i cross apply Interpret<Rectangle>(i.Content) r";
    }

    private static byte[] CreateRectangleContent(int width, int height)
    {
        var content = new byte[sizeof(int) * 2];
        BitConverter.GetBytes(width).CopyTo(content, 0);
        BitConverter.GetBytes(height).CopyTo(content, sizeof(int));
        return content;
    }

    private static string CreateNestedBinaryInterpretQuery()
    {
        return @"
            binary Point {
                X: float le,
                Y: float le
            };
            binary Vertex {
                Id: int le,
                Position: Point
            };
            select i.Name, v.Id, v.Position.X as X, v.Position.Y as Y from #apply.items() i cross apply Interpret<Vertex>(i.Content) v";
    }

    private static byte[] CreateVertexContent(int id, float x, float y)
    {
        var content = new byte[sizeof(int) + sizeof(float) * 2];
        BitConverter.GetBytes(id).CopyTo(content, 0);
        BitConverter.GetBytes(x).CopyTo(content, sizeof(int));
        BitConverter.GetBytes(y).CopyTo(content, sizeof(int) + sizeof(float));
        return content;
    }

    private static string CreateInlineBinaryInterpretQuery()
    {
        return @"
            binary InlinePacket {
                Header: {
                    Magic: int le,
                    Version: short le
                },
                Payload: byte
            };
            select i.Name, p.Header.Magic as Magic, p.Header.Version as Version, p.Payload from #apply.items() i cross apply Interpret<InlinePacket>(i.Content) p";
    }

    private static byte[] CreateInlinePacketContent(int magic, short version, byte payload)
    {
        var content = new byte[sizeof(int) + sizeof(short) + sizeof(byte)];
        BitConverter.GetBytes(magic).CopyTo(content, 0);
        BitConverter.GetBytes(version).CopyTo(content, sizeof(int));
        content[sizeof(int) + sizeof(short)] = payload;
        return content;
    }

    private static string CreateStringBinaryInterpretQuery()
    {
        return @"
            binary TextPacket {
                Length: byte,
                Text: string[Length] ascii trim
            };
            select i.Name, p.Length, p.Text from #apply.items() i cross apply Interpret<TextPacket>(i.Content) p";
    }

    private static byte[] CreateTextPacketContent()
    {
        return
        [
            5,
            (byte)'A',
            (byte)'d',
            (byte)'a',
            (byte)' ',
            (byte)' '
        ];
    }
}
