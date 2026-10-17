binary KImportedRoot {
    one: byte
};

binary KImportedAndRel {
    one: byte,
    two: KImportedRoot
};

binary KImportsAbsRel {
    one: byte,
    two: KImportedAndRel
};

SELECT r.one AS A0, r.two.one AS A1, r.two.two.one AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KImportsAbsRel>(f.Content) r
