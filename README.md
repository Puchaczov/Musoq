[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
[![Build](https://travis-ci.org/Puchaczov/Musoq.svg?branch=master)](https://travis-ci.org/Puchaczov/Musoq.svg?branch=master)

# A Quick Description of Musoq
Musoq is handy tool that allows you to use SQL syntax on a variety of data sources.

![Anim](https://github.com/Puchaczov/Musoq/blob/master/musoq_anim_3.gif)

# What is Musoq? (in depth) 
Musoq exposes raw data sets as queryable sources. This allows you to query those sources using a syntax very similar to SQL. It uses concepts of schemas and tables to logically define your datasources. What can be used as query source? Virtually anything! Here are some ideas (many of them are already included in this project!):

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

## What does a query look like?

  `select * from #os.files('path/to/folder', false) where Extension = '.exe' or Extension = '.png'`
 
## How to run it?

To run it, you need `.NET Core 2.1` runtime. You can find it [here](https://www.microsoft.com/net/download/dotnet-core/2.1). Once you have that, then download **[Musoq Simple Client](https://github.com/Puchaczov/Musoq.Console)**. You can find latest releases [here](https://github.com/Puchaczov/Musoq.Console/releases).

## Does it work on Linux?

Yes, it does. I have tested it on Ubuntu 18.04. If you try to run it on different distro or version, I will be grateful if you would post an issue reporting either success or fail

## What features does the Musoq implements

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
- Desc syntax.
- In syntax.
- Inner join syntax.

## Open to add new syntax features

Do you think that SQL lacks some syntax that could simplify your work? Write this, I am open to implementations of features that sql does not have if they can prove their usefullness. 

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
- Translated code optimizations.
- Rethink how `LibraryBase` works in mixed sources context.
- Left and right join syntax.
- between ... and ... syntax.

## Current known critical issues

- Chunks loader will greedily load datas until memory runs out (important to know for huge files).
- Unmanaged resources are disposed too fast.
- There is not any kind of framework that allows plugin communicate with runtime about the issues occurred internally.

## Architecture for plugins

You can easily plug-in your own data source. There is fairly simple plugin api that all sources use. To read in details how to do it, jump into wiki section of this repo [click](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Roughly about performance

[![Maintenance](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)

Tested on laptop with i7 7700HQ, 12 GB RAM, Windows 10, Main Disk (250 GB SSD), Secondary Disk (1TB HDD). Files were placed on the HDD. The query tested was counting how many rows the files has. The file tested was a single 6GB csv file with 11 columns. For each test the file was split to reflect sizes you can observe in chart. This should give you some guidance on what data processing rate you can expect using this tool.

## Plugins

Plugins which have been implemented so far include:

| Plugin | Description |
| ---    | --- |
| `#Os`       | Exposes operating system tables. One of them are disk and files sources |
| `#Zip`      | Exposes compressed (.zip) files from the hard disk so that you can decompress files that fits sophisticated conditions. |
| `#Json`     | Exposes json file as queryable source. |
| `#Csv`      | Exposes csv file as queryable source. |
| `#FlatFile` | Exposes FlatFile file as queryable source. |
| `#Time`     | Exposes time as queryable source. |


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
    
## How do I know what columns the source has?

There is a built-in way to list all the columns from a source, all plugins supports it out of the box! The command is: `desc #git.commits('path/to/repo')`. 

## Motivation for creating this project

On the one hand, I needed something that allowed me to perform queries on my own bank account file, at the same time something that filters with respect to file names and their content. I had the idea that I would like it to be a single tool rather than a set of tools. That's how the musoq was born in my mind, with extensible plugins system and user defined grouping operators. All that Musoq does, you can achieve by "hand writing" multiple scripts manually, however I found it useful to automate this process and as a result minimizing the amount of time to create it. Fast querying was my goal. Looking at it another way, you might see that Musoq transpiles SQL code into C# code and then compiles it with Roslyn. In that case, writing C# code is redundant when all you have to do is to write a query and it will do the magic with your data source.

## Please, be aware of

As the language looks like sql, it doesn't mean it is fully SQL compliant. It uses SQL syntax and repeats some of it's behaviour however, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

I will try to keep this list of the incompatibilities up-to-date (hopefully):

- `Parent group aggregations`
- `Non standard set operators based on keys rather than rows.`
- `There is no support for huge sources exceeds memory`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
