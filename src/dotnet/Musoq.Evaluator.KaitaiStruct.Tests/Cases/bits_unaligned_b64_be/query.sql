binary KBitsUnalignedB64Be {
    first: byte,
    tail: ulong be,
    a: = (first & 128) <> 0,
    b: = (((first & 127) + 0ul) << 57) | (tail >> 7),
    c: = tail & 127
};


SELECT r.a AS A0, r.b AS A1, r.c AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsUnalignedB64Be>(f.Content) r
