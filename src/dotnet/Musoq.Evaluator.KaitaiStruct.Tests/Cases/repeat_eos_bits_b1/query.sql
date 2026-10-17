binary KRepeatEosBitsB1 {
    raw: byte repeat until eof,
    value0: = ((raw[0] >> 7) & 1) <> 0,
    value1: = ((raw[0] >> 6) & 1) <> 0,
    value2: = ((raw[0] >> 5) & 1) <> 0,
    value3: = ((raw[0] >> 4) & 1) <> 0,
    value4: = ((raw[0] >> 3) & 1) <> 0,
    value5: = ((raw[0] >> 2) & 1) <> 0,
    value6: = ((raw[0] >> 1) & 1) <> 0,
    value7: = ((raw[0] >> 0) & 1) <> 0,
    value8: = ((raw[1] >> 7) & 1) <> 0,
    value9: = ((raw[1] >> 6) & 1) <> 0,
    value10: = ((raw[1] >> 5) & 1) <> 0,
    value11: = ((raw[1] >> 4) & 1) <> 0,
    value12: = ((raw[1] >> 3) & 1) <> 0,
    value13: = ((raw[1] >> 2) & 1) <> 0,
    value14: = ((raw[1] >> 1) & 1) <> 0,
    value15: = ((raw[1] >> 0) & 1) <> 0
};


SELECT r.raw.Length * 8 AS A0, r.value0 AS A1, r.value1 AS A2, r.value2 AS A3, r.value3 AS A4, r.value4 AS A5, r.value5 AS A6, r.value6 AS A7, r.value7 AS A8, r.value8 AS A9, r.value9 AS A10, r.value10 AS A11, r.value11 AS A12, r.value12 AS A13, r.value13 AS A14, r.value14 AS A15, r.value15 AS A16
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosBitsB1>(f.Content) r
