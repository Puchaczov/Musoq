binary KTermStrz2 {
    s1_raw: byte repeat until s1_raw[-1] = 124 at 0,
    s1: = ToText(FromHex(Substring(ToHex(s1_raw), 0, (s1_raw.Length - 1) * 2)), 'utf8'),
    s2_raw: byte repeat until s2_raw[-1] = 124 at (0) + s1_raw.Length,
    s2: = ToText(s2_raw, 'utf8'),
    s3_raw: byte repeat until s3_raw[-1] = 64 at ((0) + s1_raw.Length) + s2_raw.Length,
    s3: = ToText(FromHex(Substring(ToHex(s3_raw), 0, (s3_raw.Length - 1) * 2)), 'utf8')
};


SELECT r.s1 AS A0, r.s2 AS A1, r.s3 AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTermStrz2>(f.Content) r
