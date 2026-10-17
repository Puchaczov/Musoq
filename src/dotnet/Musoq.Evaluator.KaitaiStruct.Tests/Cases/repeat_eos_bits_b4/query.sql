binary KRepeatEosBitsB4 {
    raw: byte repeat until eof,
    value0: = (raw[0] >> 4) & 15,
    value1: = (raw[0] >> 0) & 15,
    value2: = (raw[1] >> 4) & 15,
    value3: = (raw[1] >> 0) & 15
};


SELECT r.raw.Length * 2 AS A0, r.value0 AS A1, r.value1 AS A2, r.value2 AS A3, r.value3 AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosBitsB4>(f.Content) r
