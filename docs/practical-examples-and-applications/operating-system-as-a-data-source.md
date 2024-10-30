---
title: Operating System as a Data Source
layout: default
parent: Practical Examples and Applications
nav_order: 2
---

# Operating System as a Data Source

The operating system can be a source of numerous data whose processing can benefit us. This data source allows us to work with constructs that are managed by the operating system, such as processes, files, or directories. In the future, there will also be the possibility of using various operating system tools (e.g., ping).
## Filtering Processes by Name

Although access to many properties of the process object requires elevated privileges, we can still take advantage of this and take a look at the processes that concern us, for example:

```sql
SELECT 
    Id,
    ProcessName,
    Directory,
    FileName
FROM #os.processes() where ProcessName like '%Musoq%'
```

## Finding `.cfg` and `.tmp` Files in Downloads

This query retrieves the file size (`Length`) and the full path (`FullName`) of all files located in the `Downloads` directory of the user `{USER}` that have either a `.cfg` or `.tmp` extension. It searches through all the subdirectories (`true` parameter indicates recursive search) within the specified path for files matching the criteria.

```sql
SELECT Length, FullName FROM #os.files('C:\Users\{USER}\Downloads', true) WHERE FullName LIKE '%.cfg' OR FullName LIKE '%.tmp'
```

## Listing Non-empty Files

This query lists the names (`Name`) of all non-empty files located in the `Downloads` directory of the user `{USER}`. It includes files from all subdirectories (the `true` parameter enables recursive search) within the specified path, filtering out any files with a length of `0` (empty files).

```sql
SELECT Name FROM #os.files('C:\Users\{USER}\Downloads', true) WHERE Length > 0
```

## Counting File Types

This query calculates the number of files for each file type (extension) located in the `Downloads` directory of the user `{USER}`. By grouping the results by the file extension (`Extension`), it provides a count of files for each unique extension. The search includes all subdirectories within the specified path due to the `true` parameter, enabling a comprehensive overview of file types present in the Downloads folder.

```sql
SELECT Extension, Count(Extension) FROM #os.files('C:\Users\{USER}\Downloads', true) GROUP BY Extension
```

## Paginating Files in Downloads

This query displays the names (`Name`) of files located in the `Downloads` directory of user `{USER}`, implementing pagination by skipping the first 5 files and then taking the next 5 files. It searches through all subdirectories within the specified path (`true` parameter for recursive search), effectively listing files in a segmented manner, which is particularly useful for processing large sets of files in manageable chunks.

```sql
SELECT Name FROM #os.files('C:\Users\{USER}\Downloads', true) skip 5 take 5
```

## Finding CSV Files Containing 'Frames' Word in File Name

This query searches for `.csv` files that contain the word 'Frames' within their full path (`FullName`) in the `Downloads` directory of the user `{USER}`. It leverages the `rlike` operator for regex pattern matching to filter files. The `true` parameter ensures that the search is conducted recursively through all subdirectories within the specified path, targeting only those `.csv` files whose names include 'Frames'.

```sql
SELECT Name FROM #os.files('C:\Users\{USER}\Downloads', true) WHERE FullName rlike '.*Frames.*.csv'
```

## Filtering `.tmp` and `.cfg` Files by Size

This query selects the names (`Name`) of files within the `Downloads` directory of the user `{USER}` that meet specific criteria based on their extension and size. It filters for `.tmp` files that are empty (`Length = 0`) and `.cfg` files larger than 1MB (`Length > 1000000`). The search is performed recursively in all subdirectories within the specified path (`true` parameter), allowing for a comprehensive filtering across the Downloads folder.

## Combining JPG Files from Two Folders

This query aggregates the full paths (`FullName`) of `.jpg` files from two specific locations: `Folder1` and `Folder2` within the user `{USER}`'s directory. It uses the `UNION ALL` operation to combine the results from both folders into a single list, including duplicates if they exist. The `true` parameter for each `#os.files` function call ensures that the search includes all subdirectories within both specified paths, targeting `.jpg` files exclusively.

```sql
SELECT FullName FROM #os.files('C:\Users\{USER}\Folder1', true) WHERE Name LIKE '%.jpg'
UNION ALL (FullName)
SELECT FullName FROM #os.files('C:\Users\{USER}\Folder2', true) WHERE Name LIKE '%.jpg'
```