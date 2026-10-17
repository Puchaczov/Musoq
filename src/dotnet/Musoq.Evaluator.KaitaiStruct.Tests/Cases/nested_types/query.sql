binary KNestedTypesSubtypeASubtypeC {
    value_c: sbyte
};

binary KNestedTypesSubtypeB {
    value_b: sbyte
};

binary KNestedTypesSubtypeA {
    typed_at_root: KNestedTypesSubtypeB,
    typed_here: KNestedTypesSubtypeASubtypeC
};

binary KNestedTypes {
    one: KNestedTypesSubtypeA,
    two: KNestedTypesSubtypeB
};

SELECT r.one.typed_at_root.value_b AS A0, r.one.typed_here.value_c AS A1, r.two.value_b AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNestedTypes>(f.Content) r
