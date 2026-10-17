binary KIntegersDoubleOverflow {
    signed_safe_min_be: long be,
    signed_safe_min_le: long le,
    signed_safe_max_be: long be,
    signed_safe_max_le: long le,
    signed_unsafe_neg_be: long be,
    signed_unsafe_neg_le: long le,
    signed_unsafe_pos_be: long be,
    signed_unsafe_pos_le: long le,
    unsigned_safe_max_be: ulong be at 16,
    unsigned_safe_max_le: ulong le at 24,
    unsigned_unsafe_pos_be: ulong be at 48,
    unsigned_unsafe_pos_le: ulong le at 56
};

SELECT r.signed_safe_min_be AS A0, r.signed_safe_min_le AS A1, r.signed_safe_max_be AS A2, r.signed_safe_max_le AS A3, f.ToString(r.signed_unsafe_neg_be) AS A4, f.ToString(r.signed_unsafe_neg_le) AS A5, f.ToString(r.signed_unsafe_pos_be) AS A6, f.ToString(r.signed_unsafe_pos_be) AS A7
FROM #fixture.bytes() f
CROSS APPLY Interpret<KIntegersDoubleOverflow>(f.Content) r
