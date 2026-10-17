binary KBitsSignedShiftB32Le {
    a: bits[31],
    b: bits[9]
};

SELECT r.a AS A0, r.b AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSignedShiftB32Le>(f.Content) r
