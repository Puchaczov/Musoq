binary KValidFailRepeatContentsValidatedElement {
    value: byte[4] magic [18, 52, 86, 120]
};

binary KValidFailRepeatContents {
    foo: KValidFailRepeatContentsValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatContents>(f.Content) r
