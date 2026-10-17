binary KValidFailRepeatEqIntValidatedElement {
    value: uint be const 305419896
};

binary KValidFailRepeatEqInt {
    foo: KValidFailRepeatEqIntValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatEqInt>(f.Content) r
