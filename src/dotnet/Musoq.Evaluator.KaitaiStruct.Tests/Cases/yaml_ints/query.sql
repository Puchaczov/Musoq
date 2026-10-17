binary KYamlInts {
    test_u4_dec: 4294967295ui,
    test_u4_hex: 4294967295ui,
    test_u8_dec: 18446744073709551615ul,
    test_u8_hex: 18446744073709551615ul
};

SELECT r.test_u4_dec AS A0, r.test_u4_hex AS A1, r.test_u8_dec AS A2, r.test_u8_hex AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KYamlInts>(f.Content) r
