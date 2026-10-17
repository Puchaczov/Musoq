binary KBitsSignedResB32Le {
    a: bits[32]
};

SELECT r.a AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSignedResB32Le>(f.Content) r
