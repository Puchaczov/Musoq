-- Source size-eos is expressed by the profile's byte repetition boundary.
binary KStrEos {
    raw_bytes: byte repeat until eof,
    str: = ToText(raw_bytes, 'utf8')
};


SELECT r.str AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrEos>(f.Content) r
