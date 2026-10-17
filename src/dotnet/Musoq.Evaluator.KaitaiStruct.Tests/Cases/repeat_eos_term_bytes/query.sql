binary KRepeatEosTermBytesRecord { raw: byte repeat until raw[-1] = 178 };
binary KRepeatEosTermBytes { records: KRepeatEosTermBytesRecord repeat until eof };


SELECT r.records.Length AS A0, r.records[0].raw AS A1, r.records[1].raw AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosTermBytes>(f.Content) r
