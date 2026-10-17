binary KEosExceptionSizedFoo {
};

binary KEosExceptionSizedData {
    buf: substream[7] as KEosExceptionSizedFoo lax
};

binary KEosExceptionSized {
    envelope: substream[6] as KEosExceptionSizedData lax
};

SELECT r.envelope AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEosExceptionSized>(f.Content) r
