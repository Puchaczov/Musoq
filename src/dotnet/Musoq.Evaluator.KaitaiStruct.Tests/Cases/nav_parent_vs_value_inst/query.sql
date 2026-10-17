binary KNavParentVsValueInstChildObj { do_something: = CASE WHEN parent.s1 = 'foo' THEN true ELSE false END };
binary KNavParentVsValueInst { raw: byte repeat until raw[-1] = 124, s1: = ToText(FromHex(Substring(ToHex(raw), 0, (raw.Length - 1) * 2)), 'utf8'), child: KNavParentVsValueInstChildObj };


SELECT r.s1 AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KNavParentVsValueInst>(f.Content) r
