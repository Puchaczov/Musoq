binary KSwitchManualIntSizeMeta {
 title_raw: byte repeat until title_raw[-1] = 0,
 title: = ToText(FromHex(Substring(ToHex(title_raw), 0, (title_raw.Length - 1) * 2)), 'utf8'),
 author_raw: byte repeat until author_raw[-1] = 0,
 author: = ToText(FromHex(Substring(ToHex(author_raw), 0, (author_raw.Length - 1) * 2)), 'utf8')
};
binary KSwitchManualIntSizeBody {
 meta: KSwitchManualIntSizeMeta when parent.code = 17,
 entries: string[4] utf8 repeat until eof when parent.code = 34,
 rest: byte repeat until eof when parent.code <> 17 AND parent.code <> 34
};
binary KSwitchManualIntSizeChunk { code: byte, size: uint le, body: substream[size] as KSwitchManualIntSizeBody };
binary KSwitchManualIntSize { chunks: KSwitchManualIntSizeChunk repeat until eof };


SELECT r.chunks[2].body.meta IS NULL AND r.chunks[2].body.entries IS NULL AS A0, CASE WHEN r.chunks[2].body.meta IS NULL AND r.chunks[2].body.entries IS NULL THEN NULL ELSE 1 END AS A1, r.chunks[3].body.meta IS NULL AND r.chunks[3].body.entries IS NULL AS A2, CASE WHEN r.chunks[3].body.meta IS NULL AND r.chunks[3].body.entries IS NULL THEN NULL ELSE 1 END AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KSwitchManualIntSize>(f.Content) r
