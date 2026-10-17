binary KTermStrzUtf16V1 {
    s1_units: ushort le repeat until s1_units[-1] = 0 at 0,
    s1_raw: byte[s1_units.Length * 2] at 0,
    s1: = ToText(FromHex(Substring(ToHex(s1_raw), 0, (s1_raw.Length - 2) * 2)), 'utf16le'),
    s2_units: ushort le repeat until s2_units[-1] = 0 at 0 + s1_raw.Length,
    s2_raw: byte[s2_units.Length * 2] at 0 + s1_raw.Length,
    s2: = ToText(FromHex(Substring(ToHex(s2_raw), 0, (s2_raw.Length - 2) * 2)), 'utf16le'),
    term: ushort le at 0 + s1_raw.Length + s2_raw.Length - 2,
    s3_units: ushort le repeat until s3_units[-1] = 0 at 0 + s1_raw.Length + s2_raw.Length - 2 + 2,
    s3_raw: byte[s3_units.Length * 2] at 0 + s1_raw.Length + s2_raw.Length - 2 + 2,
    s3: = ToText(FromHex(Substring(ToHex(s3_raw), 0, (s3_raw.Length - 0) * 2)), 'utf16le')
};


SELECT r.s1 AS A0, r.s2 AS A1, r.term AS A2, r.s3 AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTermStrzUtf16V1>(f.Content) r
