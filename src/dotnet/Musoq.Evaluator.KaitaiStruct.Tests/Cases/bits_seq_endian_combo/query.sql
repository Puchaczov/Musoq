binary KBitsSeqEndianCombo {
    first: ushort be,
    be1: = first >> 10,
    be2: = first & 1023,
    le3: byte,
    be4: byte,
    little: ushort le,
    le5: = little & 31,
    le6: = (little >> 5) & 63,
    le7: = little >> 11,
    final: byte,
    be8: = (final & 128) <> 0
};


SELECT r.be1 AS A0, r.be2 AS A1, r.le3 AS A2, r.be4 AS A3, r.le5 AS A4, r.le6 AS A5, r.le7 AS A6, r.be8 AS A7
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsSeqEndianCombo>(f.Content) r
