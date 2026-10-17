binary KFixedContents {
    normal: byte[6] magic [80, 65, 67, 75, 45, 49],
    high_bit_8: byte[2] magic [255, 255]
};

SELECT r.normal AS Parsed
FROM #fixture.bytes() f
CROSS APPLY Interpret<KFixedContents>(f.Content) r
