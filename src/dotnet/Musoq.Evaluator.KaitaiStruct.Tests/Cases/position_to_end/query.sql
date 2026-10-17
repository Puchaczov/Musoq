binary KPositionToEnd {
    foo: uint le,
    bar: uint le
};

SELECT r.foo AS A0, r.bar AS A1
FROM #fixture.bytes() f
CROSS APPLY InterpretAt<KPositionToEnd>(f.Content, f.Content.Length - 8) r
