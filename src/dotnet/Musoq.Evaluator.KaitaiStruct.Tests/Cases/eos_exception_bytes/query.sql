binary KEosExceptionBytesData {
    buf: byte[7]
};

binary KEosExceptionBytes {
    envelope: substream[6] as KEosExceptionBytesData lax
};

SELECT r.envelope AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEosExceptionBytes>(f.Content) r
