binary KDefaultEndianExprIsLeDoc {
    indicator: byte[2],
    big: = ToHex(indicator) <> '4949',
    number: uint le,
    some_int: = CASE WHEN big THEN ((number & 255ui) << 24) | ((number & 65280ui) << 8) | ((number >> 8) & 65280ui) | (number >> 24) ELSE number END,
    some_int_be: ushort be,
    some_int_le: ushort le
};
binary KDefaultEndianExprIsLe { docs: KDefaultEndianExprIsLeDoc repeat until eof, inst_le: uint le at 2, inst_be: uint be at 2, restore: byte[0] at docs.Length * 10 };


SELECT r.docs[0].indicator AS A0, r.docs[0].some_int AS A1, r.docs[0].some_int_be AS A2, r.docs[0].some_int_le AS A3, r.docs[1].indicator AS A4, r.docs[1].some_int AS A5, r.docs[1].some_int_be AS A6, r.docs[1].some_int_le AS A7, r.docs[2].indicator AS A8, r.docs[2].some_int AS A9, r.docs[2].some_int_be AS A10, r.docs[2].some_int_le AS A11
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultEndianExprIsLe>(f.Content) r
