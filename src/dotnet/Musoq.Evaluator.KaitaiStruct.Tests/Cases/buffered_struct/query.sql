binary KBufferedStructBlock {
    number1: uint le,
    number2: uint le
};

binary KBufferedStruct {
    len1: uint le,
    block1: substream[len1] as KBufferedStructBlock lax,
    len2: uint le,
    block2: substream[len2] as KBufferedStructBlock lax,
    finisher: uint le
};

SELECT r.len1 AS A0, r.block1.number1 AS A1, r.block1.number2 AS A2, r.len2 AS A3, r.block2.number1 AS A4, r.block2.number2 AS A5, r.finisher AS A6
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBufferedStruct>(f.Content) r
