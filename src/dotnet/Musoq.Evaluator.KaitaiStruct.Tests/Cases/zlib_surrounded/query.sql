binary KZlibSurrounded { pre: byte[4], zlib_raw: byte[12], post: byte[4] };
binary KZlibSurroundedInflated { num: int le };
SELECT r.num AS A0
FROM #fixture.bytes() f
CROSS APPLY Interpret<KZlibSurrounded>(f.Content) root
CROSS APPLY Interpret<KZlibSurroundedInflated>(f.DecompressZLibToBytes(root.zlib_raw)) r
