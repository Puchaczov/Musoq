binary KValidFailRepeatAnyofIntValidatedElement {
    value: byte oneOf [0, 1, 65]
};

binary KValidFailRepeatAnyofInt {
    foo: KValidFailRepeatAnyofIntValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatAnyofInt>(f.Content) r
