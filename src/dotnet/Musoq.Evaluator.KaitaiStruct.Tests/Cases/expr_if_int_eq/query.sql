binary KExprIfIntEq {
    skip: byte[2],
    seq: short le,
    seq_if: short le when true,
    calc: = 16705,
    calc_if: = CASE WHEN true THEN 16705 ELSE NULL END,
    seq_eq_lit: = seq = 0x4141,
    seq_eq_calc: = seq = calc,
    seq_eq_calc_if: = seq = calc_if,
    seq_eq_seq_if: = seq = seq_if,
    calc_eq_lit: = calc = 0x4141,
    calc_eq_calc_if: = calc = calc_if,
    calc_eq_seq_if: = calc = seq_if,
    calc_if_eq_lit: = calc_if = 0x4141,
    calc_if_eq_seq_if: = calc_if = seq_if,
    seq_if_eq_lit: = seq_if = 0x4141
};

SELECT r.seq_eq_lit AS A0, r.seq_eq_calc AS A1, r.seq_eq_calc_if AS A2, r.seq_eq_seq_if AS A3, r.calc_eq_lit AS A4, r.calc_eq_calc_if AS A5, r.calc_eq_seq_if AS A6, r.calc_if_eq_lit AS A7, r.calc_if_eq_seq_if AS A8, r.seq_if_eq_lit AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprIfIntEq>(f.Content) r
