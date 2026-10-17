binary KDefaultEndianExprIsBeDoc {
    indicator: byte[2],
    big: = ToHex(indicator) = '4D4D',
    number: uint le,
    some_int: = CASE WHEN big THEN ((number & 255ui) << 24) | ((number & 65280ui) << 8) | ((number >> 8) & 65280ui) | (number >> 24) ELSE number END,
    some_int_be: ushort be,
    some_int_le: ushort le
};
binary KDefaultEndianExprIsBe { docs: KDefaultEndianExprIsBeDoc repeat until eof, inst_le: uint le at 2, inst_be: uint be at 2, restore: byte[0] at docs.Length * 10 };


SELECT r.docs[0].indicator AS A0, r.docs[0].some_int AS A1, r.docs[0].some_int_be AS A2, r.docs[0].some_int_le AS A3, CASE WHEN r.docs[0].big THEN r.inst_be ELSE r.inst_le END AS A4, CASE WHEN r.docs[0].big THEN r.inst_be ELSE r.inst_le END AS A5, r.docs[1].indicator AS A6, r.docs[1].some_int AS A7, r.docs[1].some_int_be AS A8, r.docs[1].some_int_le AS A9, CASE WHEN r.docs[1].big THEN r.inst_be ELSE r.inst_le END AS A10, CASE WHEN r.docs[1].big THEN r.inst_be ELSE r.inst_le END AS A11, r.docs[2].indicator AS A12, r.docs[2].some_int AS A13, r.docs[2].some_int_be AS A14, r.docs[2].some_int_le AS A15, CASE WHEN r.docs[2].big THEN r.inst_be ELSE r.inst_le END AS A16, CASE WHEN r.docs[2].big THEN r.inst_be ELSE r.inst_le END AS A17
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultEndianExprIsBe>(f.Content) r
