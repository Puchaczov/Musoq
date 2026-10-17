binary KDocstringsDocrefMulti {
};

SELECT 1 AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDocstringsDocrefMulti>(f.Content) r
