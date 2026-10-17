binary KRepeatEosStructChunk {
    offset: uint le,
    len: uint le
};

binary KRepeatEosStruct {
    chunks: KRepeatEosStructChunk repeat until eof
};

SELECT r.chunks.Length AS A0, r.chunks[0].offset AS A1, r.chunks[0].len AS A2, r.chunks[1].offset AS A3, r.chunks[1].len AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatEosStruct>(f.Content) r
