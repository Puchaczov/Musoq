binary KStrLiterals2 {
    dollar1: r'$foo',
    dollar2: r'${foo}',
    hash: r'#{foo}',
    at_sign: r'@foo'
};

SELECT r.dollar1 AS A0, r.dollar2 AS A1, r.hash AS A2, r.at_sign AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KStrLiterals2>(f.Content) r
