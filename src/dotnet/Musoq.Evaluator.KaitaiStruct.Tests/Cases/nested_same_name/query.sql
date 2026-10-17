binary KNestedSameNameMainFooObj {
    data: byte[parent.main_size * 2]
};

binary KNestedSameNameMain {
    main_size: int le,
    foo: KNestedSameNameMainFooObj
};

binary KNestedSameNameDummyObjFoo {
};

binary KNestedSameNameDummyObj {
};

binary KNestedSameName {
    main_data: KNestedSameNameMain,
    dummy: KNestedSameNameDummyObj
};

SELECT r.main_data.main_size AS A0, r.main_data.foo.data AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNestedSameName>(f.Content) r
