binary KRepeatNStrzLine {
    raw: byte repeat until raw[-1] = 0,
    decoded: = ToText(raw, 'utf8'),
    value: = Substring(decoded, 0, decoded.Length - 1)
};
binary KRepeatNStrz {
    qty: uint le,
    lines: KRepeatNStrzLine[qty]
};


SELECT r.qty AS A0, r.lines.Length AS A1_Length, r.lines[0].value AS A1_0, r.lines[1].value AS A1_1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNStrz>(f.Content) r
