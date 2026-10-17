binary KDefaultEndianExprExceptionDoc {
    indicator: byte[2] check ToHex(indicator) = '4949' OR ToHex(indicator) = '4D4D',
    number: uint le,
    big: = ToHex(indicator) = '4D4D',
    some_int: = CASE WHEN big THEN ((number & 255ui) << 24) | ((number & 65280ui) << 8) | ((number >> 8) & 65280ui) | (number >> 24) ELSE number END,
    some_int_be: ushort be,
    some_int_le: ushort le
};
binary KDefaultEndianExprException { docs: KDefaultEndianExprExceptionDoc repeat until eof };


SELECT r.docs AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultEndianExprException>(f.Content) r
