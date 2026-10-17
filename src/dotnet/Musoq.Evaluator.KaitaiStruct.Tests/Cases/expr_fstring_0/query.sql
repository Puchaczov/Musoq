binary KExprFstring0 {
    seq_str: string[5] ascii,
    seq_int: byte,
    empty: = '',
    literal: = 'abc',
    literal_with_escapes: = 'abc\n\tt',
    head_and_int_literal: = 'abc=' + ToString(123),
    head_and_str_literal: = 'abc=' + 'foo',
    head_and_int: = 'abc=' + ToString(seq_int),
    head_and_str: = 'abc=' + seq_str
};


SELECT r.seq_str AS A0, r.seq_int AS A1, r.empty AS A2, r.literal AS A3, r.literal_with_escapes AS A4, r.head_and_int_literal AS A5, r.head_and_str_literal AS A6, r.head_and_int AS A7, r.head_and_str AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprFstring0>(f.Content) r
