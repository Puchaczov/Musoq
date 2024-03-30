[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)

# What is Musoq
Musoq is a powerful engine designed to apply SQL syntax across a variety of data sources, making data querying more intuitive and accessible. Whether it's files, directories, comma separated values, or even complex data structures, Musoq simplifies data access.

![Anim](https://github.com/Puchaczov/Musoq/blob/master/musoq_anim_3.gif)

## Features

- **Versatility:** Data sources comes as plugins, you can visit the **[repository](https://github.com/Puchaczov/Musoq.DataSources)** where all are stored.
- **SQL Syntax Variant:** The engine uses SQL syntax variant with support for complex queries.
- **Cross-Platform:** Runs on Linux, Windows, and Docker. MacOS compatibility is anticipated.
- **In-place querying without data movement**: Musoq allows users to query data where it resides, without the need to move or load it into a central data store. This eliminates the cost, complexity, and latency of data movement
- **Extensible architecture for custom data sources**: Musoq provides an extensible plugin architecture that allows users to add support for custom data sources

## Example Data Sources

- SeparatedValues (allows to treat separated values files as tables)
- CANBus (allows to treat CAN .dbc files and corresponding .csv files that contains records of a CAN bus as tables)
- Archives (allows to treat archives as tables)
- OS (allows to treat your hard disk as a data source)

...and many more

## How to try it out

You can run it from your CLI. Just follow the instructions from **[CLI repository](https://github.com/Puchaczov/Musoq.CLI)**

## Syntax features

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

API of the engine were improved so it is possible now to integrate seamlessly with LLMs. For example, I made a custom plugin that uses enhanced syntax and query the invoice file based on pdf.co and GPT 4. This is the query I have constructed:

```sql
table PdfInvoice {
    ItemPosition 'int',
    ItemName 'string',
    ItemPrice 'decimal'
};
couple #custom.invoices with table PdfInvoice as SourceOfInvoiceValues;
select 
    ItemPosition,
    ItemName,
    ItemPrice
from SourceOfInvoiceValues('./Invoice.pdf') where ItemPrice > 0
```
Query above will effectivelly extract table from invoice with the column you asking for based on LLM inference on requested columns and their data types.

#### Use GPT to compute sentiment on a comment

```sql
select 
    csv.PostId,
    csv.Comment,
    gpt.Sentiment(csv.Comment) as Sentiment,
    csv.Date
from #separatedvalues.csv('/home/somebody/comments_sample.csv', true, 0) csv
inner join #openai.gpt('gpt-4-1106-preview') gpt on 1 = 1
```

#### Get only files that extension is `.png` or `.jpg`
```sql
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true) 
WHERE Extension = '.png' OR Extension = '.jpg'
```
#### equivalent with `in` operator: 
```sql
SELECT 
	FullName 
FROM #os.files('C:/Some/Path/To/Dir', true)
WHERE Extension IN ('.png', '.jpg')
```

#### query CAN DBC files:

```sql
SELECT  
	ID,
	Name,
	DLC,
	CycleTime
from #can.messages('./file.dbc')
```

or signals:

```sql
SELECT
	Name,
	ByteOrder,
	Length,
	StartBit,
	Factor,
	...
from #can.signals('./file.dbc')
```

#### concat two columns in csv file:

```sql
SELECT Concat(Column1, Column2) as ConcatenatedColumn from #separatedvalues.csv('./file.csv', true, 0)
```

#### group by directory and show size of each directories
```sql
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
```sql
SELECT
	*
FROM #os.files('', true)
WHERE Name like '%report%'
```
#### try to find a file that has in it's title word that sounds like:
```sql
SELECT 
	FullName
FROM #os.files('E:/', true) 
WHERE 
	IsAudio() AND 
	HasWordThatSoundLike(Name, 'material')
```
#### get first, last 5 bits from files and consecutive 10 bytes of file with offset of 5 from tail
```sql
SELECT
	ToHex(Head(5), '|'),
	ToHex(Tail(5), '|'),
	ToHex(GetFileBytes(10, 5), '|')
FROM #os.files('', false)
```
#### compare two directories
```sql
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
```sql
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
```sql
SELECT
	DirectoryName, 
	AggregateValues(Name) 
FROM #os.files('E:/', true) 
WHERE IsZipArchive() 
GROUP BY DirectoryName
```
#### Look for files greater than 1 gig
```sql
SELECT 
	FullName 
FROM #os.files('', true) 
WHERE ToDecimal(Length) / 1024 / 1024 / 1024 > 1
```
#### Tries to read the text from `.png` file through OCR plugin.
```sql	
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
```sql
SELECT Value FROM #system.range(1, 10)
```

## Architecture - high level overview

![Png](https://github.com/Puchaczov/Musoq/blob/master/Musoq-Architecture-Engine.png)

## Architecture for plugins

You can easily plug-in your own data source. There is fairly simple plugin api that all sources use. To read in details how to do it, jump into wiki section of this repo [click](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Motivation

Developed out of a need for a versatile tool that could query various data sources with SQL syntax, Musoq aims to minimize the effort and time required for data querying and analysis.

## Please, be aware of

As the language looks like sql, it doesn't mean it is fully SQL compliant. It uses SQL syntax and repeats some of it's behaviour however, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

## License

Musoq is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
