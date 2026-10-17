binary KExprArrayString {
    raw: byte repeat until raw[-1] = 0,
    value: = ToText(FromHex(Substring(ToHex(raw), 0, (raw.Length - 1) * 2)), 'utf8')
};
binary KExprArray {
    aint: uint le[4],
    afloat: double le[3],
    astr: KExprArrayString[3]
};


SELECT r.aint.Length AS A0, r.aint[0] AS A1, r.aint[3] AS A2, CASE WHEN r.aint[0] <= r.aint[1] AND r.aint[0] <= r.aint[2] AND r.aint[0] <= r.aint[3] THEN r.aint[0] WHEN r.aint[1] <= r.aint[2] AND r.aint[1] <= r.aint[3] THEN r.aint[1] WHEN r.aint[2] <= r.aint[3] THEN r.aint[2] ELSE r.aint[3] END AS A3, CASE WHEN r.aint[0] >= r.aint[1] AND r.aint[0] >= r.aint[2] AND r.aint[0] >= r.aint[3] THEN r.aint[0] WHEN r.aint[1] >= r.aint[2] AND r.aint[1] >= r.aint[3] THEN r.aint[1] WHEN r.aint[2] >= r.aint[3] THEN r.aint[2] ELSE r.aint[3] END AS A4, r.afloat.Length AS A5, r.afloat[0] AS A6, r.afloat[2] AS A7, CASE WHEN r.afloat[0] <= r.afloat[1] AND r.afloat[0] <= r.afloat[2] THEN r.afloat[0] WHEN r.afloat[1] <= r.afloat[2] THEN r.afloat[1] ELSE r.afloat[2] END AS A8, CASE WHEN r.afloat[0] >= r.afloat[1] AND r.afloat[0] >= r.afloat[2] THEN r.afloat[0] WHEN r.afloat[1] >= r.afloat[2] THEN r.afloat[1] ELSE r.afloat[2] END AS A9, r.astr.Length AS A10, r.astr[0].value AS A11, r.astr[2].value AS A12, CASE WHEN r.astr[0].value <= r.astr[1].value AND r.astr[0].value <= r.astr[2].value THEN r.astr[0].value WHEN r.astr[1].value <= r.astr[2].value THEN r.astr[1].value ELSE r.astr[2].value END AS A13, CASE WHEN r.astr[0].value >= r.astr[1].value AND r.astr[0].value >= r.astr[2].value THEN r.astr[0].value WHEN r.astr[1].value >= r.astr[2].value THEN r.astr[1].value ELSE r.astr[2].value END AS A14
FROM #fixture.bytes() f
CROSS APPLY Interpret<KExprArray>(f.Content) r
