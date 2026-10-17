binary KDefaultBigEndian {
    one: uint be
};

SELECT r.one AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultBigEndian>(f.Content) r
