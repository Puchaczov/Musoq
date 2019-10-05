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
try to find a file that has in it's part word that sounds like:
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
WITH ThoseInBoth AS (
	SELECT 
		f1.FullName as FullName,
		f1.Sha256File(),
		f2.Sha256File(),
		(CASE WHEN f1.Sha256File() = f2.Sha256File() THEN 'Same' ELSE 'Changed' END)
	FROM #os.files('', true) f1
	INNER JOIN #os.files('', true) f2 ON f1.FullName = f2.FullName
), ThoseInLeft AS (
	SELECT
		f1.FullName as FullName,
		f1.Sha256File(),
		null,
		'Removed' as 'State'
	FROM #os.files('', true) f1
	LEFT OUTER JOIN #os.files('', true) f2 ON f1.FullName = f2.FullName
	WHERE f2.FullName is null
), ThoseInRight AS (
	SELECT
		f2.FullName as FullName,
		null,
		f2.Sha256File(),
		'Added' as 'State'
	FROM #os.files('', true) f1
	RIGHT OUTER JOIN #os.files('', true) f2 ON f1.FullName = f2.FullName
	WHERE f1.FullName is null
)
SELECT * FROM ThoseInBoth
UNION ALL (FullName)
SELECT * FROM ThoseInLeft
UNION ALL (FullName)
SELECT * FROM ThoseInRight
```
basically equivalent by using build-in plugin:
```
SELECT * FROM #os.compareDirs('', true)
```
Aggregate zip files inside directories
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

