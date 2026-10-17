binary KBitsUnalignedB64Le {
    a_raw: bits[1],
    a: a_raw <> 0,
    b: bits[64],
    c: bits[7]
};

SELECT r.a AS A0, r.b AS A1, r.c AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsUnalignedB64Le>(f.Content) r
