binary KJsSignedRightShift {
    should_be_40000000: = 2147483648ui >> 1,
    should_be_a00000: = 2684354560ui >> 8
};


SELECT r.should_be_40000000 AS A0, r.should_be_a00000 AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KJsSignedRightShift>(f.Content) r
