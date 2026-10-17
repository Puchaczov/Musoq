binary KSwitchIntegersOpcode {
    code: byte,
    b1: byte when code = 1,
    b2: ushort le when code = 2,
    b4: uint le when code = 4,
    b8: ulong le when code = 8
};

binary KSwitchIntegers {
    opcodes: KSwitchIntegersOpcode repeat until eof
};


SELECT r.opcodes.Length AS A0, r.opcodes[0].code AS A1, CASE WHEN r.opcodes[0].code = 1 THEN r.opcodes[0].b1 WHEN r.opcodes[0].code = 2 THEN r.opcodes[0].b2 WHEN r.opcodes[0].code = 4 THEN r.opcodes[0].b4 ELSE r.opcodes[0].b8 END AS A2, r.opcodes[1].code AS A3, CASE WHEN r.opcodes[1].code = 1 THEN r.opcodes[1].b1 WHEN r.opcodes[1].code = 2 THEN r.opcodes[1].b2 WHEN r.opcodes[1].code = 4 THEN r.opcodes[1].b4 ELSE r.opcodes[1].b8 END AS A4, r.opcodes[2].code AS A5, CASE WHEN r.opcodes[2].code = 1 THEN r.opcodes[2].b1 WHEN r.opcodes[2].code = 2 THEN r.opcodes[2].b2 WHEN r.opcodes[2].code = 4 THEN r.opcodes[2].b4 ELSE r.opcodes[2].b8 END AS A6, r.opcodes[3].code AS A7, CASE WHEN r.opcodes[3].code = 1 THEN r.opcodes[3].b1 WHEN r.opcodes[3].code = 2 THEN r.opcodes[3].b2 WHEN r.opcodes[3].code = 4 THEN r.opcodes[3].b4 ELSE r.opcodes[3].b8 END AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchIntegers>(f.Content) r
