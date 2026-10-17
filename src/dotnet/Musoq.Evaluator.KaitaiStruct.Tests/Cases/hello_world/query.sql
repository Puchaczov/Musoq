binary KHelloWorld {
    one: byte
};

SELECT r.one AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KHelloWorld>(f.Content) r
