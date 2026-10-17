binary KStrPadTermEqual { s1_raw: byte[20], s2_raw: byte[20], s3_raw: byte[20], s4_raw: byte[20], empty: byte[0] };
binary KStrPadTermEqualValue { raw: byte repeat until eof, value: = ToText(raw, 'utf8') };

SELECT p0.value AS A0, p1.value AS A1, p2.value AS A2, p3.value AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrPadTermEqual>(f.Content) r
CROSS APPLY Interpret<KStrPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s1_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s1_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) END) p0
CROSS APPLY Interpret<KStrPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.s2_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.s2_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) END) p1
CROSS APPLY Interpret<KStrPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s3_raw), '^((?:[0-9A-F]{2})*?)2B[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s3_raw), '^((?:[0-9A-F]{2})*?)2B[0-9A-F]*$', '${1}')) END) p2
CROSS APPLY Interpret<KStrPadTermEqualValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.s4_raw), '^((?:[0-9A-F]{2})*?)2E[0-9A-F]*$', '${1}2E')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.s4_raw), '^((?:[0-9A-F]{2})*?)2E[0-9A-F]*$', '${1}2E')) END) p3
