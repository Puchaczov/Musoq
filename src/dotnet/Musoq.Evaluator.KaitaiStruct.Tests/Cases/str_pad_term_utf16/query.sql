binary KStrPadTermUtf16 { str_term_raw: byte[10], str_term_include_raw: byte[10], str_term_and_pad_raw: byte[9], empty: byte[0] };
binary KStrPadTermUtf16Value { raw: byte repeat until eof, value: = ToText(raw, 'utf16le') };

SELECT p0.value AS A0, p1.value AS A1, p2.value AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrPadTermUtf16>(f.Content) r
CROSS APPLY Interpret<KStrPadTermUtf16Value>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.str_term_raw), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.str_term_raw), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}')) END) p0
CROSS APPLY Interpret<KStrPadTermUtf16Value>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.str_term_include_raw), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}0000')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.str_term_include_raw), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}0000')) END) p1
CROSS APPLY Interpret<KStrPadTermUtf16Value>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.str_term_and_pad_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.str_term_and_pad_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{4})*?)0000[0-9A-F]*$', '${1}')) END) p2
