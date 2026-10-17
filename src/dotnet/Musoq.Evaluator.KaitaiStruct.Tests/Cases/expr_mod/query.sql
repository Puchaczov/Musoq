-- Kaitai's modulo with positive divisor is non-negative. Core % is remainder.
binary KExprMod {
    int_u: uint le,
    int_s: int le,
    mod_pos_const: = 9837 % 13,
    mod_neg_const: = ((-9837 % 13) + 13) % 13,
    mod_pos_seq: = int_u % 13,
    mod_neg_seq: = ((int_s % 13) + 13) % 13
};


SELECT r.int_u AS A0, r.int_s AS A1, r.mod_pos_const AS A2, r.mod_neg_const AS A3, r.mod_pos_seq AS A4, r.mod_neg_seq AS A5
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprMod>(f.Content) r
