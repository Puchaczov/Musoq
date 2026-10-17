binary KBitsSimpleLe {
    byte_1: bits[8],
    byte_2: bits[8],
    bits_a_raw: bits[1],
    bits_a: bits_a_raw <> 0,
    bits_b: bits[3],
    bits_c: bits[4],
    large_bits_1: bits[10],
    spacer: bits[3],
    large_bits_2: bits[11],
    normal_s2: short be,
    byte_8_9_10: bits[24],
    byte_11_to_14: bits[32],
    byte_15_to_19: bits[40],
    byte_20_to_27: bits[64],
    test_if_b1: = CASE WHEN bits_a = true THEN 123 ELSE NULL END
};

SELECT r.byte_1 AS A0, r.byte_2 AS A1, r.bits_a AS A2, r.bits_b AS A3, r.bits_c AS A4, r.large_bits_1 AS A5, r.spacer AS A6, r.large_bits_2 AS A7, r.normal_s2 AS A8, r.byte_8_9_10 AS A9, r.byte_11_to_14 AS A10, r.byte_15_to_19 AS A11, r.byte_20_to_27 AS A12, r.test_if_b1 AS A13
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSimpleLe>(f.Content) r
