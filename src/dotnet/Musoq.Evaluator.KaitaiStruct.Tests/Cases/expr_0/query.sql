binary KExpr0 {
    len_of_1: ushort le,
    must_be_f7: = 7 + 0xf0,
    must_be_abc123: = r'abc' + r'123'
};

SELECT r.must_be_f7 AS A0, r.must_be_abc123 AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExpr0>(f.Content) r
