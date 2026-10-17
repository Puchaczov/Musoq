binary KFloatToI {
    single_value: float le,
    double_value: double le,
    single_value_if: float be when true,
    double_value_if: double be when true,
    calc_float1: = 1.234,
    calc_float2: = 1.5,
    calc_float3: = 1.9,
    calc_float4: = -2.7,
    calc_if: = 13.9
};


SELECT r.single_value AS A0, r.double_value AS A1, r.single_value_if AS A2, r.double_value_if AS A3, CASE WHEN r.single_value >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.single_value))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.single_value))) END AS A4, CASE WHEN r.double_value >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.double_value))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.double_value))) END AS A5, CASE WHEN r.single_value_if >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.single_value_if))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.single_value_if))) END AS A6, CASE WHEN r.double_value_if >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.double_value_if))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.double_value_if))) END AS A7, CASE WHEN r.calc_float1 >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.calc_float1))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.calc_float1))) END AS A8, CASE WHEN r.calc_float2 >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.calc_float2))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.calc_float2))) END AS A9, CASE WHEN r.calc_float3 >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.calc_float3))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.calc_float3))) END AS A10, CASE WHEN r.calc_float4 >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.calc_float4))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.calc_float4))) END AS A11, CASE WHEN r.calc_if >= 0 THEN f.ToInt32(f.Floor(f.ToDecimal(r.calc_if))) ELSE f.ToInt32(f.Ceil(f.ToDecimal(r.calc_if))) END AS A12
FROM #fixture.bytes() f
CROSS APPLY Interpret<KFloatToI>(f.Content) r
