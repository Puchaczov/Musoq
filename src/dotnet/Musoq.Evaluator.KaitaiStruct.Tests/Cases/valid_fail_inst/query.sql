binary KValidFailInst {
    inst: byte const 80 at 5,
    a: byte at 0 when inst >= 0
};


SELECT r.a AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailInst>(f.Content) r
