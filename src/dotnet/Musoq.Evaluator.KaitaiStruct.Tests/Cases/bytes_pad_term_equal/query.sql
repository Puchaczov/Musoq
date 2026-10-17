binary KBytesPadTermEqual { s1_raw: byte[20], s2_raw: byte[20], s3_raw: byte[20], s4_raw: byte[20], empty: byte[0] };
binary KBytesPadTermEqualValue { raw: byte repeat until eof };

SELECT p0.raw AS A0, p1.raw AS A1, p2.raw AS A2, p3.raw AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBytesPadTermEqual>(f.Content) r
CROSS APPLY Interpret<KBytesPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s1_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s1_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) END) p0
CROSS APPLY Interpret<KBytesPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.s2_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.s2_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) END) p1
CROSS APPLY Interpret<KBytesPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s3_raw), '^((?:[0-9A-F]{2})*?)2B[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s3_raw), '^((?:[0-9A-F]{2})*?)2B[0-9A-F]*$', '${1}')) END) p2
CROSS APPLY Interpret<KBytesPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s4_raw), '^((?:[0-9A-F]{2})*?)2E[0-9A-F]*$', '${1}2E')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s4_raw), '^((?:[0-9A-F]{2})*?)2E[0-9A-F]*$', '${1}2E')) END) p3
