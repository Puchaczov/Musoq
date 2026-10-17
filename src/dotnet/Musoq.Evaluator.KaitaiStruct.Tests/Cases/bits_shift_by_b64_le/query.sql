binary KBitsShiftByB64Le {
    a: bits[64],
    b: bits[8]
};

SELECT r.a AS A0, r.b AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsShiftByB64Le>(f.Content) r
