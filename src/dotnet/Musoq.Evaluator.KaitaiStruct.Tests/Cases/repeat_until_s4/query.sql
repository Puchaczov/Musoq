binary KRepeatUntilS4 {
    entries: int le repeat until entries[-1] = -1,
    after_raw: byte repeat until after_raw[-1] = 0,
    decoded: = ToText(after_raw, 'ascii'),
    afterall: = Substring(decoded, 0, decoded.Length - 1)
};


SELECT r.entries AS A0, r.afterall AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatUntilS4>(f.Content) r
