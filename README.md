# Musoq
Musoq is handy tool that decouples queries from the database. You can query whatever you want.

Would you like to query multiple folders that fits some sophisticated conditions? No problem! Perform data analysis on CSV from your bank account? That's why Musoq was created for. It can do things that databases can't do easily like adding/extending your own grouping operators, taking calculations on parent groups, works on objects and access their properties. You also don't need schema to query your datas.

## Pluggable architecture

You can easily write your own data source which can be virtually anything that is queryable (currently implemented plugins are: CSV querying, directories querying)

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

## Supported types

`Long`, `Int`, `Short`, `Bool`, `DateTimeOffset`, `String`, `Decimal`

## Query examples

- Shows `.cs` files from folders `some_path_to_dir_1`, `some_path_to_dir_2`, `some_path_to_dir` and their subfolders (uses disk plugin).

      select Name, CreationTime, Length from #disk.directory('some_path_to_dir_1', 'true')
      where Extension = '.cs'    
      union all (Name)
      select Name, CreationTime, Length from #disk.directory('some_path_to_dir_2', 'true')
      where Extension = '.cs'
      union all (Name)
      select Name, CreationTime, Length from #disk.directory('some_path_to_dir_3', 'true')
      where Extension = '.cs'

- Groups by `Country` and `City` and calculates. `ParentCount` returns count of rows that has specific country.

      select Country, City, Count(City), ParentCount(1) from #A.Entities() group by Country, City
      
- Accessing complex objects and incrementing it's value.

      select Inc(Self.Array[2]) from #A.Entities()
      
## Implemented aggregation functions

- `Count`
- `AggregateValue`
- `Sum`
- `SumIncome`
- `SumOutcome`
- `Max`
- `Min`
- `Avg`
- `Dominant`
- `ParentCount`

## Some functions

- `Abs`
- `Md5`
- `Sha256`
- `Sha512`
- `Substr`
- `ToDecimal`
- `IndexOf`
- `Contains`
- `Concat`
- `ExtractFromDate`
- `PercentOf`

## Some functions from disk plugin

- `Sha256File`
- `Md5File`
- `HasContent`
- `HasAttribute`
- `GetLinesContainingWord`
- `Substring`
- `Format`
- `CountOfLines`
- `CountOfNotEmptyLines`

## Please, be aware of

As the language looks like sql, it doesn't mean it is SQL compliant. It uses SQL syntax and repeats it's behaviour hoverwer, some differences may appear. I will also implement some experimental syntax and behaviours that are not used by database engines.

Hopefully, I will list all of this incompatibilities here

- `Currently, there is no support for NULL values (it implies grouping operators behave slightly different in some aspects than in DB-engines)`

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
