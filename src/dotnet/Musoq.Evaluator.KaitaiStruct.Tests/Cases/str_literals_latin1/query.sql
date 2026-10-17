binary KStrLiteralsLatin1 {
    len_parsed: ushort le,
    parsed: string[len_parsed] utf8,
    parsed_eq_literal: parsed = r'£'
};

SELECT r.parsed_eq_literal AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrLiteralsLatin1>(f.Content) r
