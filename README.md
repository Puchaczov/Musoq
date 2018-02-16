# Musoq
Musoq is handy tool that allows to quering whatever you want.

Would you like to query multiple folders that fits some sophisticated conditions? No problem! Perform data analysis on CSV from your bank account? That's why Musoq was created for. It can do things that databases can't do easily like adding/extending your own grouping operators, taking calculations on parent groups, works on objects and access their properties.

![alt text](https://raw.githubusercontent.com/Puchaczov/Musoq/master/query_res.png)

## Pluggable architecture

You can easily write your own data source which can be virtually anything that is queryable (currently implemented plugins are: CSV querying, directories querying, JSON querying)

## Currently implemented features

- Use of `*` to select all columns.
- Group by (access to parent group, group by expression rather than column, group by column)
- User defined functions and aggregation functions.
- Set operators (non sql-like usage) (union, union all, except, intersect)
- Complex object arrays and properties accessing.
- Like / not like operator
- Contains operator (Doesn't support nested queries yet)

## Features considered to be explored / implemented

- Query parallelization
- CTE expressions
- Nested queries
- Case when sytax

## Query examples

- Shows `.cs` files from folders `some_path_to_dir_1`, `some_path_to_dir_2`, `some_path_to_dir` and their subfolders (uses disk plugin).

      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir_1', 'true')
      where Extension = '.cs' take 3
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir_2', 'true')
      where Extension = '.cs' take 4
      union all (Name)
      select Name, Sha256File(), CreationTime, Length from #disk.directory('some_path_to_dir', 'true')
      where Extension = '.cs' take 5

- Groups by `Country` and `City` and calculates. `ParentCount` returns count of rows that specific country has.

      select Country, City, Count(City), ParentCount(1) from #A.Entities() group by Country, City
      
- Accessing complex objects and incrementing it's value.

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

## Plugins

<table>
      <thead>
            <tr><td>#disk</td><td>Exposes files and directories from the hard disk as queryable source.</td></tr>
            <tr><td>#zip</td><td>Exposes compressed (.zip) files from the hard disk so that you can decompress files that fits sophisticated conditions.</td></tr>
            <tr><td>#json</td><td>Exposes json file as queryable source.</td></tr>
            <tr><td>#csv</td><td>Exposes csv file as queryable source.</td></tr>
      </thead>
</table>

## Please, be aware of

As the language looks like sql, it doesn't mean it is SQL compliant. It uses SQL syntax and repeats some of it's behaviour hoverwer, some differences may appear. It will also implement some experimental syntax and behaviours that are not used by database engines.

Hopefully, I will list all of this incompatibilities here

- `Currently, there is no support for NULL values (it implies grouping operators behave slightly different in some aspects than in DB-engines)`
- `Parent group aggregations`
- `Non standard set operators based on keys rather than rows.`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
