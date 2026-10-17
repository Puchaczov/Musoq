binary KNestedSameName2MainFooObj {
    data1: byte[parent.main_size * 2]
};

binary KNestedSameName2Main {
    main_size: int le,
    foo: KNestedSameName2MainFooObj
};

binary KNestedSameName2DummyObjFooObj {
    data2: byte[parent.dummy_size * 2]
};

binary KNestedSameName2DummyObj {
    dummy_size: int le,
    foo: KNestedSameName2DummyObjFooObj
};

binary KNestedSameName2 {
    version: uint le,
    main_data: KNestedSameName2Main,
    dummy: KNestedSameName2DummyObj
};

SELECT r.version AS A0, r.main_data.main_size AS A1, r.main_data.foo.data1 AS A2, r.dummy.dummy_size AS A3, r.dummy.foo.data2 AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNestedSameName2>(f.Content) r
