binary KBitsByteAlignedFoo { raw: byte[3], inner: = ((raw[0] << 16) | (raw[1] << 8) | raw[2]) >> 5 };
binary KBitsByteAligned {
 first: byte, one: = first >> 2, byte_1: byte,
 middle: byte, two: = middle >> 5, three: = ((middle >> 4) & 1) = 1, byte_2: byte[1],
 wide: ushort be, four: = wide >> 2, byte_3: substream[3] as KBitsByteAlignedFoo,
 full_byte: byte, byte_4: byte, raw_five: byte[3], five: = ((raw_five[0] << 16) | (raw_five[1] << 8) | raw_five[2]) >> 2,
 bytes_term: byte repeat until bytes_term[-1] = 69, six: byte
};


SELECT r.one AS A0, r.byte_1 AS A1, r.two AS A2, r.three AS A3, r.byte_2 AS A4, r.four AS A5, r.byte_3.inner AS A6, r.full_byte AS A7, r.byte_4 AS A8, r.five AS A9, r.bytes_term AS A10, r.six AS A11
FROM #fixture.bytes() f
CROSS APPLY Interpret<KBitsByteAligned>(f.Content) r
