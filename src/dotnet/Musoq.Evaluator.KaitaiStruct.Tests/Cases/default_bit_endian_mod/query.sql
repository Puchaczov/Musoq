binary KDefaultBitEndianModLittle { two: ushort le };
binary KDefaultBitEndianModBig { two: ushort be };
binary KDefaultBitEndianModMain {
    one: bits[9],
    two: bits[15],
    nest: KDefaultBitEndianModLittle,
    nest_be: KDefaultBitEndianModBig
};
binary KDefaultBitEndianMod { main: KDefaultBitEndianModMain };


SELECT r.main.one AS A0, r.main.two AS A1, r.main.nest.two AS A2, r.main.nest_be.two AS A3
FROM #fixture.bytes() f
CROSS APPLY Interpret<KDefaultBitEndianMod>(f.Content) r
