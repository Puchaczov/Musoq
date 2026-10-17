binary KBitsByteAlignedEofBe {
    prebuf: byte[8],
    raw: uint be,
    bits: = raw >> 1
};


SELECT r.prebuf AS A0, r.bits AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsByteAlignedEofBe>(f.Content) r
