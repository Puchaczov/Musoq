binary KExprBytesOps {
    one: byte[3],
    two: = FromHex('41FF4B'),
    one_size: = one.Length,
    one_first: = one[0],
    one_mid: = one[1],
    one_last: = one[2],
    one_min: = CASE WHEN one[0] <= one[1] AND one[0] <= one[2] THEN one[0] WHEN one[1] <= one[2] THEN one[1] ELSE one[2] END,
    one_max: = CASE WHEN one[0] >= one[1] AND one[0] >= one[2] THEN one[0] WHEN one[1] >= one[2] THEN one[1] ELSE one[2] END,
    two_size: = two.Length,
    two_first: = two[0],
    two_mid: = two[1],
    two_last: = two[2],
    two_min: = CASE WHEN two[0] <= two[1] AND two[0] <= two[2] THEN two[0] WHEN two[1] <= two[2] THEN two[1] ELSE two[2] END,
    two_max: = CASE WHEN two[0] >= two[1] AND two[0] >= two[2] THEN two[0] WHEN two[1] >= two[2] THEN two[1] ELSE two[2] END
};


SELECT r.one_size AS A0, r.one_first AS A1, r.one_mid AS A2, r.one_last AS A3, f.ToString(r.one_last) AS A4, r.one_min AS A5, r.one_max AS A6, f.ToString(r.one_max) AS A7, r.two_size AS A8, r.two_first AS A9, r.two_mid AS A10, f.ToString(r.two_mid) AS A11, r.two_last AS A12, r.two_min AS A13, r.two_max AS A14, f.ToString(r.two_max) AS A15
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprBytesOps>(f.Content) r
