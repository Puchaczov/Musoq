binary KHelloWorld {
    one: byte
};

binary KImports0 {
    two: byte,
    hw: KHelloWorld,
    hw_one: hw.one
};

SELECT r.two AS A0, r.hw.one AS A1, r.hw_one AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KImports0>(f.Content) r
