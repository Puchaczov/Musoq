binary KRepeatEosBytes {
    records: byte[5] repeat until eof
};

SELECT r.records.Length AS A0, r.records[0] AS A1, r.records[1] AS A2, r.records[2] AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosBytes>(f.Content) r
