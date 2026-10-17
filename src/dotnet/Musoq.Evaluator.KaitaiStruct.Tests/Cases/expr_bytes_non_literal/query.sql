binary KExprBytesNonLiteral { one: byte, two: byte, raw: byte[2] at 0, calc_bytes: = raw };


SELECT r.calc_bytes.Length AS A0, r.calc_bytes[0] AS A1, r.calc_bytes[1] AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprBytesNonLiteral>(f.Content) r
