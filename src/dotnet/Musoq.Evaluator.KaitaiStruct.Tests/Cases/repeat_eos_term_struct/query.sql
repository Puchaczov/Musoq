binary KRepeatEosTermStructRecord { value: byte repeat until value[-1] = 178 };
binary KRepeatEosTermStruct { records: KRepeatEosTermStructRecord repeat until eof };


SELECT r.records.Length AS A0, r.records[0].value AS A1, r.records[1].value AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosTermStruct>(f.Content) r
