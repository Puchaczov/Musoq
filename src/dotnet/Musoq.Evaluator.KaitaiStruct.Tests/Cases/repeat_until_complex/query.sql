binary KRepeatUntilComplexTypeU1 {
    count: byte,
    values: byte[count]
};

binary KRepeatUntilComplexTypeU2 {
    count: ushort le,
    values: ushort le[count]
};

binary KRepeatUntilComplex {
    first: KRepeatUntilComplexTypeU1 repeat until first[-1].count = 0,
    second: KRepeatUntilComplexTypeU2 repeat until second[-1].count = 0,
    third: byte repeat until third[-1] = 0
};

SELECT r.first.Length AS A0, r.first[0].count AS A1, r.first[0].values AS A2, r.first[1].count AS A3, r.first[1].values AS A4, r.first[2].count AS A5, r.second.Length AS A6, r.second[0].count AS A7, r.second[0].values AS A8, r.second[1].count AS A9, r.second[1].values AS A10, r.second[2].count AS A11, r.second[2].values AS A12, r.second[3].count AS A13, r.third AS A14
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatUntilComplex>(f.Content) r
