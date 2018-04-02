# Musoq
Musoq is handy tool that allows querying everything that can be treated as queryable source.

# What is Musoq? 
Musoq exposes raw datas as queryable source. This allows you to write SQL-like queries and ask questions for those sources. Musoq uses concepts of schemas and tables. Those allows you to logically group your tables. What would be used as query source? Virtually anything! Those below are just ideas but some of them had been already implemented!

- Directories
- Files
- Structured files (.csv, .json, .xml, logs)
- Photos (by exif attributes)
- Archived files (.zip)
- Git, Svn, TFS
- Websites (tables, lists)
- Processes
- Time

![alt text](https://raw.githubusercontent.com/Puchaczov/Musoq/master/query_res.png)

## Currently implemented features

- Use of `*` to select all columns.
- Group by
- User defined functions and aggregation functions.
- Plugin api
- Set operators (non sql-like usage) (union, union all, except, intersect)
- Complex object arrays and properties accessing.
- Parametrizable source.
- Like / not like operator
- Contains operator (Doesn't support nested queries yet)
- CTE expressions
- Skip & Take operators.

## Features considered to be explored / implemented

- Query parallelization
- Case when syntax
- Order by syntax
- Joins multiple tables
- Nested queries (corellated and uncorellated)

## Pluggable architecture

You can easily write your own data source. There is fairly simple plugin api that all plugins uses. To read in details how to do it, jump into wiki of this repo ![click](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Plugins

<table>
      <thead>
            <tr><td>#Disk</td><td>Exposes files and directories from the hard disk as queryable source.</td></tr>
            <tr><td>#Zip</td><td>Exposes compressed (.zip) files from the hard disk so that you can decompress files that fits sophisticated conditions.</td></tr>
            <tr><td>#Json</td><td>Exposes json file as queryable source.</td></tr>
            <tr><td>#Csv</td><td>Exposes csv file as queryable source.</td></tr>
            <tr><td>#FlatFile</td><td>Exposes FlatFile file as queryable source.</td></tr>
            <tr><td>#Git</td><td>Exposes git repository as queryable source.</td></tr>
            <tr><td>#Time</td><td>Exposes time as queryable source.</td></tr>
      </thead>
</table>

## Query examples

- Gets all commits from repo

      select * from #git.commits('path/to/repo')

- Gets all files from folder that has extension `.exe` or `.png`

      select * from #disk.files('path/to/foder', 'false') where Extension = '.exe' or Extension = '.png'
      
- Gets all hours from 7 to 12 (excludingly) for all saturday and sundays from `01.04.2018 00:00:00` to `30.04.2018 00:00:00`

      select DateTime, DayOfWeek + 1 from #time.interval('01.04.2018 00:00:00', '30.04.2018 00:00:00', 'hours') where Hour >= 7 and Hour < 12 and (DayOfWeek + 1 = 6 or DayOfWeek + 1 = 7)

- Shows `.cs` files from folders `some_path_to_dir_1`, `some_path_to_dir_2`, `some_path_to_dir` and their subfolders (uses disk plugin).

      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir_1', 'true')
      where Extension = '.cs' take 3
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir_2', 'true')
      where Extension = '.cs' take 4
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir', 'true')
      where Extension = '.cs' take 5

- Groups by `Country` and `City`.

      select Country, City, Count(City) from #A.Entities() group by Country, City
      
- Accessing complex objects and passing it to method.

      select Inc(Self.Array[2]) from #A.Entities()
      
- Compressing files from folder (uses `AggregateFiles` grouping method)

      select Compress(AggregateFiles(), './Results/some_out_name.zip', 'fastest') from #disk.directory('./Files', 'false')
      
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

## Please, be aware of

As the language looks like sql, it doesn't mean it is fully SQL compliant. It uses SQL syntax and repeats some of it's behaviour hoverwer, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

Hopefully, I will list the incompatibilities here:

- `Currently, there is no support for NULL values (it implies grouping operators behave slightly different in some aspects than in DB-engines)`
- `Parent group aggregations`
- `Non standard set operators based on keys rather than rows.`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
