binary KImported2 {
    one: byte
};

binary KImported1 {
    one: byte,
    two: KImported2
};

binary KImportsRel1 {
    one: byte,
    two: KImported1
};

SELECT r.one AS A0, r.two.one AS A1, r.two.two.one AS A2
FROM #fixture.bytes() f
CROSS APPLY Interpret<KImportsRel1>(f.Content) r
