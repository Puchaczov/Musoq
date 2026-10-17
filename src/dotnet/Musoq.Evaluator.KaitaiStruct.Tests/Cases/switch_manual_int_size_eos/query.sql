binary KSwitchManualIntSizeEosMeta {
 title_raw: byte repeat until title_raw[-1] = 0,
 title: = ToText(FromHex(Substring(ToHex(title_raw), 0, (title_raw.Length - 1) * 2)), 'utf8'),
 author_raw: byte repeat until author_raw[-1] = 0,
 author: = ToText(FromHex(Substring(ToHex(author_raw), 0, (author_raw.Length - 1) * 2)), 'utf8')
};
binary KSwitchManualIntSizeEosBody {
 meta: KSwitchManualIntSizeEosMeta when parent.code = 17,
 entries: string[4] utf8 repeat until eof when parent.code = 34,
 rest: byte repeat until eof when parent.code <> 17 AND parent.code <> 34
};
binary KSwitchManualIntSizeEosChunk { code: byte, size: uint le, body: substream[size] as KSwitchManualIntSizeEosBody };
binary KSwitchManualIntSizeEos { chunks: KSwitchManualIntSizeEosChunk repeat until eof };


SELECT r.chunks.Length AS A0, r.chunks[0].code AS A1, r.chunks[0].body.meta.title AS A2, r.chunks[0].body.meta.author AS A3, r.chunks[1].code AS A4, r.chunks[1].body.entries.Length AS A5_Length, r.chunks[1].body.entries[0] AS A5_0, r.chunks[1].body.entries[1] AS A5_1, r.chunks[1].body.entries[2] AS A5_2, r.chunks[2].code AS A6, r.chunks[2].body.rest AS A7, r.chunks[3].code AS A8, r.chunks[3].body.rest AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchManualIntSizeEos>(f.Content) r
