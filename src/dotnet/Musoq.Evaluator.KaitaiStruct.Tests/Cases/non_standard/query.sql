-- Kaitai vendor keys beginning with '-' are metadata, with no decoding effect.
binary KNonStandard {
    foo: byte,
    bar_short: ushort le when foo = 42,
    bar_long: uint le when foo = 43,
    vi: = foo,
    pi: byte at 0
};


SELECT r.foo AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNonStandard>(f.Content) r
