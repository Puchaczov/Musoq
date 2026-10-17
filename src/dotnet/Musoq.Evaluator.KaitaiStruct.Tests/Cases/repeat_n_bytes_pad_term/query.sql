binary KRepeatNBytesPadTermItem { raw: byte[5] };
binary KRepeatNBytesPadTerm { records: KRepeatNBytesPadTermItem[3], empty: byte[0] };
binary KRepeatNBytesPadTermValue { raw: byte repeat until eof };

SELECT r.records.Length AS A0, p0.raw AS A1, p1.raw AS A2, p2.raw AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNBytesPadTerm>(f.Content) r
CROSS APPLY Interpret<KRepeatNBytesPadTermValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[0].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[0].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) END) p0
CROSS APPLY Interpret<KRepeatNBytesPadTermValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[1].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[1].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) END) p1
CROSS APPLY Interpret<KRepeatNBytesPadTermValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[2].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.records[2].raw), '^((?:[0-9A-F]{2})*?)(?:AA)*$', '${1}'), '^((?:[0-9A-F]{2})*?)55[0-9A-F]*$', '${1}55')) END) p2
