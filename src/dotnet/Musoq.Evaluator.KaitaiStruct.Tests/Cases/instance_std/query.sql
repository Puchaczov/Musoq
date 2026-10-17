binary KInstanceStd {
    header: string[5] ascii at 2
};

SELECT r.header AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KInstanceStd>(f.Content) r
