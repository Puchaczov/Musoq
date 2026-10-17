binary KSwitchManualIntOpcode {
    code: byte,
    integer: byte when code = 73,
    raw_text: byte repeat until raw_text[-1] = 0 when code = 83,
    decoded: = CASE WHEN code = 83 THEN ToText(raw_text, 'ascii') ELSE NULL END,
    text: = CASE WHEN code = 83 THEN Substring(decoded, 0, decoded.Length - 1) ELSE NULL END
};

binary KSwitchManualInt { opcodes: KSwitchManualIntOpcode repeat until eof };


SELECT r.opcodes.Length AS A0, r.opcodes[0].code AS A1, r.opcodes[0].text AS A2, r.opcodes[1].code AS A3, r.opcodes[1].integer AS A4, r.opcodes[2].code AS A5, r.opcodes[2].integer AS A6, r.opcodes[3].code AS A7, r.opcodes[3].text AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchManualInt>(f.Content) r
