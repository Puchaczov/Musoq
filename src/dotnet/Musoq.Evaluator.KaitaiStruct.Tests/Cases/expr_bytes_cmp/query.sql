binary KExprBytesCmp {
    one: byte[1],
    two: byte[3],
    ack: = FromHex('41434B'),
    ack2: = FromHex('41434B32'),
    hi_val: = FromHex('9043'),
    is_eq: = ToHex(two) = ToHex(ack),
    is_ne: = ToHex(two) <> ToHex(ack),
    is_lt: = ToHex(two) < ToHex(ack2),
    is_gt: = ToHex(two) > ToHex(ack2),
    is_le: = ToHex(two) <= ToHex(ack2),
    is_ge: = ToHex(two) >= ToHex(ack2),
    is_lt2: = ToHex(one) < ToHex(two),
    is_gt2: = ToHex(hi_val) > ToHex(two)
};


SELECT r.one AS A0, r.two AS A1, r.is_eq AS A2, r.is_ne AS A3, r.is_lt AS A4, r.is_gt AS A5, r.is_le AS A6, r.is_ge AS A7, r.is_lt2 AS A8, r.is_gt2 AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprBytesCmp>(f.Content) r
