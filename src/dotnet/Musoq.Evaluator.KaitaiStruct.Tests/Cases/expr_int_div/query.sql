-- Kaitai integer division floors; Core integral division truncates toward zero.
-- Preserve Kaitai's contract explicitly for negative, non-divisible numerators.
binary KExprIntDiv {
    int_u: uint le,
    int_s: int le,
    div_pos_const: = 9837 / 13,
    div_neg_const: = (-9837 / 13) - 1,
    div_pos_seq: = int_u / 13,
    div_neg_seq: = CASE WHEN int_s < 0 AND int_s % 13 <> 0 THEN (int_s / 13) - 1 ELSE int_s / 13 END
};


SELECT r.int_u AS A0, r.int_s AS A1, r.div_pos_const AS A2, r.div_neg_const AS A3, r.div_pos_seq AS A4, r.div_neg_seq AS A5
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprIntDiv>(f.Content) r
