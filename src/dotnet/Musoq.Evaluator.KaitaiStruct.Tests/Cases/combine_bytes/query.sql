binary KCombineBytes {
    term_raw: byte repeat until term_raw[-1] = 124,
    bytes_term: byte[term_raw.Length - 1] at 0,
    bytes_limit: byte[4] at term_raw.Length,
    bytes_eos: byte repeat until eof,
    bytes_calc: = FromHex('526E44'),
    term_or_limit: = CASE WHEN true THEN bytes_term ELSE bytes_limit END,
    term_or_eos: = CASE WHEN false THEN bytes_term ELSE bytes_eos END,
    term_or_calc: = CASE WHEN true THEN bytes_term ELSE bytes_calc END,
    limit_or_eos: = CASE WHEN true THEN bytes_limit ELSE bytes_eos END,
    limit_or_calc: = CASE WHEN false THEN bytes_limit ELSE bytes_calc END,
    eos_or_calc: = CASE WHEN true THEN bytes_eos ELSE bytes_calc END
};


SELECT r.bytes_term AS A0, r.bytes_limit AS A1, r.bytes_eos AS A2, r.bytes_calc AS A3, r.term_or_limit AS A4, r.term_or_eos AS A5, r.term_or_calc AS A6, r.limit_or_eos AS A7, r.limit_or_calc AS A8, r.eos_or_calc AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KCombineBytes>(f.Content) r
