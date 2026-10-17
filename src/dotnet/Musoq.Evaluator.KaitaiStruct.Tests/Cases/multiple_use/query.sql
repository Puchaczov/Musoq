binary KMultipleUseMulti {
    value: int le
};

binary KMultipleUseType1 {
    first_use: KMultipleUseMulti
};

binary KMultipleUseType2 {
    second_use: KMultipleUseMulti at 0
};

binary KMultipleUse {
    t1: KMultipleUseType1,
    t2: KMultipleUseType2
};

SELECT r.t1.first_use.value AS A0, r.t2.second_use.value AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KMultipleUse>(f.Content) r
