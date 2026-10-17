binary KEosExceptionU4Data {
    prebuf: byte[3],
    fail_int: uint le
};

binary KEosExceptionU4 {
    envelope: substream[6] as KEosExceptionU4Data lax
};

SELECT r.envelope AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEosExceptionU4>(f.Content) r
