binary KEofExceptionSwitchUserOne { val: short le };
binary KEofExceptionSwitchUserTwo { val: ushort le };
binary KEofExceptionSwitchUser {
    code: ushort le,
    data_one: KEofExceptionSwitchUserOne when code = 511,
    data_two: KEofExceptionSwitchUserTwo when code = 2
};


SELECT r.code AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KEofExceptionSwitchUser>(f.Content) r
