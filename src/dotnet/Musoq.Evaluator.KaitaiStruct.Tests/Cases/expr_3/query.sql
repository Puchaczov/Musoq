binary KExpr3 {
    one: byte,
    two: string[3] ascii,
    three: = r'@' + two,
    four: = r'_' + two + r'_',
    is_str_eq: = two = r'ACK',
    is_str_ne: = two <> r'ACK',
    is_str_lt: = two < r'ACK2',
    is_str_gt: = two > r'ACK2',
    is_str_le: = two <= r'ACK2',
    is_str_ge: = two >= r'ACK2',
    is_str_lt2: = three < two,
    test_not: = NOT false
};

SELECT r.one AS A0, r.two AS A1, r.three AS A2, r.four AS A3, r.is_str_eq AS A4, r.is_str_ne AS A5, r.is_str_lt AS A6, r.is_str_gt AS A7, r.is_str_le AS A8, r.is_str_ge AS A9, r.is_str_lt2 AS A10, r.test_not AS A11
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExpr3>(f.Content) r
