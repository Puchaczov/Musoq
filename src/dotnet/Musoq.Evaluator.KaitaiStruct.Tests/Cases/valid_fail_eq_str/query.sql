binary KValidFailEqStr {
    foo: string[4] ascii const r'BACK'
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailEqStr>(f.Content) r
