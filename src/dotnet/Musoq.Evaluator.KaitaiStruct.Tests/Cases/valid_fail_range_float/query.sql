binary KValidFailRangeFloat {
    foo: float le check foo >= 0.25 AND foo <= 0.375
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRangeFloat>(f.Content) r
