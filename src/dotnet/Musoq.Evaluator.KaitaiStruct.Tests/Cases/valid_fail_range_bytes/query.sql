binary KValidFailRangeBytes {
    foo: byte[2] check ToHex(foo) >= r'50' AND ToHex(foo) <= r'5031'
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRangeBytes>(f.Content) r
