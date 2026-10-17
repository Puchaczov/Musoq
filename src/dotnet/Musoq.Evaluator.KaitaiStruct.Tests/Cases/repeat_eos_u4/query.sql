binary KRepeatEosU4 {
    numbers: uint le repeat until eof
};

SELECT r.numbers.Length AS A0, r.numbers[0] AS A1, r.numbers[1] AS A2, r.numbers[2] AS A3, r.numbers[3] AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosU4>(f.Content) r
