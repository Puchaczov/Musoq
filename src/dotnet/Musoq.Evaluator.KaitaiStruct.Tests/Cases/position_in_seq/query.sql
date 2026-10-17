binary KPositionInSeq {
    qty_numbers: uint le at 16,
    numbers: byte[qty_numbers] at 0
};


SELECT r.numbers AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KPositionInSeq>(f.Content) r
