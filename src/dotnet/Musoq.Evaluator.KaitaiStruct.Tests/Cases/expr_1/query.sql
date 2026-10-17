binary KExpr1 {
    len_of_1: ushort le,
    len_of_1_mod: = len_of_1 - 2,
    str1: string[len_of_1_mod] ascii,
    str1_len: = str1.Length
};

SELECT r.len_of_1 AS A0, r.len_of_1_mod AS A1, r.str1 AS A2, r.str1_len AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExpr1>(f.Content) r
