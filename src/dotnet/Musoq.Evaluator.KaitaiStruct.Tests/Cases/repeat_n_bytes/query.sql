binary KRepeatNBytes {
    records: byte[5][3]
};

SELECT r.records.Length AS A0, r.records[0] AS A1, r.records[1] AS A2, r.records[2] AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNBytes>(f.Content) r
