-- Source: formats/str_encodings_utf16.ksy at the pinned upstream commit.
-- The bounded stream's remaining length is the parent's declared length minus
-- the two-byte BOM. Each encoding is explicit; the BOM remains an asserted field.
binary KStrEncodingsUtf16Be {
    bom: ushort be,
    str: string[parent.len_be - 2] utf16be
};
binary KStrEncodingsUtf16Le {
    bom: ushort le,
    str: string[parent.len_le - 2] utf16le
};
binary KStrEncodingsUtf16 {
    len_be: uint le,
    be_bom_removed: substream[len_be] as KStrEncodingsUtf16Be lax,
    len_le: uint le,
    le_bom_removed: substream[len_le] as KStrEncodingsUtf16Le lax
};


SELECT r.len_be AS A0, r.be_bom_removed.bom AS A1, r.be_bom_removed.str AS A2, r.len_le AS A3, r.le_bom_removed.bom AS A4, r.le_bom_removed.str AS A5
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrEncodingsUtf16>(f.Content) r
