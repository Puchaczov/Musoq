binary KTermStrz4S1Type { raw: byte repeat until eof, marker: = IndexOf(raw, FromHex('7C')), length: = CASE WHEN marker < 0 THEN raw.Length ELSE marker END, value: = ToText(FromHex(Substring(ToHex(raw), 0, (length) * 2)), 'utf8') };

binary KTermStrz4S2Type { raw: byte repeat until eof, marker: = IndexOf(raw, FromHex('7C')), length: = CASE WHEN marker < 0 THEN raw.Length ELSE marker END, value: = ToText(FromHex(Substring(ToHex(raw), 0, (length) * 2)), 'utf8') };

binary KTermStrz4S3Type { raw: byte repeat until eof, marker: = IndexOf(raw, FromHex('40')), length: = CASE WHEN marker < 0 THEN raw.Length ELSE marker + 1 END, value: = ToText(FromHex(Substring(ToHex(raw), 0, (length) * 2)), 'utf8') };

binary KTermStrz4 { s1: substream[3] as KTermStrz4S1Type lax, skip_term1: byte, s2: substream[3] as KTermStrz4S2Type lax, skip_term2: byte, s3: substream[3] as KTermStrz4S3Type lax };


SELECT r.s1.value AS A0, r.s2.value AS A1, r.s3.value AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTermStrz4>(f.Content) r
