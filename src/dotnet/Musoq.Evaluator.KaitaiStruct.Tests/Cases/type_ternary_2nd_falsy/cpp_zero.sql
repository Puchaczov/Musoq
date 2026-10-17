binary KTypeTernary2NdFalsyFoo { m: byte };
binary KTypeTernary2NdFalsy {
    int_truthy: byte,
    ut: KTypeTernary2NdFalsyFoo,
    int_array: byte[2],
    int_array_empty: byte[0],
    t: = true,
    null_ut: = CASE WHEN false THEN ut ELSE NULL END,
    v_false: = CASE WHEN t THEN false ELSE true END,
    v_int_zero: = CASE WHEN t THEN 0 ELSE 10 END,
    v_int_neg_zero: = CASE WHEN t THEN -0 ELSE -20 END,
    v_float_zero: = CASE WHEN t THEN 0.0 ELSE 3.14 END,
    v_float_neg_zero: = CASE WHEN t THEN -0.0 ELSE -2.72 END,
    v_str_w_zero: = CASE WHEN t THEN '0' ELSE '30' END,
    v_null_ut: = CASE WHEN t THEN null_ut ELSE ut END,
    v_str_empty: = CASE WHEN t THEN '' ELSE 'kaitai' END,
    v_int_array_empty: = CASE WHEN t THEN int_array_empty ELSE int_array END
};


SELECT r.v_float_zero AS A0, r.v_float_neg_zero AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTypeTernary2NdFalsy>(f.Content) r
