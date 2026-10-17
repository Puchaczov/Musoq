binary KRepeatNStructChunk {
    offset: uint le,
    len: uint le
};

binary KRepeatNStruct {
    qty: uint le,
    chunks: KRepeatNStructChunk[qty]
};

SELECT r.chunks.Length AS A0, r.chunks[0].offset AS A1, r.chunks[0].len AS A2, r.chunks[1].offset AS A3, r.chunks[1].len AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNStruct>(f.Content) r
