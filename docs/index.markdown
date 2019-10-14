---
# Feel free to add content and custom Front Matter to this file.
# To modify the layout, see https://jekyllrb.com/docs/themes/#overriding-theme-defaults

layout: home
---
### **Welcome to the kingdom of Musoq!**

Musoq is handy execution engine that allows execution of SQL like commands on a variety of data sources.
It treats different datas as tables so it's easy to ask curious questions quickly.
Musoq supports various SQL syntax like WHERE, GROUP BY, HAVING, INNER, OUTER JOINS.
Thanks to plugin system, it's easy to connect to different data streams, create own functions and even create own aggregation functions.

You can use it to do things you would never think of! For example, this query scans the directory and tries to read text from the `.png` images. OCR involved.
```
	SELECT 
		ocr.GetText(file.FullName) as text
	FROM 
		#os.files('E:/Path/To/Directory', false) file 
	INNER JOIN 
		#ocr.single() ocr 
	ON 1 = 1 
	WHERE files.Extension = '.png'
```
Query below computes two folders comparision so you can easily see which files had been changed, removed, added or are in the same state!
```
WITH filesOfA AS (
	SELECT 
		GetRelativeName('E:\DiffDirsTests\A') AS FullName, 
		Sha256File() AS ShaedFile 
	FROM #os.files('E:\DiffDirsTests\A', true)
), filesOfB AS (
	SELECT 
		GetRelativeName('E:\DiffDirsTests\B') AS FullName, 
		Sha256File() AS ShaedFile 
	FROM #os.files('E:\DiffDirsTests\B', true)
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
SELECT 
	inBoth.FullName AS FullName, 
	inBoth.Status AS Status 
FROM inBothDirs inBoth
UNION (FullName)
SELECT 
	inSource.FullName AS FullName, 
	inSource.Status AS Status 
FROM inSourceDir inSource
UNION (FullName)
SELECT 
	inDest.FullName AS FullName, 
	inDest.Status AS Status 
FROM inDestinationDir inDest
```
Would you like to see more example queries? Look [here](/examples).

There are various plugins you can work with:

| Plugin  | Description |
| ------------- | ------------- |
|  Musoq.Text  | Breaks the text to streams of tokens  |
|  Musoq.System | Dual and Range tables resides here  |
|  Musoq.Time | Allows to work with time  |
|  Musoq.Xml | Queries against XML? why not...  |
|  Musoq.SeparateValues | Loads csv or tsv and treat it's data as table  |
|  Musoq.FlatFile | Just read file line by line  |
|  Musoq.Os | files as table, directories as table, zip as table, processess as table  |
|  Musoq.Media | Handy tools to ask questions about the media (photos, music, vidoes) you have on your hard drive |
|  Musoq.Ocr | Read the text from image files  |
|  Musoq.Json  | Treat json as queryable source   |

Musoq engine standard library contains over **120** build it functions, some of them are simple like `PadLeft`, others are more complex like `Soundex`,  `LevenshteinDistance` or `LongestCommonSubstring`, all of them are to allows do quick data overview. Additionally, each plugin define it's own specialized functions and aggreations
that works against specialized data source.