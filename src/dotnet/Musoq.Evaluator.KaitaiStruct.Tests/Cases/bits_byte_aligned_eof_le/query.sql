binary KBitsByteAlignedEofLe {
    prebuf: byte[8],
    bits: bits[31]
};

SELECT r.prebuf AS A0, r.bits AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsByteAlignedEofLe>(f.Content) r
