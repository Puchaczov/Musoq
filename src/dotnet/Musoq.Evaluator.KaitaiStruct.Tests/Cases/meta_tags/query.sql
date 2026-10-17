binary KMetaTags {
};

SELECT 1 AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KMetaTags>(f.Content) r
