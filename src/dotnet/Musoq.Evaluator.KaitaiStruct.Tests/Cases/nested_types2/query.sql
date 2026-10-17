binary KNestedTypes2SubtypeASubtypeCSubtypeD {
    value_d: sbyte
};

binary KNestedTypes2SubtypeASubtypeCc {
    value_cc: sbyte
};

binary KNestedTypes2SubtypeB {
    value_b: sbyte
};

binary KNestedTypes2SubtypeASubtypeC {
    value_c: sbyte,
    typed_here: KNestedTypes2SubtypeASubtypeCSubtypeD,
    typed_parent: KNestedTypes2SubtypeASubtypeCc,
    typed_root: KNestedTypes2SubtypeB
};

binary KNestedTypes2SubtypeA {
    typed_at_root: KNestedTypes2SubtypeB,
    typed_here1: KNestedTypes2SubtypeASubtypeC,
    typed_here2: KNestedTypes2SubtypeASubtypeCc
};

binary KNestedTypes2 {
    one: KNestedTypes2SubtypeA,
    two: KNestedTypes2SubtypeB
};

SELECT r.one.typed_at_root.value_b AS A0, r.one.typed_here1.value_c AS A1, r.one.typed_here1.typed_here.value_d AS A2, r.one.typed_here1.typed_parent.value_cc AS A3, r.one.typed_here1.typed_root.value_b AS A4, r.one.typed_here2.value_cc AS A5, r.two.value_b AS A6
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNestedTypes2>(f.Content) r
