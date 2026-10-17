binary KValidFailContentsInst {
    foo: byte[2] magic [81, 65] at 0,
    a: byte[0] at 0 when foo.Length = 0
};


SELECT r.a AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailContentsInst>(f.Content) r
