[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
[![Build](https://travis-ci.org/Puchaczov/Musoq.svg?branch=master)](https://travis-ci.org/Puchaczov/Musoq.svg?branch=master)

# Shortly about Musoq
Musoq is handy tool that allows using SQL on various data sources.

![Anim](https://github.com/Puchaczov/Musoq/blob/master/musoq_anim_3.gif)

# What is Musoq (more detailed)? 
Musoq exposes raw data sets as queryable sources. This allows you to write queries to those sources. It uses concepts of schemas and tables to logically group your tables. What would be used as query source? Virtually anything! Those below are just ideas but some of them had been already implemented.

- Directories
- Files
- Structured files (.csv, .json, .xml, logs)
- Photos (by exif attributes)
- Archived files (.zip)
- Git, Svn, TFS
- Websites (tables, lists)
- Processes
- Time

You can also mix sources between each other.

## How does the query looks like?

  `select * from #os.files('path/to/folder', false) where Extension = '.exe' or Extension = '.png'`
 
## How to run it?

To run it, you need `.NET Core 2.1` runtime. You can find it ![here](https://www.microsoft.com/net/download/dotnet-core/2.1). If you've got it, then download **![Musoq Simple Client](https://github.com/Puchaczov/Musoq.Console)**. You can find latest releases here.

## Does it work on Linux?

Yes, it does. I was testing it on Ubuntu 18.04. If you tried to run it on different distro or version, I will be grateful when you post an issue wherever you success or fail

## What features does the Musoq implements

- Use of `*` to select all columns.
- Group by operator.
- Having operator.
- Skip & Take operators.
- Complex object accessing ability `column.Name`.
- User defined functions and aggregation functions.
- Plugin API (for those who wants to create own data source).
- Set operators (non sql-like usage) (union, union all, except, intersect).
- Parametrizable sources.
- Like / not Like operator.
- RLike / not RLike operator (regex like operator).
- Contains operator (Doesn't support nested queries yet).
- CTE expressions.
- Desc syntax.
- In syntax.
- Inner join syntax.

## Open to add new syntax features

Do you think that SQL lacks some syntax that could simplify your work? Write this, I am open to implementations of features that sql does not have if they really prove their usefullnes. 

## Roadmap

- Dynamic Query parameters like: `select * from #schema.table(@Arg2, ...) where ColumnName = @Arg1`
- Query as data source (views)
- Optional query reordering `FROM ... WHERE ... SELECT...`
- Ability to use query as a source of next query like `with p as (select 1 from #source) select 2 from #source.method(p)`
- Syntax to query constructors about it's parameters (desc for constructors).
- Syntax to query plugins about it's methods and parameters (desc for methods).
- Rethink and design mechanism to dispose unmanaged resources.
- Further project cleanups and more robust tests.

## Long term goals

- Order by further implementation.
- Translated code optimisations.
- Rethink how `LibraryBase` works in mixed sources context.
- Left and right join syntax.
- betwen ... and ... syntax.

## Current known vital issues

- Chunks loader will greedly load datas until memory ends (important for huge files).
- Unmanaged resources are disposed too fast.
- There is no any kind of framework that allows plugin communicate with runtime about the issues occured internally.

## Architecture for plugins

You can easily plug-in your own data source. There is fairly simple plugin api that all sources uses. To read in details how to do it, jump into wiki section of this repo ![click](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Roughly about performance

[![Maintenance](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)

Tested on laptop with i7 7700HQ, 12 GB RAM, Windows 10, Main Disk (250 GB SSD), Secondary Disk (1TB HDD) where the files were placed. The query was counting how many rows does the files have. About the files, It was a single 6GB csv file with 11 columns. File had been splitted to reflect sizes you can observe in chart. It should hints you what data volume it can handle in what time.

## Plugins

<table>
      <thead>
            <tr><td>#Os</td><td>Exposes operating system tables. One of them are disk and files sources</td></tr>
            <tr><td>#Zip</td><td>Exposes compressed (.zip) files from the hard disk so that you can decompress files that fits sophisticated conditions.</td></tr>
            <tr><td>#Json</td><td>Exposes json file as queryable source.</td></tr>
            <tr><td>#Csv</td><td>Exposes csv file as queryable source.</td></tr>
            <tr><td>#FlatFile</td><td>Exposes FlatFile file as queryable source.</td></tr>
            <tr><td>#Time</td><td>Exposes time as queryable source.</td></tr>
      </thead>
</table>

## Query examples

- Gets all files from folder that has extension `.exe` or `.png`

      select * from #os.files('path/to/foder', false) where Extension = '.exe' or Extension = '.png'
      
- Gets all hours from 7 to 12 (excludingly) for all saturday and sundays from `01.04.2018 00:00:00` to `30.04.2018 00:00:00`

      select DateTime, DayOfWeek + 1 from #time.interval('01.04.2018 00:00:00', '30.04.2018 00:00:00', 'hours') where Hour >= 7 and Hour < 12 and (DayOfWeek + 1 = 6 or DayOfWeek + 1 = 7)

- Shows `.cs` files from folders `some_path_to_dir_1`, `some_path_to_dir_2`, `some_path_to_dir` and their subfolders (uses disk plugin).

      select Name, Sha256File(), CreationTime, Length from #os.directory('some_path_to_dir_1', true)
      where Extension = '.cs' take 3
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #os.directory('some_path_to_dir_2', true)
      where Extension = '.cs' take 4
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #os.directory('some_path_to_dir', true)
      where Extension = '.cs' take 5

- Groups by `Country` and `City`.

      select Country, City, Count(City) from #A.Entities() group by Country, City
      
- Accessing complex objects and passing it to method.

      select Inc(Self.Array[2]) from #A.Entities()
      
- Compressing files from folder (uses `AggregateFiles` grouping method)

      select Compress(AggregateFiles(), './Results/some_out_name.zip', 'fastest') from #os.directory('./Files', false)
      
- Decompresses only those files that fits the condition. Files are extracted to directory `./Results/DecompressWithFilterTest` 

      select Decompress(AggregateFiles(File), './Results/DecompressWithFilterTest') from #zip.file('./Files.zip') 
      where Level = 1
     
- Querying `.json` file.

      select Name, Age from #json.file('./JsonTestFile_First.json', './JsonTestFile_First.schema.json', ' ')
     
where schema is defined as: 

    { 
       "Age": "int",
       "Name": "string",
       "Books": [] 
    }
    
and file to be queried is:

    [
      {
        "Name": "Aleksander",
        "Age": 24,
        "Books": [
          {
            "Name": "A"
          },
          {
            "Name" : "B" 
          }
        ]
      },
      {
        "Name": "Mikolaj",
        "Age": 11,
        "Books": []
      },
      {
        "Name": "Marek",
        "Age": 45,
        "Books": []
      }
    ]
    
## How do I know what columns does the source have?

You can easily check it by typing a query that asks the source about columns it has. It's super easy and looks like `desc #git.commits('path/to/repo')`. All plugins supports it out of the box!

## Whats was the reason for creating it

On the one hand, I needed something that allows me performing queries on my own bank account file, on the other hand something that simultaneously filters with respect to file names and their content. For some reason, I would like it to be a single tool rather than a set of tools. That's how the musoq was born in my mind, with extensible plugins system and user defined grouping operators. All that Musoq does, you can achieve by "hand writting" all scripts manually however I found it usefull to automate this process and as a result avoid wasting time to create it. Fast querying was my goal. At a second glance, you might see that Musoq transpiles SQL code into C# code and then compiles it with Roslyn. In that case, writing script is redundant and all you have to do is to write a query and it will do the magic with your data source.

## Please, be aware of

As the language looks like sql, it doesn't mean it is fully SQL compliant. It uses SQL syntax and repeats some of it's behaviour hoverwer, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

Hopefully, I will list the incompatibilities here:

- `Parent group aggregations`
- `Non standard set operators based on keys rather than rows.`
- `There is no support for huge sources exceeds memory`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
