binary KValidFailRepeatExprValidatedElement {
    value: byte[4] check ToHex(value) <> r'00123456'
};

binary KValidFailRepeatExpr {
    foo: KValidFailRepeatExprValidatedElement repeat until eof
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatExpr>(f.Content) r
