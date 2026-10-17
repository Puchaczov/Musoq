binary KIntegers {
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

binary KNameClashImportVsInst {
    integers: KIntegers at 0,
    std: = 1 + 2
};

SELECT r.integers.uint8 AS A0, r.std AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNameClashImportVsInst>(f.Content) r
