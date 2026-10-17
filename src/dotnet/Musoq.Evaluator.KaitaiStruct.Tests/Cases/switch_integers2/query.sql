binary KSwitchIntegers2 {
    code: byte,
    b1: byte when code = 1,
    b2: ushort le when code = 2,
    b4: uint le when code = 4,
    b8: ulong le when code = 8,
    len: = CASE WHEN code = 1 THEN b1 WHEN code = 2 THEN b2 WHEN code = 4 THEN b4 ELSE b8 END,
    ham: byte[len],
    padding: byte when len > 3,
    len_mod_str: = ToString(len * 2 - 1)
};


SELECT r.code AS A0, r.len AS A1, r.ham AS A2, r.padding AS A3, r.len_mod_str AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchIntegers2>(f.Content) r
