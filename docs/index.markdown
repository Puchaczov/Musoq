---
# Feel free to add content and custom Front Matter to this file.
# To modify the layout, see https://jekyllrb.com/docs/themes/#overriding-theme-defaults

layout: home
---
### Welcome to the kingdom of Musoq!

Musoq is handy execution engine that allows execution of SQL like command on a variety of data sources.
It treat different datas as tables so it's easy to ask curious questions quickly.
Musoq supports various SQL syntax like WHERE, GROUP BY, HAVING, INNER, OUTER JOINS.
Thanks to plugin system, it's easy to connect to different data streams, create own functions and even create own aggregation functions.

You can use it to do things you would never think of!
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

For example, this query scans the directory and tries to read text from the `.png` images. OCR involved!

```
	WITH InBothDirs AS (
		SELECT * FROM #os.files('E:/SomeDir1', true) file1
	)
	SELECT * FROM InBothDirs ibd
```

and this query computes two folders comparision so you can easily see which files had been changed, removed, added or is in the same state!
Would you like to see more example queries? Look [here](/examples).

There are various plugins you can work with:

| Plugin  | Description |
| ------------- | ------------- |
|  Musoq.Text  | Breaks the text to streams of tokens  |
|  Musoq.System | Dual and Range tables resides here  |
|  Musoq.Time | Allows to work with time  |
|  Musoq.Xml | Queries against XML? why not...  |
|  Musoq.Csv | Loads csv and treat it's data as table  |
|  Musoq.FlatFile | Just read file line by line  |
|  Musoq.Os | files as table, directories as table, zip as table, processess as table  |
|  Musoq.Media | Handy tools to ask questions about the media (photos, music, vidoes) you have on your hard drive |
|  Musoq.Ocr | Read the text from image files  |
|  Musoq.Json  | Treat json as queryable source   |

Musoq engine standard library contains over **120** build it functions, some of them are simple like `PadLeft`, others are more complex like `Soundex`,  `LevenshteinDistance` or `LongestCommonSubstring`, all of them are to allows do quick data overview. Additionally, each plugin define it's own specialized functions and aggreations
that works against specialized data source.

### Download

It is required to have **.NET CORE 3.0**. Musoq engine itself requires `.NET Core 2.2` runtime but to utilize Musoq engine as a command line tool you have to download **Musoq.Server (.NET CORE 3.0)** that processess queries and **Musoq.Server.Console (.NET CORE 3.0)** which is a thin client. The server and client does have build-in auto update so it will update itself once newer version appear.

### Licenses

- Musoq execution engine is MIT.
- Musoq.Server is propietary software.
- Musoq.Server.Console will be published as MIT / LGPL 3.0 later.