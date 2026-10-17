binary KValidFailEqInt {
    foo: byte const 123
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailEqInt>(f.Content) r
