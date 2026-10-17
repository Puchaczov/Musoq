binary KValidFailRepeatMinIntValidatedElement {
    value: sbyte check value >= 0
};

binary KValidFailRepeatMinInt {
    foo: KValidFailRepeatMinIntValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatMinInt>(f.Content) r
