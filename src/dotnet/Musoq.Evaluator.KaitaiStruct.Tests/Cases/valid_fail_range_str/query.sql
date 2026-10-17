binary KValidFailRangeStr {
    foo: string[2] ascii check foo >= r'P' AND foo <= r'P1'
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRangeStr>(f.Content) r
