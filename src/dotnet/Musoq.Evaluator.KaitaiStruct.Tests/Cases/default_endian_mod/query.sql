binary KDefaultEndianModMainObjSubnest {
    two: int le
};

binary KDefaultEndianModMainObjSubnestBe {
    two: int be
};

binary KDefaultEndianModMainObj {
    one: int le,
    nest: KDefaultEndianModMainObjSubnest,
    nest_be: KDefaultEndianModMainObjSubnestBe
};

binary KDefaultEndianMod {
    main: KDefaultEndianModMainObj
};

SELECT r.main.one AS A0, r.main.nest.two AS A1, r.main.nest_be.two AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultEndianMod>(f.Content) r
