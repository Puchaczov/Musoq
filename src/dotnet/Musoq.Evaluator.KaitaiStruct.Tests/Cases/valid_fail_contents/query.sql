binary KValidFailContents {
    foo: byte[2] magic [81, 65]
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailContents>(f.Content) r
