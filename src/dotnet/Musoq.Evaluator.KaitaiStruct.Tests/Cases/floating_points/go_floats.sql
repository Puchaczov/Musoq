binary KFloatingPoints {
    single_value: float le,
    double_value: double le,
    single_value_be: float be,
    double_value_be: double be,
    approximate_value: float le,
    single_value_plus_int: single_value + 1,
    single_value_plus_float: single_value::Decimal + 0.5,
    double_value_plus_float: double_value::Decimal + 0.05
};

SELECT r.single_value AS A0, r.single_value_be AS A1, r.double_value AS A2, r.double_value_be AS A3, r.approximate_value AS A4, r.single_value_plus_int AS A5, r.single_value_plus_float AS A6, r.double_value_plus_float AS A7
FROM #fixture.bytes() f
CROSS APPLY Interpret<KFloatingPoints>(f.Content) r
