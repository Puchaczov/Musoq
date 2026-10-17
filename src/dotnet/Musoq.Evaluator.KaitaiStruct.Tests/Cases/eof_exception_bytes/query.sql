binary KEofExceptionBytes {
    buf: byte[13]
};

SELECT r.buf AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEofExceptionBytes>(f.Content) r
