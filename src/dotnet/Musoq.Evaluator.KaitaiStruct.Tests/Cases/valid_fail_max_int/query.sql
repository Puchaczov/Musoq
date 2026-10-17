binary KValidFailMaxInt {
    foo: byte check foo <= 12
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailMaxInt>(f.Content) r
