binary KExprStrOps {
    one: string[5] ascii,
    two: = '0123456789',
    input_decimal: = '9173',
    input_signed: = '-072',
    input_binary: = '1010110',
    input_octal: = '721',
    input_hex: = '47cf'
};


SELECT r.one AS A0, r.one.Length AS A1, f.Reverse(r.one) AS A2, f.Substring(r.one, 0, 3 - 0) AS A3, f.Substring(r.one, 2, 5 - 2) AS A4, f.Substring(r.one, 3, 3 - 3) AS A5, f.Substring(r.one, 0, 0 - 0) AS A6, r.two AS A7, r.two.Length AS A8, f.Reverse(r.two) AS A9, f.Substring(r.two, 0, 7 - 0) AS A10, f.Substring(r.two, 4, 10 - 4) AS A11, f.Substring(r.two, 0, 10 - 0) AS A12, f.ToInt32(r.input_decimal) AS A13, f.ToInt32(r.input_signed) AS A14, f.FromBin(r.input_binary) AS A15, f.FromOct(r.input_octal) AS A16, f.FromHex(r.input_hex) AS A17
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprStrOps>(f.Content) r
