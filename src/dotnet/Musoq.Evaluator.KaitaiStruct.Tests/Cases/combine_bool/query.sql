binary KCombineBool {
    raw: byte,
    bool_bit: = (raw & 128) <> 0,
    bool_calc: = false,
    bool_calc_bit: = CASE WHEN true THEN bool_calc ELSE bool_bit END
};


SELECT r.bool_bit AS A0, r.bool_calc_bit AS A1
FROM #fixture.bytes() f
CROSS APPLY Interpret<KCombineBool>(f.Content) r
