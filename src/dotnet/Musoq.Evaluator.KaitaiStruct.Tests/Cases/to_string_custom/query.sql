binary KToStringCustom {
    raw1: byte repeat until raw1[-1] = 124,
    raw2: byte repeat until raw2[-1] = 124,
    decoded1: = ToText(raw1, 'utf8'),
    decoded2: = ToText(raw2, 'utf8'),
    s1: = Substring(decoded1, 0, decoded1.Length - 1),
    s2: = Substring(decoded2, 0, decoded2.Length - 1),
    display: = 's1 = ' + s1 + ', s2 = ' + s2
};


SELECT r.display AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KToStringCustom>(f.Content) r
