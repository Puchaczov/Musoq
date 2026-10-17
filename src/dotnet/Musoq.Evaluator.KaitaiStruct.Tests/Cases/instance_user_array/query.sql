binary KInstanceUserArrayEntry {
    word1: ushort le,
    word2: ushort le
};

binary KInstanceUserArrayBoundedEntry {
    payload: substream[parent.entry_size] as KInstanceUserArrayEntry lax
};

binary KInstanceUserArray {
    ofs: uint le,
    entry_size: uint le,
    qty_entries: uint le,
    user_entries: KInstanceUserArrayBoundedEntry[qty_entries] at ofs when ofs > 0
};


SELECT r.ofs AS A0, r.qty_entries AS A1, r.entry_size AS A2, r.user_entries.Length AS A3, r.user_entries[0].payload.word1 AS A4, r.user_entries[0].payload.word2 AS A5, r.user_entries[1].payload.word1 AS A6, r.user_entries[1].payload.word2 AS A7, r.user_entries[2].payload.word1 AS A8, r.user_entries[2].payload.word2 AS A9
FROM #fixture.bytes() f
CROSS APPLY Interpret<KInstanceUserArray>(f.Content) r
