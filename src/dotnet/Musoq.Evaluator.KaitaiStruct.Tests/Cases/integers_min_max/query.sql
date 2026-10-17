binary KIntegersMinMaxUnsigned {
    u1: byte,
    u2le: ushort le,
    u4le: uint le,
    u8le: ulong le,
    u2be: ushort be,
    u4be: uint be,
    u8be: ulong be
};

binary KIntegersMinMaxSigned {
    s1: sbyte,
    s2le: short le,
    s4le: int le,
    s8le: long le,
    s2be: short be,
    s4be: int be,
    s8be: long be
};

binary KIntegersMinMax {
    unsigned_min: KIntegersMinMaxUnsigned,
    unsigned_max: KIntegersMinMaxUnsigned,
    signed_min: KIntegersMinMaxSigned,
    signed_max: KIntegersMinMaxSigned
};

SELECT r.unsigned_min.u1 AS A0, r.unsigned_min.u2le AS A1, r.unsigned_min.u4le AS A2, r.unsigned_min.u8le AS A3, r.unsigned_min.u2be AS A4, r.unsigned_min.u4be AS A5, r.unsigned_min.u8be AS A6, r.unsigned_max.u1 AS A7, r.unsigned_max.u2le AS A8, r.unsigned_max.u4le AS A9, r.unsigned_max.u8le AS A10, r.unsigned_max.u2be AS A11, r.unsigned_max.u4be AS A12, r.unsigned_max.u8be AS A13, r.signed_min.s1 AS A14, r.signed_min.s2le AS A15, r.signed_min.s4le AS A16, r.signed_min.s8le AS A17, r.signed_min.s2be AS A18, r.signed_min.s4be AS A19, r.signed_min.s8be AS A20, r.signed_max.s1 AS A21, r.signed_max.s2le AS A22, r.signed_max.s4le AS A23, r.signed_max.s8le AS A24, r.signed_max.s2be AS A25, r.signed_max.s4be AS A26, r.signed_max.s8be AS A27
FROM #fixture.bytes() f
CROSS APPLY Interpret<KIntegersMinMax>(f.Content) r
