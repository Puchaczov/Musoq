binary KValidFailRepeatMaxIntValidatedElement {
    value: byte check value <= 254
};

binary KValidFailRepeatMaxInt {
    foo: KValidFailRepeatMaxIntValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatMaxInt>(f.Content) r
