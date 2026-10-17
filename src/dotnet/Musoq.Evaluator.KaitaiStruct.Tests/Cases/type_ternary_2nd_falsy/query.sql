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


SELECT r.v_false AS A0, r.v_int_zero AS A1, r.v_int_neg_zero AS A2, r.v_float_zero AS A3, r.v_float_neg_zero AS A4, r.v_str_w_zero AS A5, r.v_str_w_zero.Length AS A6, r.ut.m AS A7, r.v_null_ut AS A8, r.v_str_empty AS A9, r.v_str_empty.Length AS A10, r.int_array.Length AS A11, r.v_int_array_empty.Length AS A12
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTypeTernary2NdFalsy>(f.Content) r
