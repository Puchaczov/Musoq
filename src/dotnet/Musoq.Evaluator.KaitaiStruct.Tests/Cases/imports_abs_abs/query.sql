binary KImportedRoot {
    one: byte
};

binary KImportedAndAbs {
    one: byte,
    two: KImportedRoot
};

binary KImportsAbsAbs {
    one: byte,
    two: KImportedAndAbs
};

SELECT r.one AS A0, r.two.one AS A1, r.two.two.one AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KImportsAbsAbs>(f.Content) r
