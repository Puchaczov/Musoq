binary KRepeatNStrzDoubleLine {
    raw: byte repeat until raw[-1] = 0,
    decoded: = ToText(raw, 'utf8'),
    value: = Substring(decoded, 0, decoded.Length - 1)
};
binary KRepeatNStrzDouble {
    qty: uint le,
    lines1: KRepeatNStrzDoubleLine[qty / 2],
    lines2: KRepeatNStrzDoubleLine[qty / 2]
};


SELECT r.qty AS A0, r.lines1.Length AS A1_Length, r.lines1[0].value AS A1_0, r.lines2.Length AS A2_Length, r.lines2[0].value AS A2_0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNStrzDouble>(f.Content) r
