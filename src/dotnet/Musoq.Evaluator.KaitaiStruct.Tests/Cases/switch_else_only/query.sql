binary KSwitchElseOnlyData {
    value: byte[4]
};

binary KSwitchElseOnly {
    opcode: sbyte,
    prim_byte: sbyte,
    indicator: byte[4],
    ut: KSwitchElseOnlyData
};


SELECT r.opcode AS A0, r.prim_byte AS A1, r.ut.value AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchElseOnly>(f.Content) r
