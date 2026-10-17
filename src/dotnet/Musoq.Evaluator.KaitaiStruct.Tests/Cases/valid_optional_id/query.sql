binary KValidOptionalId {
    discard_0: byte[6] magic [80, 65, 67, 75, 45, 49],
    discard_1: byte const 255,
    discard_2: sbyte check discard_2 = -1
};


SELECT r.discard_0 AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KValidOptionalId>(f.Content) r
