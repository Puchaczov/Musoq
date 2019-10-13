---
layout: page
title: Examples
permalink: /examples/
---

Get only files that extension is `.png` or `.jpg`
```
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true) 
WHERE Extension = '.png' OR Extension = '.jpg'
```
equivalent with `in` operator: 
```
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true)
WHERE Extension IN ('.png', '.jpg')
```
group by directory and show size of each directories
```
SELECT
	DirectoryName,
	Sum(Length) / 1024 / 1024 as 'MB',
	Min(Length) as 'Min',
	Max(Length) as 'Max',
	Count(FullName) as 'CountOfFiles',
FROM #os.files('', true)
GROUP BY DirectoryName
```
try to find a file that has part `report` in his name:
```
SELECT
	*
FROM #os.files('', true)
WHERE Name like '%report%'
```
try to find a file that has in it's title word that sounds like:
```
SELECT 
	FullName
FROM #os.files('E:/', true) 
WHERE 
	IsAudio() AND 
	HasWordThatSoundLike(Name, 'material')
```
get first, last 5 bits from files and consecutive 10 bytes of file with offset of 5 from tail
```
SELECT
	ToHex(Head(5), '|'),
	ToHex(Tail(5), '|'),
	ToHex(GetFileBytes(10, 5), '|')
FROM #os.files('', false)
```
compare two directories
```
WITH filesOfA AS (
	SELECT GetRelativeName('E:\DiffDirsTests\A') AS FullName, Sha256File() AS ShaedFile FROM #os.files('E:\DiffDirsTests\A', true)
), filesOfB AS (
	SELECT GetRelativeName('E:\DiffDirsTests\B') AS FullName, Sha256File() AS ShaedFile FROM #os.files('E:\DiffDirsTests\B', true)
), inBothDirs AS (
	SELECT 
		a.FullName AS FullName, 
		(
			CASE WHEN a.ShaedFile = b.ShaedFile 
			THEN 'The Same' 
			ELSE 'Modified' 
			END
		) AS Status 
	FROM filesOfA a INNER JOIN filesOfB b ON a.FullName = b.FullName
), inSourceDir AS (
	SELECT 
		a.FullName AS FullName,
		'Removed' AS Status
	FROM filesOfA a LEFT OUTER JOIN filesOfB b ON a.FullName = b.FullName
), inDestinationDir AS (
	SELECT 
		b.FullName AS FullName,
		'Added' AS Status
	FROM filesOfA a RIGHT OUTER JOIN filesOfB b ON a.FullName = b.FullName
)
SELECT inBoth.FullName AS FullName, inBoth.Status AS Status FROM inBothDirs inBoth
UNION (FullName)
SELECT inSource.FullName AS FullName, inSource.Status AS Status FROM inSourceDir inSource
UNION (FullName)
SELECT inDest.FullName AS FullName, inDest.Status AS Status FROM inDestinationDir inDest
```
which basically equivalent with build-in plugin is:
```
SELECT 
	(
		CASE WHEN SourceFile IS NOT NULL 
		THEN SourceFileRelative 
		ELSE DestinationFileRelative 
		END
	) AS FullName, 
	(
		CASE WHEN State = 'TheSame' 
		THEN 'The Same' 
		ELSE State 
		END
	) AS Status 
FROM #os.dirscompare('E:\DiffDirsTests\A', 'E:\DiffDirsTests\B')
```
Zip files inside directories
```
SELECT
	DirectoryName, 
	AggregateValues(Name) 
FROM #os.files('E:/', true) 
WHERE IsZipArchive() 
GROUP BY DirectoryName
```
Those files greater than 1 gig
```
SELECT 
	FullName 
FROM #os.files('', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1
```
Extract and process `.csv` files packed in `.zip` file.  
```
TABLE CsvFileTable {
	fileName 'string',
	intValue 'int',
	stringValue 'string'
};

COUPLE #separatedvalues.csv WITH TABLE CsvFileTable AS SourceOfRows;

WITH FromZipCsvs AS (
	SELECT 
		zip.UnpackTo(Combine('.\ZippedValues', 'temp')), 
		true, 
		0 
	FROM #os.zip('.\Zippy.zip') zip
), ReadedFiles as (
	select fileName, intValue, stringValue from SourceOfRows(FromZipCsvs)
)
SELECT * FROM ReadedFiles
```
For file:
```
<?xml version="1.0" encoding="UTF-8"?>
<breakfast_menu>
<food>
    <name>Belgian Waffles</name>
    <price>$5.95</price>
    <description>
   Two of our famous Belgian Waffles with plenty of real maple syrup
   </description>
    <calories>650</calories>
</food>
<food>
    <name>Strawberry Belgian Waffles</name>
    <price>$7.95</price>
    <description>
    Light Belgian waffles covered with strawberries and whipped cream
    </description>
    <calories>900</calories>
</food>
<food>
    <name>Berry-Berry Belgian Waffles</name>
    <price>$8.95</price>
    <description>
    Belgian waffles covered with assorted fresh berries and whipped cream
    </description>
    <calories>900</calories>
</food>
<food>
    <name>French Toast</name>
    <price>$4.50</price>
    <description>
    Thick slices made from our homemade sourdough bread
    </description>
    <calories>600</calories>
</food>
<food>
    <name>Homestyle Breakfast</name>
    <price>$6.95</price>
    <description>
    Two eggs, bacon or sausage, toast, and our ever-popular hash browns
    </description>
    <calories>950</calories>
</food>
</breakfast_menu> 
```
Aggregate price values
```
WITH p AS ( 
   SELECT ToDecimal(Substring(price.text, 1, Length(price.text))) AS price 
   FROM #xml.file('E:\XmlTests\File.txt') 
   WHERE parent.element = 'food' AND price IS NOT NULL
) 
SELECT Sum(price) FROM p GROUP BY 'fake'
```

