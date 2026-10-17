binary KBitsSimple {
    byte_1: byte,
    byte_2: byte,
    third: byte,
    bits_a: = (third & 128) <> 0,
    bits_b: = (third >> 4) & 7,
    bits_c: = third & 15,
    fourth_fifth: ushort be,
    sixth: byte,
    large_bits_1: = fourth_fifth >> 6,
    spacer: = (fourth_fifth >> 3) & 7,
    large_bits_2: = ((fourth_fifth & 7) << 8) | sixth,
    normal_s2: short be,
    high24: ushort be,
    low24: byte,
    byte_8_9_10: = ((high24 + 0ui) << 8) | low24,
    byte_11_to_14: uint be,
    high40: byte,
    low40: uint be,
    byte_15_to_19: = ((high40 + 0ul) << 32) | low40,
    byte_20_to_27: ulong be,
    test_if_b1: = CASE WHEN bits_a = false THEN 123 ELSE NULL END
};


SELECT r.byte_1 AS A0, r.byte_2 AS A1, r.bits_a AS A2, r.bits_b AS A3, r.bits_c AS A4, r.large_bits_1 AS A5, r.spacer AS A6, r.large_bits_2 AS A7, r.normal_s2 AS A8, r.byte_8_9_10 AS A9, r.byte_11_to_14 AS A10, r.byte_15_to_19 AS A11, r.byte_20_to_27 AS A12, r.test_if_b1 AS A13
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSimple>(f.Content) r
