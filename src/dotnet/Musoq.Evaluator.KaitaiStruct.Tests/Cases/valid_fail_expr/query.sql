binary KValidFailExpr {
    foo: byte check foo = 1,
    bar: short le check bar < -190 OR bar > -190
};


SELECT r.foo AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailExpr>(f.Content) r
