binary KTermStrzUtf16V4Value { value: string[6] utf16le };
binary KTermStrzUtf16V4 { s1: substream[6] as KTermStrzUtf16V4Value, skip_term1: byte[2], s2: substream[6] as KTermStrzUtf16V4Value, skip_term2: byte[2], s3: substream[6] as KTermStrzUtf16V4Value };


SELECT r.s1.value AS A0, r.s2.value AS A1, r.s3.value AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTermStrzUtf16V4>(f.Content) r
