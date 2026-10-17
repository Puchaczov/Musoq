binary KValidSwitch {
    a: byte const 80,
    little: ushort le const 17217 when a = 80,
    big: ushort be const 17217 when a <> 80,
    b: = CASE WHEN a = 80 THEN little ELSE big END
};


SELECT r.a AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidSwitch>(f.Content) r
