binary KStrLiterals {
    complex_str: = ToText(FromHex('000001000200070008000a000d0009000b000c001b003d0007000a0024003b26'), 'utf16le'),
    double_quotes: = ToText(FromHex('220022002200'), 'utf16le'),
    backslashes: = ToText(FromHex('5c005c005c00'), 'utf16le'),
    octal_eatup: = ToText(FromHex('000032003200'), 'utf16le'),
    octal_eatup2: = ToText(FromHex('02003200'), 'utf16le')
};


SELECT r.complex_str.Length AS A0_Length, f.ToInt32(r.complex_str[0]) AS A0_0, f.ToInt32(r.complex_str[1]) AS A0_1, f.ToInt32(r.complex_str[2]) AS A0_2, f.ToInt32(r.complex_str[3]) AS A0_3, f.ToInt32(r.complex_str[4]) AS A0_4, f.ToInt32(r.complex_str[5]) AS A0_5, f.ToInt32(r.complex_str[6]) AS A0_6, f.ToInt32(r.complex_str[7]) AS A0_7, f.ToInt32(r.complex_str[8]) AS A0_8, f.ToInt32(r.complex_str[9]) AS A0_9, f.ToInt32(r.complex_str[10]) AS A0_10, f.ToInt32(r.complex_str[11]) AS A0_11, f.ToInt32(r.complex_str[12]) AS A0_12, f.ToInt32(r.complex_str[13]) AS A0_13, f.ToInt32(r.complex_str[14]) AS A0_14, f.ToInt32(r.complex_str[15]) AS A0_15, r.double_quotes.Length AS A1_Length, f.ToInt32(r.double_quotes[0]) AS A1_0, f.ToInt32(r.double_quotes[1]) AS A1_1, f.ToInt32(r.double_quotes[2]) AS A1_2, r.backslashes.Length AS A2_Length, f.ToInt32(r.backslashes[0]) AS A2_0, f.ToInt32(r.backslashes[1]) AS A2_1, f.ToInt32(r.backslashes[2]) AS A2_2, r.octal_eatup.Length AS A3_Length, f.ToInt32(r.octal_eatup[0]) AS A3_0, f.ToInt32(r.octal_eatup[1]) AS A3_1, f.ToInt32(r.octal_eatup[2]) AS A3_2, r.octal_eatup2.Length AS A4_Length, f.ToInt32(r.octal_eatup2[0]) AS A4_0, f.ToInt32(r.octal_eatup2[1]) AS A4_1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrLiterals>(f.Content) r
