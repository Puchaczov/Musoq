binary KValidFailRepeatInstElement { value: uint be const 305419896 };
binary KValidFailRepeatInst {
    inst: KValidFailRepeatInstElement repeat until eof,
    a: byte[0] at 0 when inst.Length = 0
};


SELECT r.a AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidFailRepeatInst>(f.Content) r
