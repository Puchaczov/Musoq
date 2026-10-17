binary KEofExceptionU4 {
    prebuf: byte[9],
    fail_int: uint le
};

SELECT r.prebuf AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEofExceptionU4>(f.Content) r
