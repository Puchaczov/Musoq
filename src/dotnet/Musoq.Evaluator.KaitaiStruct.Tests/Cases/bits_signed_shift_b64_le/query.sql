binary KBitsSignedShiftB64Le {
    a: bits[63],
    b: bits[9]
};

SELECT r.a AS A0, r.b AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSignedShiftB64Le>(f.Content) r
