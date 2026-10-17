binary KSwitchRepeatExpr { codes: byte[3], body: byte[4][3] };


SELECT r.codes.Length AS A0, r.codes[0] AS A1, r.codes[1] AS A2, r.codes[2] AS A3, CASE WHEN r.codes[0] = 1 OR r.codes[0] = 2 THEN r.body[0] ELSE NULL END AS A4, CASE WHEN r.codes[1] = 7 THEN r.body[1] ELSE NULL END AS A5, CASE WHEN r.codes[2] = 1 OR r.codes[2] = 2 THEN r.body[2] ELSE NULL END AS A6
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchRepeatExpr>(f.Content) r
