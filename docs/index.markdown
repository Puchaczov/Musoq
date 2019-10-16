---

layout: home
title: ""
---
### **Welcome to the kingdom of Musoq!**

Musoq is handy execution engine that allows execution of SQL like commands on a variety of data sources.
It treats different datas as tables so it's easy to ask curious questions quickly.
Musoq supports various SQL syntax like WHERE, GROUP BY, HAVING, INNER, OUTER JOIN.
Because of plugins system, it's easy to connect to different datas, 
create own functions and even create own aggregation functions.

This query scans the directory and print first ten bytes of founded files.
```
Musoq.Server.Console --query "select Name, ToHex(GetFileBytes(10), '|') from #os.files('E:\Some\Path\To\Directory', false)" --wait --output
```
Calculate basic statistics for directory
```
Musoq.Server.Console --query "select Min(Length), Max(Length), Avg(Length), Sum(Length), Count(Length) from #os.files('E:\Some\Path\To\Directory', true)" --wait --output
```

You can use it to do things you would never suppose to! Look [here](./examples).

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