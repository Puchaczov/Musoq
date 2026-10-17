binary KBitsSignedResB32Be {
    a: uint be
};


SELECT r.a AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSignedResB32Be>(f.Content) r
