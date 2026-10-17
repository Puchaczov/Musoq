binary KBitsUnalignedB32Be {
    first: byte,
    tail: uint be,
    a: = (first & 128) <> 0,
    b: = (((first & 127) + 0ui) << 25) | (tail >> 7),
    c: = tail & 127
};


SELECT r.a AS A0, r.b AS A1, r.c AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsUnalignedB32Be>(f.Content) r
