binary KIfValuesCode {
    opcode: byte,
    half_opcode: = CASE WHEN opcode % 2 = 0 THEN opcode / 2 ELSE NULL END
};

binary KIfValues {
    codes: KIfValuesCode[3]
};

SELECT r.codes[0].opcode AS A0, r.codes[0].half_opcode AS A1, r.codes[1].opcode AS A2, r.codes[1].half_opcode AS A3, r.codes[2].opcode AS A4, r.codes[2].half_opcode AS A5
FROM #fixture.bytes() f
CROSS APPLY Interpret<KIfValues>(f.Content) r
