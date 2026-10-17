binary KValidFailMinInt {
    foo: byte check foo >= 123
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailMinInt>(f.Content) r
