binary KValidNotParsedIf {
    not_parsed: byte const 42 when false,
    parsed: byte const 80 when true
};


SELECT r.not_parsed AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidNotParsedIf>(f.Content) r
