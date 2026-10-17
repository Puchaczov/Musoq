binary KEofExceptionSizedFoo {
};

binary KEofExceptionSized {
    buf: substream[13] as KEofExceptionSizedFoo lax
};

SELECT r.buf AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEofExceptionSized>(f.Content) r
