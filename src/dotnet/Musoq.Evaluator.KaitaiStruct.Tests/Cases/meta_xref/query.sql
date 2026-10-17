binary KMetaXref {
};

SELECT 1 AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KMetaXref>(f.Content) r
