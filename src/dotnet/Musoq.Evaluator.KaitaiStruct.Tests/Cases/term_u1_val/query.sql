binary KTermU1Val {
    raw_foo: byte repeat until raw_foo[-1] = 227,
    foo: byte[raw_foo.Length - 1] at 0,
    raw_bar: byte repeat until raw_bar[-1] = 171,
    bar: = ToText(raw_bar, 'utf8')
};


SELECT r.foo AS A0, r.bar AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTermU1Val>(f.Content) r
