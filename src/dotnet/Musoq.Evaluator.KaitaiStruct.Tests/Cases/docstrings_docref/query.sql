binary KDocstringsDocref {
    one: byte,
    two: byte,
    three: byte,
    foo: = true,
    parse_inst: byte at 0
};

SELECT r.one AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDocstringsDocref>(f.Content) r
