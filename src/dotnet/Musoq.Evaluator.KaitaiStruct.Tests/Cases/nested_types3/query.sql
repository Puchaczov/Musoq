binary KNestedTypes3SubtypeASubtypeCSubtypeD {
    value_d: sbyte
};

binary KNestedTypes3SubtypeASubtypeC {
};

binary KNestedTypes3SubtypeASubtypeCc {
    value_cc: sbyte
};

binary KNestedTypes3SubtypeA {
};

binary KNestedTypes3SubtypeB {
    value_b: sbyte,
    a_cc: KNestedTypes3SubtypeASubtypeCc,
    a_c_d: KNestedTypes3SubtypeASubtypeCSubtypeD
};

binary KNestedTypes3 {
    a_cc: KNestedTypes3SubtypeASubtypeCc,
    a_c_d: KNestedTypes3SubtypeASubtypeCSubtypeD,
    b: KNestedTypes3SubtypeB
};

SELECT r.a_cc.value_cc AS A0, r.a_c_d.value_d AS A1, r.b.value_b AS A2, r.b.a_cc.value_cc AS A3, r.b.a_c_d.value_d AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNestedTypes3>(f.Content) r
