binary KNavRootHeaderObj {
    qty_entries: uint le,
    filename_len: uint le
};

binary KNavRootEntry {
    filename: string[parent.parent.header.filename_len] utf8
};

binary KNavRootIndexObj {
    magic: byte[4],
    entries: KNavRootEntry[parent.header.qty_entries]
};

binary KNavRoot {
    header: KNavRootHeaderObj,
    index: KNavRootIndexObj
};


SELECT r.header.qty_entries AS A0, r.header.filename_len AS A1, r.index.entries.Length AS A2, r.index.entries[0].filename AS A3, r.index.entries[1].filename AS A4
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNavRoot>(f.Content) r
