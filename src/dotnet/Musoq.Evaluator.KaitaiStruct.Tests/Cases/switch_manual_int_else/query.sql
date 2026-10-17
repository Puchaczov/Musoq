binary KSwitchManualIntElseOpcode {
    code: byte,
    integer: byte when code = 73,
    raw_text: byte repeat until raw_text[-1] = 0 when code = 83,
    decoded: = CASE WHEN code = 83 THEN ToText(raw_text, 'ascii') ELSE NULL END,
    text: = CASE WHEN code = 83 THEN Substring(decoded, 0, decoded.Length - 1) ELSE NULL END,
    filler: uint le when code <> 73 AND code <> 83
};

binary KSwitchManualIntElse { opcodes: KSwitchManualIntElseOpcode repeat until eof };


SELECT r.opcodes.Length AS A0, r.opcodes[0].code AS A1, r.opcodes[0].text AS A2, r.opcodes[1].code AS A3, r.opcodes[1].filler AS A4, r.opcodes[2].code AS A5, r.opcodes[2].filler AS A6, r.opcodes[3].code AS A7, r.opcodes[3].integer AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchManualIntElse>(f.Content) r
