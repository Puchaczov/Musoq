binary KInstanceStdArray {
    ofs: uint le,
    entry_size: uint le,
    qty_entries: uint le,
    entries: byte[entry_size][qty_entries] at ofs
};

SELECT r.ofs AS A0, r.qty_entries AS A1, r.entry_size AS A2, r.entries.Length AS A3, r.entries[0] AS A4, r.entries[1] AS A5, r.entries[2] AS A6
FROM #fixture.bytes() f
CROSS APPLY Interpret<KInstanceStdArray>(f.Content) r
