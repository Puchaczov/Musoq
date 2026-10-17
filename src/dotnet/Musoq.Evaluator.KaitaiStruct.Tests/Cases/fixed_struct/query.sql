binary KFixedStructHeader {
    magic1: byte[6] magic [80, 65, 67, 75, 45, 49],
    uint8: byte,
    sint8: sbyte,
    magic_uint: byte[10] magic [80, 65, 67, 75, 45, 85, 45, 68, 69, 70],
    uint16: ushort le,
    uint32: uint le,
    uint64: ulong le,
    magic_sint: byte[10] magic [80, 65, 67, 75, 45, 83, 45, 68, 69, 70],
    sint16: short le,
    sint32: int le,
    sint64: long le,
    magic_uint_le: byte[9] magic [80, 65, 67, 75, 45, 85, 45, 76, 69],
    uint16le: ushort le,
    uint32le: uint le,
    uint64le: ulong le,
    magic_sint_le: byte[9] magic [80, 65, 67, 75, 45, 83, 45, 76, 69],
    sint16le: short le,
    sint32le: int le,
    sint64le: long le,
    magic_uint_be: byte[9] magic [80, 65, 67, 75, 45, 85, 45, 66, 69],
    uint16be: ushort be,
    uint32be: uint be,
    uint64be: ulong be,
    magic_sint_be: byte[9] magic [80, 65, 67, 75, 45, 83, 45, 66, 69],
    sint16be: short be,
    sint32be: int be,
    sint64be: long be
};

binary KFixedStruct {
    hdr: KFixedStructHeader at 0
};

SELECT r.hdr.uint8 AS A0, r.hdr.uint16 AS A1, r.hdr.uint32 AS A2, r.hdr.uint64 AS A3, r.hdr.sint8 AS A4, r.hdr.sint16 AS A5, r.hdr.sint32 AS A6, r.hdr.sint64 AS A7, r.hdr.uint16le AS A8, r.hdr.uint32le AS A9, r.hdr.uint64le AS A10, r.hdr.sint16le AS A11, r.hdr.sint32le AS A12, r.hdr.sint64le AS A13, r.hdr.uint16be AS A14, r.hdr.uint32be AS A15, r.hdr.uint64be AS A16, r.hdr.sint16be AS A17, r.hdr.sint32be AS A18, r.hdr.sint64be AS A19
FROM #fixture.bytes() f
CROSS APPLY Interpret<KFixedStruct>(f.Content) r
