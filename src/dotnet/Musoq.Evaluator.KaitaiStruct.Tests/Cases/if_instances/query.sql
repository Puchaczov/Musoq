binary KIfInstances {
    never_happens: byte at 100500 when false
};


SELECT r.never_happens AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KIfInstances>(f.Content) r
