binary KDefaultEndianExprInheritedDoc {
    indicator: byte[2],
    big: = ToHex(indicator) <> '4949',
    number: uint le,
    some_int: = CASE WHEN big THEN ((number & 255ui) << 24) | ((number & 65280ui) << 8) | ((number >> 8) & 65280ui) | (number >> 24) ELSE number END,
    some_int1_raw: ushort le,
    some_int1: = CASE WHEN big THEN ((some_int1_raw & 255) << 8) | (some_int1_raw >> 8) ELSE some_int1_raw END,
    some_int2_raw: ushort le,
    some_int2: = CASE WHEN big THEN ((some_int2_raw & 255) << 8) | (some_int2_raw >> 8) ELSE some_int2_raw END
};
binary KDefaultEndianExprInherited { docs: KDefaultEndianExprInheritedDoc repeat until eof, inst_le: uint le at 2, inst_be: uint be at 2, restore: byte[0] at docs.Length * 10 };


SELECT r.docs[0].indicator AS A0, r.docs[0].some_int AS A1, r.docs[0].some_int1 AS A2, r.docs[0].some_int2 AS A3, CASE WHEN r.docs[0].big THEN r.inst_be ELSE r.inst_le END AS A4, r.docs[1].indicator AS A5, r.docs[1].some_int AS A6, r.docs[1].some_int1 AS A7, r.docs[1].some_int2 AS A8, CASE WHEN r.docs[1].big THEN r.inst_be ELSE r.inst_le END AS A9, r.docs[2].indicator AS A10, r.docs[2].some_int AS A11, r.docs[2].some_int1 AS A12, r.docs[2].some_int2 AS A13, CASE WHEN r.docs[2].big THEN r.inst_be ELSE r.inst_le END AS A14
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultEndianExprInherited>(f.Content) r
