binary KRepeatNTermStructRecords1 { raw: byte repeat until raw[-1] = 170, value: = FromHex(Substring(ToHex(raw), 0, (raw.Length - 1) * 2)) };
binary KRepeatNTermStructRecords2 { raw: byte repeat until raw[-1] = 170, value: = FromHex(Substring(ToHex(raw), 0, (raw.Length - 0) * 2)) };
binary KRepeatNTermStructRecords3 { raw: byte repeat until raw[-1] = 85, value: = FromHex(Substring(ToHex(raw), 0, (raw.Length - 1) * 2)) };
binary KRepeatNTermStruct {
    records1_0: KRepeatNTermStructRecords1 at 0,
    records1_1: KRepeatNTermStructRecords1 at 0 + records1_0.raw.Length,
    records2_0: KRepeatNTermStructRecords2 at 0 + records1_0.raw.Length + records1_1.raw.Length,
    records2_1: KRepeatNTermStructRecords2 at 0 + records1_0.raw.Length + records1_1.raw.Length + records2_0.raw.Length,
    records3_0: KRepeatNTermStructRecords3 at 0 + records1_0.raw.Length + records1_1.raw.Length + records2_0.raw.Length + records2_1.raw.Length,
    records3_1: KRepeatNTermStructRecords3 at 0 + records1_0.raw.Length + records1_1.raw.Length + records2_0.raw.Length + records2_1.raw.Length + records3_0.raw.Length - 1
};


SELECT 2 AS A0, r.records1_0.value AS A1, r.records1_1.value AS A2, 2 AS A3, r.records2_0.value AS A4, r.records2_1.value AS A5, 2 AS A6, r.records3_0.value AS A7, r.records3_1.value AS A8
FROM #fixture.bytes() f
CROSS APPLY Interpret<KRepeatNTermStruct>(f.Content) r
