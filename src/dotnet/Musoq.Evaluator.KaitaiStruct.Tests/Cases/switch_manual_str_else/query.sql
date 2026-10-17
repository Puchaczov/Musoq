binary KSwitchManualStrElseOpcode {
    code: string[1] ascii,
    integer: byte when code = 'I',
    raw_text: byte repeat until raw_text[-1] = 0 when code = 'S',
    decoded: = CASE WHEN code = 'S' THEN ToText(raw_text, 'ascii') ELSE NULL END,
    text: = CASE WHEN code = 'S' THEN Substring(decoded, 0, decoded.Length - 1) ELSE NULL END,
    filler: uint le when code <> 'I' AND code <> 'S'
};

binary KSwitchManualStrElse { opcodes: KSwitchManualStrElseOpcode repeat until eof };


SELECT r.opcodes.Length AS A0, r.opcodes[0].code AS A1, r.opcodes[0].text AS A2, r.opcodes[1].code AS A3, r.opcodes[1].filler AS A4, r.opcodes[2].code AS A5, r.opcodes[2].filler AS A6, r.opcodes[3].code AS A7, r.opcodes[3].integer AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchManualStrElse>(f.Content) r
