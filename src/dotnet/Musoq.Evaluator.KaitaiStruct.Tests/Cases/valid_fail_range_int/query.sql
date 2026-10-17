binary KValidFailRangeInt {
    foo: byte check foo >= 5 AND foo <= 10
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRangeInt>(f.Content) r
