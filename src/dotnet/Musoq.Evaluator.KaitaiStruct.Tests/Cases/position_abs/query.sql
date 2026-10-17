binary KPositionAbs {
    index_offset: uint le
};

binary KPositionAbsIndex {
    raw: byte repeat until raw[-1] = 0,
    decoded: = ToText(raw, 'utf8'),
    entry: = Substring(decoded, 0, decoded.Length - 1)
};

SELECT root.index_offset AS A0, r.entry AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KPositionAbs>(f.Content) root
CROSS APPLY InterpretAt<KPositionAbsIndex>(f.Content, root.index_offset::int ?? 0) r
