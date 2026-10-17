binary KUserTypeHeader {
    width: uint le,
    height: uint le
};

binary KUserType {
    one: KUserTypeHeader
};

SELECT r.one.width AS A0, r.one.height AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KUserType>(f.Content) r
