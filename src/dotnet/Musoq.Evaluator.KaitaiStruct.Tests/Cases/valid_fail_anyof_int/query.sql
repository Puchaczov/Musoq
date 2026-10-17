binary KValidFailAnyofInt {
    foo: byte oneOf [5, 6, 7, 8, 10, 11, 12, 47]
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailAnyofInt>(f.Content) r
