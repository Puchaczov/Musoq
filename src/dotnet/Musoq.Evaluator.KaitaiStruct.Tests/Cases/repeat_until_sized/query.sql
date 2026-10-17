binary KRepeatUntilSizedRecord {
    marker: byte,
    body: uint le
};

binary KRepeatUntilSized {
    records: substream[5] as KRepeatUntilSizedRecord lax repeat until records[-1].marker = 0xaa
};

SELECT r.records.Length AS A0, r.records[0].marker AS A1, r.records[0].body AS A2, r.records[1].marker AS A3, r.records[1].body AS A4, r.records[2].marker AS A5, r.records[2].body AS A6
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatUntilSized>(f.Content) r
