binary KDocstringsComplexSubtype {
};

binary KDocstrings {
    one: byte,
    two: byte at 0,
    three: = 66
};

SELECT r.one AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDocstrings>(f.Content) r
