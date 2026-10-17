binary KCombineStr {
    term_raw: byte repeat until term_raw[-1] = 124,
    str_term: string[term_raw.Length - 1] ascii at 0,
    str_limit: string[4] ascii at term_raw.Length,
    eos_raw: byte repeat until eof,
    str_eos: = ToText(eos_raw, 'ascii'),
    str_calc: = 'bar',
    calc_bytes: = FromHex('62617A'),
    str_calc_bytes: = ToText(calc_bytes, 'ascii'),
    term_or_limit: = CASE WHEN true THEN str_term ELSE str_limit END,
    term_or_eos: = CASE WHEN false THEN str_term ELSE str_eos END,
    term_or_calc: = CASE WHEN true THEN str_term ELSE str_calc END,
    term_or_calc_bytes: = CASE WHEN false THEN str_term ELSE str_calc_bytes END,
    limit_or_eos: = CASE WHEN true THEN str_limit ELSE str_eos END,
    limit_or_calc: = CASE WHEN false THEN str_limit ELSE str_calc END,
    limit_or_calc_bytes: = CASE WHEN true THEN str_limit ELSE str_calc_bytes END,
    eos_or_calc: = CASE WHEN false THEN str_eos ELSE str_calc END,
    eos_or_calc_bytes: = CASE WHEN true THEN str_eos ELSE str_calc_bytes END,
    calc_or_calc_bytes: = CASE WHEN false THEN str_calc ELSE str_calc_bytes END
};


SELECT r.str_term AS A0, r.str_limit AS A1, r.str_eos AS A2, r.str_calc AS A3, r.str_calc_bytes AS A4, r.term_or_limit AS A5, r.term_or_eos AS A6, r.term_or_calc AS A7, r.term_or_calc_bytes AS A8, r.limit_or_eos AS A9, r.limit_or_calc AS A10, r.limit_or_calc_bytes AS A11, r.eos_or_calc AS A12, r.eos_or_calc_bytes AS A13, r.calc_or_calc_bytes AS A14
FROM #fixture.bytes() f
CROSS APPLY Interpret<KCombineStr>(f.Content) r
