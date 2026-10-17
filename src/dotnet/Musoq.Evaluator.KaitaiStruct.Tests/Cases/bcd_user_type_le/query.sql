binary KBcdUserTypeLeLtrObj {
    b1: byte,
    b2: byte,
    b3: byte,
    b4: byte,
    digit1: = (b4 & 0xF0) >> 4,
    digit2: = (b4 & 0x0F),
    digit3: = (b3 & 0xF0) >> 4,
    digit4: = (b3 & 0x0F),
    digit5: = (b2 & 0xF0) >> 4,
    digit6: = (b2 & 0x0F),
    digit7: = (b1 & 0xF0) >> 4,
    digit8: = (b1 & 0x0F),
    as_int: = digit8 *        1 + digit7 *       10 + digit6 *      100 + digit5 *     1000 + digit4 *    10000 + digit3 *   100000 + digit2 *  1000000 + digit1 * 10000000,
    as_str: = ToString(digit1) + ToString(digit2) + ToString(digit3) + ToString(digit4) + ToString(digit5) + ToString(digit6) + ToString(digit7) + ToString(digit8)
};

binary KBcdUserTypeLeRtlObj {
    b1: byte,
    b2: byte,
    b3: byte,
    b4: byte,
    digit1: = (b4 & 0xF0) >> 4,
    digit2: = (b4 & 0x0F),
    digit3: = (b3 & 0xF0) >> 4,
    digit4: = (b3 & 0x0F),
    digit5: = (b2 & 0xF0) >> 4,
    digit6: = (b2 & 0x0F),
    digit7: = (b1 & 0xF0) >> 4,
    digit8: = (b1 & 0x0F),
    as_int: = digit1 *        1 + digit2 *       10 + digit3 *      100 + digit4 *     1000 + digit5 *    10000 + digit6 *   100000 + digit7 *  1000000 + digit8 * 10000000,
    as_str: = ToString(digit8) + ToString(digit7) + ToString(digit6) + ToString(digit5) + ToString(digit4) + ToString(digit3) + ToString(digit2) + ToString(digit1)
};

binary KBcdUserTypeLeLeadingZeroLtrObj {
    b1: byte,
    b2: byte,
    b3: byte,
    b4: byte,
    digit1: = (b4 & 0xF0) >> 4,
    digit2: = (b4 & 0x0F),
    digit3: = (b3 & 0xF0) >> 4,
    digit4: = (b3 & 0x0F),
    digit5: = (b2 & 0xF0) >> 4,
    digit6: = (b2 & 0x0F),
    digit7: = (b1 & 0xF0) >> 4,
    digit8: = (b1 & 0x0F),
    as_int: = digit8 *        1 + digit7 *       10 + digit6 *      100 + digit5 *     1000 + digit4 *    10000 + digit3 *   100000 + digit2 *  1000000 + digit1 * 10000000,
    as_str: = ToString(digit1) + ToString(digit2) + ToString(digit3) + ToString(digit4) + ToString(digit5) + ToString(digit6) + ToString(digit7) + ToString(digit8)
};

binary KBcdUserTypeLe {
    ltr: substream[4] as KBcdUserTypeLeLtrObj lax,
    rtl: substream[4] as KBcdUserTypeLeRtlObj lax,
    leading_zero_ltr: substream[4] as KBcdUserTypeLeLeadingZeroLtrObj lax
};

SELECT r.ltr.as_int AS A0, r.ltr.as_str AS A1, r.rtl.as_int AS A2, r.rtl.as_str AS A3, r.leading_zero_ltr.as_int AS A4, r.leading_zero_ltr.as_str AS A5
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBcdUserTypeLe>(f.Content) r
