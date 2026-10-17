binary KValidFailEqBytes {
    foo: byte[2] const [0x51, 0x41]
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailEqBytes>(f.Content) r
