binary KBitsUnalignedB32Le {
    a_raw: bits[1],
    a: a_raw <> 0,
    b: bits[32],
    c: bits[7]
};

SELECT r.a AS A0, r.b AS A1, r.c AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsUnalignedB32Le>(f.Content) r
