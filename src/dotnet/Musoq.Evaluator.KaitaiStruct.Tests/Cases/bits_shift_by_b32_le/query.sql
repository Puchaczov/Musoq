binary KBitsShiftByB32Le {
    a: bits[32],
    b: bits[8]
};

SELECT r.a AS A0, r.b AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsShiftByB32Le>(f.Content) r
