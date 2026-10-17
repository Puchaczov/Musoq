binary KZlibWithHeader78 { raw: byte repeat until eof };


SELECT f.DecompressZLibToBytes(r.raw) AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KZlibWithHeader78>(f.Content) r
