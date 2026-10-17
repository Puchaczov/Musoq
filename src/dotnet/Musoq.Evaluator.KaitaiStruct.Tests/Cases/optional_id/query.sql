binary KOptionalId {
    discard_0: byte,
    discard_1: byte,
    discard_2: byte[5]
};

SELECT r.discard_0 AS A0, r.discard_1 AS A1, r.discard_2 AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KOptionalId>(f.Content) r
