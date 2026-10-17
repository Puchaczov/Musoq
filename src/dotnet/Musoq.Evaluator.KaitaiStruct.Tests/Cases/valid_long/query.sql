binary KValidLong {
    magic1: byte[6] const [0x50, 0x41, 0x43, 0x4b, 0x2d, 0x31],
    uint8: byte const 255,
    sint8: sbyte const -1,
    magic_uint: string[10] utf8 const r'PACK-U-DEF',
    uint16: ushort le const 65535,
    uint32: uint le const 4294967295ui,
    uint64: ulong le const 18446744073709551615ul,
    magic_sint: string[10] utf8 const r'PACK-S-DEF',
    sint16: short le const -1,
    sint32: int le const -1,
    sint64: long le const -1
};


SELECT r.magic1 AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidLong>(f.Content) r
