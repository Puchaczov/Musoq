binary KRepeatEosBytesPadItem { raw: byte[5] };
binary KRepeatEosBytesPad { records: KRepeatEosBytesPadItem repeat until eof, empty: byte[0] };
binary KRepeatEosBytesPadValue { raw: byte repeat until eof };

SELECT r.records.Length AS A0, p0.raw AS A1, p1.raw AS A2, p2.raw AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosBytesPad>(f.Content) r
CROSS APPLY Interpret<KRepeatEosBytesPadValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.records[0].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.records[0].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) END) p0
CROSS APPLY Interpret<KRepeatEosBytesPadValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.records[1].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.records[1].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) END) p1
CROSS APPLY Interpret<KRepeatEosBytesPadValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.records[2].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.records[2].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}')) END) p2
