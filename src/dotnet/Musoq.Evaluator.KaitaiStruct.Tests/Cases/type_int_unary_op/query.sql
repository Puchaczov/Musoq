binary KTypeIntUnaryOp {
    value_s2: short le,
    value_s8: long le,
    unary_s2: = -value_s2,
    unary_s8: = -value_s8
};

SELECT r.value_s2 AS A0, r.value_s8 AS A1, r.unary_s2 AS A2, r.unary_s8 AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KTypeIntUnaryOp>(f.Content) r
