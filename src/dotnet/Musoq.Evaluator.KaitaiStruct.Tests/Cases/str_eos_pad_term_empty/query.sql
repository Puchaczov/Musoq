binary KStrEosPadTermEmpty { str_pad_raw: byte[20], str_term_raw: byte[20], str_term_and_pad_raw: byte[20], str_term_include_raw: byte[20], empty: byte[0] };
binary KStrEosPadTermEmptyValue { raw: byte repeat until eof, value: = ToText(raw, 'utf8') };

SELECT p0.value AS A0, p1.value AS A1, p2.value AS A2, p3.value AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrEosPadTermEmpty>(f.Content) r
CROSS APPLY Interpret<KStrEosPadTermEmptyValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.str_pad_raw), '^((?:[0-9A-F]{2})*?)(?:40)*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.str_pad_raw), '^((?:[0-9A-F]{2})*?)(?:40)*$', '${1}')) END) p0
CROSS APPLY Interpret<KStrEosPadTermEmptyValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.str_term_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.str_term_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) END) p1
CROSS APPLY Interpret<KStrEosPadTermEmptyValue>(CASE WHEN f.Length(f.RegexReplace(f.RegexReplace(f.ToHex(r.str_term_and_pad_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.RegexReplace(f.ToHex(r.str_term_and_pad_raw), '^((?:[0-9A-F]{2})*?)(?:2B)*$', '${1}'), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}')) END) p2
CROSS APPLY Interpret<KStrEosPadTermEmptyValue>(CASE WHEN f.Length(f.RegexReplace(f.ToHex(r.str_term_include_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) = 0 THEN r.empty ELSE f.FromHexToBytes(f.RegexReplace(f.ToHex(r.str_term_include_raw), '^((?:[0-9A-F]{2})*?)40[0-9A-F]*$', '${1}40')) END) p3
