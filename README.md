[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
[![Build & Tests](https://github.com/Puchaczov/Musoq/workflows/Unit%20Tests/badge.svg)](https://github.com/Puchaczov/Musoq/workflows/Unit%20Tests/badge.svg)

# What is Musoq?
Musoq is handy tool that allows to use SQL syntax on a variety of data sources.

![Anim](https://github.com/Puchaczov/Musoq/blob/master/musoq_anim_3.gif)

# Do you want to know more?
Musoq exposes raw data sets as queryable sources. This allows to search these data sources using SQL syntax variant. What can be used as query source? Virtually anything! Here are some ideas (many of them are already included in the project!):

- Directories
- Files
- Structured files (.csv, .json, .xml, logs)
- Photos (by exif attributes)
- Archived files (.zip)
- Git, Svn, TFS
- Websites (tables, lists)
- Processes
- Time

It is possible to mix sources between each other.

## What does a query look like?

  `select * from #os.files('path/to/folder', false) where Extension = '.exe' or Extension = '.png'`
 
## How to run it?

To run it, visit **[Musoq installation page](https://puchaczov.github.io/Musoq/installation)**. You will find there latest release with installation process description.

## Does it work on Linux?

Yes, it does work on linux. I have tested it on Ubuntu 18.04. If you try to run it on different distro or version. I will be grateful if you would post an issue reporting either success or fail.

## What features does it has?

- Optional query reordering (from ... where ... group by ... having ... select ... skip N take N2)
- Use of `*` to select all columns.
- Group by operator.
- Having operator.
- Skip & Take operators.
- Complex object accessing ability `column.Name`.
- User defined functions and aggregation functions.
- Plugin API (to create your own custom data source).
- Set operators (non sql-like usage) (union, union all, except, intersect).
- Parametrizable sources.
- Like / not Like operator.
- RLike / not RLike operator (regex like operator).
- Contains operator (Doesn't support nested queries yet).
- CTE expressions.
- Desc for schema, schema table constructors and tables.
- In syntax.
- Inner, Left outer, Right outer join syntax.

## Query examples

#### Get only files that extension is `.png` or `.jpg`
```
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true) 
WHERE Extension = '.png' OR Extension = '.jpg'
```
#### equivalent with `in` operator: 
```
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true)
WHERE Extension IN ('.png', '.jpg')
```
#### group by directory and show size of each directories
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
#### try to find a file that has part `report` in his name:
```
SELECT
	*
FROM #os.files('', true)
WHERE Name like '%report%'
```
#### try to find a file that has in it's title word that sounds like:
```
SELECT 
	FullName
FROM #os.files('E:/', true) 
WHERE 
	IsAudio() AND 
	HasWordThatSoundLike(Name, 'material')
```
#### get first, last 5 bits from files and consecutive 10 bytes of file with offset of 5 from tail
```
SELECT
	ToHex(Head(5), '|'),
	ToHex(Tail(5), '|'),
	ToHex(GetFileBytes(10, 5), '|')
FROM #os.files('', false)
```
#### compare two directories
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
#### which basically equivalent with build-in plugin is:
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
#### Look for directories contains zip files
```
SELECT
	DirectoryName, 
	AggregateValues(Name) 
FROM #os.files('E:/', true) 
WHERE IsZipArchive() 
GROUP BY DirectoryName
```
#### Look for files greater than 1 gig
```
SELECT 
	FullName 
FROM #os.files('', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1
```
#### Tries to read the text from `.png` file through OCR plugin.
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
#### Prints the values from 1 to 9
```
SELECT Value FROM #system.range(1, 10)
```
    
## How do I know what columns the source has?

There is a built-in way to list all the columns from a source, all plugins supports it out of the box! The command is: `desc #schema.table(someArg1, someArg2)`. 

## Architecture for plugins

You can easily plug-in your own data source. There is fairly simple plugin api that all sources use. To read in details how to do it, jump into wiki section of this repo [click](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Roughly about performance

[![Maintenance](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)

Tested on laptop with i7 7700HQ, 12 GB RAM, Windows 10, Main Disk (250 GB SSD), Secondary Disk (1TB HDD). Files were placed on the HDD. The query tested was counting how many rows the files has. The file tested was a single 6GB csv file with 11 columns. For each test the file was split to reflect sizes you can observe in chart. This should give you some guidance on what data processing rate you can expect using this tool.

## Motivation for creating this project

On the one hand, I needed something that allowed me to perform queries on my own bank account file, at the same time something that filters with respect to file names and their content. I had the idea that I would like it to be a single tool rather than a set of tools. That's how the musoq was born in my mind, with extensible plugins system and user defined grouping operators. All that Musoq does, you can achieve by "hand writing" multiple scripts manually, however I found it useful to automate this process and as a result minimizing the amount of time to create it. Fast querying was my goal. Looking at it another way, you might see that Musoq transpiles SQL code into C# code and then compiles it with Roslyn. In that case, writing C# code is redundant when all you have to do is to write a query and it will do the magic with your data source.

## Please, be aware of

As the language looks like sql, it doesn't mean it is fully SQL compliant. It uses SQL syntax and repeats some of it's behaviour however, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
