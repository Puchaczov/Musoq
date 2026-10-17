binary KExprOpsParens {
    i_42: = 42, i_m13: = -13,
    f_2pi: = 6.28, f_e: = 2.72,
    str_0_to_4: = '01234', str_5_to_9: = '56789',
    joined: = str_0_to_4 + str_5_to_9,
    equal: = false = true, both: = false AND true, either: = NOT false OR false
};


SELECT f.ToString(r.i_42 + r.i_m13) AS A0, f.ToInt32(f.Floor(r.f_2pi + r.f_e)) AS A1, r.joined.Length AS A2, f.Reverse(r.joined) AS A3, f.Substring(r.joined, 2, 7 - 2) AS A4, f.ToInt32(r.joined) AS A5, f.ToInt32(r.equal) AS A6, f.ToInt32(r.both) AS A7, f.ToInt32(r.either) AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprOpsParens>(f.Content) r
