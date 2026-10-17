binary KExpr2Tuple {
    byte0: byte, byte1: byte, byte2: byte,
    avg: = (byte1 + byte2) / 2
};
binary KExpr2ModStr {
    len_orig: ushort le,
    len_mod: = len_orig - 3,
    str: string[len_mod] utf8,
    rest: substream[3] as KExpr2Tuple lax,
    char5: string[1] ascii at 5,
    tuple5: KExpr2Tuple at 5
};
binary KExpr2 {
    str1: KExpr2ModStr,
    str2: KExpr2ModStr at str1.len_orig + 2,
    str1_len: = str1.str.Length,
    str1_len_mod: = str1.len_mod,
    str1_byte1: = str1.rest.byte1,
    str1_avg: = str1.rest.avg,
    str1_char5: = str1.char5
};


SELECT r.str1.len_orig AS A0, r.str1.len_mod AS A1, r.str1.str AS A2, r.str1_len AS A3, r.str1_len_mod AS A4, r.str1_byte1 AS A5, r.str1_avg AS A6, r.str1_char5 AS A7, r.str1.tuple5.byte0 AS A8, r.str1.tuple5.byte0 AS A9, r.str1.tuple5.byte1 AS A10, r.str1.tuple5.byte2 AS A11, r.str1.tuple5.avg AS A12, r.str2.tuple5.byte0 AS A13, r.str2.tuple5.byte1 AS A14, r.str2.tuple5.byte2 AS A15, r.str2.tuple5.avg AS A16
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExpr2>(f.Content) r
