---
title: Alpine X64
layout: home
parent: Os
---

# Musoq.DataSources.Os
Provides schema to work with operating system abstractions
## Tables

A table in Musoq represents a structured data source with rows and columns. Each table provides access to specific data types and can be queried using the FROM clause (e.g., 'FROM @source.table()'). Below are the available tables exposed by this data source:

### os.dirscompare(string sourceDirectory, string destinationDirectory)

Compares two directories


| Name | Type | Description |
| ---- | ---- | ----------- |
| SourceFile | ExtendedFileInfo | Source file |
| DestinationFile | ExtendedFileInfo | Destination file |
| State | string | The Same / Modified / Added / Removed |
| SourceRoot | DirectoryInfo | Source directory |
| DestinationRoot | DirectoryInfo | Destination directory |
| SourceFileRelative | string | Relative path to source file |
| DestinationFileRelative | string | Relative path to destination file |

### os.directories(string directory, bool useSubdirectories)

Gets the directories


| Name | Type | Description |
| ---- | ---- | ----------- |
| FullName | string | Full name of the directory |
| Attributes | FileAttributes | Directory attributes |
| CreationTime | DateTime | Creation time |
| CreationTimeUtc | DateTime | Creation time in UTC |
| LastAccessTime | DateTime | Last access time |
| LastAccessTimeUtc | DateTime | Last access time in UTC |
| LastWriteTime | DateTime | Last write time |
| LastWriteTimeUtc | DateTime | Last write time in UTC |
| Exists | bool | Determine does the directory exists |
| Extension | string | Gets the extension part of the file name |
| LastAccessTime | DateTime | Gets the time the current file or directory was last accessed |
| LastAccessTimeUtc | DateTime | Gets the time, in coordinated universal time (UTC), that the current file or directory was last accessed |
| Name | string | Gets the directory name |
| LastWriteTime | DateTime | Gets the date when the file or directory was written to |
| Parent | DirectoryInfo | Gets the parent directory |
| Root | DirectoryInfo | Gets the root directory |
| DirectoryInfo | DirectoryInfo | Gets raw DirectoryInfo |

### os.dlls(string path)

Gets the dlls


| Name | Type | Description |
| ---- | ---- | ----------- |
| FileInfo | FileInfo | Gets the metadata about the DLL file |
| Assembly | Assembly | Gets the Assembly object |
| Version | FileVersionInfo | Gets the assembly version |

### os.dlls(string path)

Gets the dlls


| Name | Type | Description |
| ---- | ---- | ----------- |
| FileInfo | FileInfo | Gets the metadata about the DLL file |
| Assembly | Assembly | Gets the Assembly object |
| Version | FileVersionInfo | Gets the assembly version |

### os.files(string directory, bool useSubdirectories)

Gets the files


| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | string | Name of the file |
| FileName | string | Name of the file |
| CreationTime | DateTime | Creation time |
| CreationTimeUtc | DateTime | Creation time in UTC |
| LastAccessTime | DateTime | Last access time |
| LastAccessTimeUtc | DateTime | Last access time in UTC |
| LastWriteTime | DateTime | Last write time |
| LastWriteTimeUtc | DateTime | Last write time in UTC |
| Extension | string | Gets the extension part of the file name |
| FullPath | string | Gets the full path of file |
| DirectoryName | string | Gets the directory name |
| DirectoryPath | string | Gets the directory path |
| Exists | bool | Determine whether file exists or not |
| IsReadOnly | bool | Determine whether the file is readonly |
| Length | long | Gets the length of file |

### os.processes()

Gets the processes


| Name | Type | Description |
| ---- | ---- | ----------- |
| BasePriority | int | Gets the base priority of associated process |
| EnableRaisingEvents | bool | Gets whether the exited event should be raised when the process terminates |
| ExitCode | int | Gets the value describing process termination |
| ExitTime | DateTime | Exit time in UTC |
| Handle | IntPtr | Gets the native handle of the associated process |
| HandleCount | int | Gets the number of handles opened by the process |
| HasExited | bool | Gets a value indicating whether the associated process has been terminated |
| Id | int | Gets the unique identifier for the associated process |
| MachineName | string | Gets the name of the computer the associated process is running on |
| MainWindowTitle | string | Gets the caption of the main window of the process |
| PagedMemorySize64 | long | Gets a value indicating whether the user interface of the process is responding |
| ProcessName | string | The name that the system uses to identify the process to the user |
| ProcessorAffinity | IntPtr | Gets the processors on which the threads in this process can be scheduled to run |
| Responding | bool | Gets a value indicating whether the user interface of the process is responding |
| StartTime | DateTime | Gets the time that the associated process was started |
| TotalProcessorTime | TimeSpan | Gets the total processor time for this process |
| UserProcessorTime | TimeSpan | Gets the user processor time for this process |
| Directory | string | Gets the directory of the process |
| FileName | string | Gets the filename of the process |

### os.zip(string path)

Gets the zip files


| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | string | Gets the file name of the entry in the zip archive |
| FullName | string | Gets the relative path of the entry in the zip archive |
| CompressedLength | long | Gets the compressed size of the entry in the zip archive |
| LastWriteTime | DateTimeOffset | Gets the last time the entry in the zip archive was changed |
| Length | long | Gets the uncompressed size of the entry in the zip archive |
| IsDirectory | bool | Determine whether the entry is a directory |
| Level | int | Gets the nesting level |

### os.metadata(string directoryOrFile)

Gets the metadata for file or for files within the directory


| Name | Type | Description |
| ---- | ---- | ----------- |
| FullName | string | Gets the full path of the file |
| DirectoryName | string | Gets the directory the metadata resides in |
| TagName | string | Gets the tag name |
| Description | string | Gets the description |

### os.metadata(string directory, bool throwOnMetadataReadError)

Gets the metadata for files within directories


| Name | Type | Description |
| ---- | ---- | ----------- |
| FullName | string | Gets the full path of the file |
| DirectoryName | string | Gets the directory the metadata resides in |
| TagName | string | Gets the tag name |
| Description | string | Gets the description |

### os.metadata(string directory, bool useSubdirectories, bool throwOnMetadataReadError)

Gets the metadata for files within directories


| Name | Type | Description |
| ---- | ---- | ----------- |
| FullName | string | Gets the full path of the file |
| DirectoryName | string | Gets the directory the metadata resides in |
| TagName | string | Gets the tag name |
| Description | string | Gets the description |

## This Data Source Methods

Methods allow operations on columns or values within queries. They provide specific operations and transformations on your data (e.g., 'SELECT Method(Column)'). The following methods are available in this data source:

### Aggregation methods

Aggregation methods process multiple rows to compute a single result value. These methods are used with GROUP BY clauses to summarize data across row groups. Below are the aggregation methods supported by this data source:
#### IReadOnlyList\<FileEntity\> AggregateFiles(string name)

Gets the aggregated average value from the given group name

#### IReadOnlyList\<DirectoryInfo\> AggregateDirectories(string name)

Gets the aggregated average value from the given group name

### Non aggregation methods

Non-aggregation methods process data on a row-by-row basis, performing calculations or transformations for each individual row. These methods return a result for each input row. The following non-aggregation methods are available:
#### bool IsZipArchive(string extension)

Determines whether the extension is zip archive.

#### bool IsZipArchive()

Determines whether the extension is zip archive.

#### bool IsArchive(string extension)

Determine whether the extension is archive.

#### bool IsArchive()

Determines whether the file is archive.

#### bool IsAudio(string extension)

Determine whether the extension is audio.

#### bool IsAudio()

Determine whether the extension is audio.

#### bool IsBook(string extension)

Determine whether the extension is book.

#### bool IsBook()

Determine whether the extension is book.

#### bool IsDoc(string extension)

Determine whether the extension is document.

#### bool IsDoc()

Determine whether the extension is document.

#### bool IsImage(string extension)

Determine whether the extension is image.

#### bool IsImage()

Determine whether the extension is image.

#### bool IsSource(string extension)

Determine whether the extension is source.

#### bool IsSource()

Determine whether the extension is source.

#### bool IsVideo(string extension)

Determine whether the extension is video.

#### bool IsVideo()

Determine whether the extension is video.

#### string GetFileContent()

Gets the file content

#### string GetRelativePath()

Gets the relative path of a file

#### string GetRelativePath(string basePath)

Gets the relative path of a file

#### byte[] Head(int length)

Gets head bytes of a file

#### byte[] Tail(int length)

Gets tail bytes of a file

#### byte[] GetFileBytes(long bytesCount, long offset)

Gets file bytes of a file

#### string Sha1File()

Computes Sha1 hash of a file

#### string Sha256File()

Computes Sha256 hash of a file

#### string Sha256File(FileInfo file)

Computes Sha256 hash of a file

#### string Md5File()

Computes Md5 hash of a file

#### string Base64File()

Turns file into base64 string

#### bool HasContent(string pattern)

Determine whether file has specific content

#### bool HasAttribute(long flags)

Determine whether file has attribute

#### string GetLinesContainingWord(string word)

Gets lines containing word

#### long GetFileLength(string unit)

Gets the file length

#### long GetLengthOfFile(string unit)

Gets the file length

#### string SubPath(int nesting)

Gets the SubPath from the path

#### string SubPath(int nesting)

Gets the SubPath from the path

#### string RelativeSubPath(int nesting)

Gets the relative SubPath from the path

#### string SubPath(string directoryPath, int nesting)

Gets the relative SubPath from the path

#### long Length(string unit)

Gets the length of the file

#### FileEntity GetFileInfo(string fullPath)

Gets the file info

#### FileEntity GetExtendedFileInfo()

Gets extended file info

#### long CountOfLines()

Gets the count of lines of a file

#### long CountOfNotEmptyLines()

Gets the count of non empty lines of a file

#### string Combine(string path1, string path2)

Combines the paths

#### string Combine(string path1, string path2, string path3)

Combines the paths

#### string Combine(string path1, string path2, string path3, string path4)

Combines the paths

#### string Combine(string path1, string path2, string path3, string path4, string path5)

Combines the paths

#### string Combine(string[] paths)

Combines the paths

#### string GetDirectoryName(string path)

Gets the directory name

#### string GetFileName(string path)

Gets the file name

#### string GetFileNameWithoutExtension(string path)

Gets the file name without extension

#### string GetExtension(string path)

Gets the extension

#### string GetMetadata(string directoryName, string tagName)

Gets the metadata of a file

#### string GetMetadata(string tagName)

Gets the metadata of a file

#### bool HasMetadataDirectory(string directoryName)

Checks whether file has metadata directory

#### bool HasMetadataTag(string directoryName, string tagName)

Checks whether file has metadata tag

#### bool HasMetadataTag(string tagName)

Checks whether file has metadata tag

#### string AllMetadataJson()

Gets all metadata of a file and returns it as json


## Standard Methods

Base methods constitute the fundamental function set inherited by all data sources in Musoq. These methods provide core functionality such as type conversions, mathematical operations, and string manipulations. They are automatically available alongside any data source-specific methods. The following base methods are supported:

### Aggregation methods

Aggregation methods process multiple rows to compute a single result value. These methods are used with GROUP BY clauses to summarize data across row groups. Below are the aggregation methods supported by this data source:
#### string AggregateValues(string name)

Aggregates values into a single value.

#### string AggregateValues(string name, int parent)

Aggregates values into a single value.

#### Decimal Avg(string name)

Gets the aggregated average value from the given group name

#### Decimal Avg(string name, int parent)

Gets the aggregated average value from the given group name

#### int Count(string name)

Gets the count value of a given group.

#### int Count(string name, int parent)

Gets the count value of a given group.

#### DateTimeOffset? MaxDateTimeOffset(string name, int parent)

Retrieves the maximum DateTimeOffset value from the specified group.

#### DateTimeOffset? MaxDateTimeOffset(string name)

Retrieves the maximum DateTimeOffset value from the specified group.

#### DateTimeOffset? MinDateTimeOffset(string name, int parent)

Retrieves the minimum DateTimeOffset value from the specified group.

#### DateTimeOffset? MinDateTimeOffset(string name)

Retrieves the minimum DateTimeOffset value from the specified group.

#### Decimal Max(string name)

Gets the max value of a given group.

#### Decimal Max(string name, int parent)

Gets the max value of a given group.

#### Decimal Min(string name)

Gets the min value of a given group.

#### Decimal Min(string name, int parent)

Gets the min value of a given group.

#### Decimal StDev(string name, int parent)

Gets the StDev value of a given group.

#### Decimal Sum(string name)

Gets the sum value of a given group.

#### Decimal Sum(string name, int parent)

Gets the sum value of a given group.

#### Decimal SumIncome(string name)

Gets the sum value of a given group.

#### Decimal SumIncome(string name, int parent)

Gets the sum value of a given group.

#### Decimal SumOutcome(string name)

Gets the outcome value of a given group.

#### Decimal SumOutcome(string name, int parent)

Gets the outcome value of a given group.

#### TimeSpan? SumTimeSpan(string name)

Gets the sum value of a given group.

#### TimeSpan? SumTimeSpan(string name, int parent)

Gets the sum value of a given group.

#### TimeSpan? MinTimeSpan(string name)

Gets the min value of a given group.

#### TimeSpan? MinTimeSpan(string name, int parent)

Gets the min value of a given group.

#### TimeSpan? MaxTimeSpan(string name)

Gets the max value of a given group.

#### TimeSpan? MaxTimeSpan(string name, int parent)

Gets the max value of a given group.

#### IEnumerable\<T\> Window\<T\>(string name)

Gets the window

### Non aggregation methods

Non-aggregation methods process data on a row-by-row basis, performing calculations or transformations for each individual row. These methods return a result for each input row. The following non-aggregation methods are available:
#### int RowNumber()

Gets the row number of the current row.

#### string GetTypeName(object obj)

Gets the typename of passed object.

#### byte? ShiftLeft(byte? value, int shift)

Shifts the value to the left by the specified number of bits.

#### short? ShiftLeft(short? value, int shift)

Shifts the value to the left by the specified number of bits.

#### int? ShiftLeft(int? value, int shift)

Shifts the value to the left by the specified number of bits.

#### long? ShiftLeft(long? value, int shift)

Shifts the value to the left by the specified number of bits.

#### sbyte? ShiftLeft(sbyte? value, int shift)

Shifts the value to the left by the specified number of bits.

#### ushort? ShiftLeft(ushort? value, int shift)

Shifts the value to the left by the specified number of bits.

#### uint? ShiftLeft(uint? value, int shift)

Shifts the value to the left by the specified number of bits.

#### ulong? ShiftLeft(ulong? value, int shift)

Shifts the value to the left by the specified number of bits.

#### byte? ShiftRight(byte? value, int shift)

Shifts the value to the right by the specified number of bits.

#### short? ShiftRight(short? value, int shift)

Shifts the value to the right by the specified number of bits.

#### int? ShiftRight(int? value, int shift)

Shifts the value to the right by the specified number of bits.

#### long? ShiftRight(long? value, int shift)

Shifts the value to the right by the specified number of bits.

#### sbyte? ShiftRight(sbyte? value, int shift)

Shifts the value to the right by the specified number of bits.

#### ushort? ShiftRight(ushort? value, int shift)

Shifts the value to the right by the specified number of bits.

#### uint? ShiftRight(uint? value, int shift)

Shifts the value to the right by the specified number of bits.

#### ulong? ShiftRight(ulong? value, int shift)

Shifts the value to the right by the specified number of bits.

#### byte? Not(byte? value)

Performs bitwise NOT operation on a given value.

#### short? Not(short? value)

Performs bitwise NOT operation on a given value.

#### int? Not(int? value)

Performs bitwise NOT operation on a given value.

#### long? Not(long? value)

Performs bitwise NOT operation on a given value.

#### sbyte? Not(sbyte? value)

Performs bitwise NOT operation on a given value.

#### ushort? Not(ushort? value)

Performs bitwise NOT operation on a given value.

#### uint? Not(uint? value)

Performs bitwise NOT operation on a given value.

#### ulong? Not(ulong? value)

Performs bitwise NOT operation on a given value.

#### byte? And(byte? left, byte? right)

Performs bitwise AND operation on two given values.

#### int? And(byte? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### int? And(byte? left, short? right)

Performs bitwise AND operation on two given values.

#### int? And(byte? left, ushort? right)

Performs bitwise AND operation on two given values.

#### int? And(byte? left, int? right)

Performs bitwise AND operation on two given values.

#### uint? And(byte? left, uint? right)

Performs bitwise AND operation on two given values.

#### long? And(byte? left, long? right)

Performs bitwise AND operation on two given values.

#### ulong? And(byte? left, ulong? right)

Performs bitwise AND operation on two given values.

#### int? And(sbyte? left, byte? right)

Performs bitwise AND operation on two given values.

#### sbyte? And(sbyte? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### int? And(sbyte? left, short? right)

Performs bitwise AND operation on two given values.

#### int? And(sbyte? left, ushort? right)

Performs bitwise AND operation on two given values.

#### int? And(sbyte? left, int? right)

Performs bitwise AND operation on two given values.

#### uint? And(sbyte? left, uint? right)

Performs bitwise AND operation on two given values.

#### long? And(sbyte? left, long? right)

Performs bitwise AND operation on two given values.

#### int? And(short? left, byte? right)

Performs bitwise AND operation on two given values.

#### int? And(short? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### short? And(short? left, short? right)

Performs bitwise AND operation on two given values.

#### int? And(short? left, ushort? right)

Performs bitwise AND operation on two given values.

#### int? And(short? left, int? right)

Performs bitwise AND operation on two given values.

#### uint? And(short? left, uint? right)

Performs bitwise AND operation on two given values.

#### long? And(short? left, long? right)

Performs bitwise AND operation on two given values.

#### int? And(ushort? left, byte? right)

Performs bitwise AND operation on two given values.

#### int? And(ushort? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### int? And(ushort? left, short? right)

Performs bitwise AND operation on two given values.

#### ushort? And(ushort? left, ushort? right)

Performs bitwise AND operation on two given values.

#### int? And(ushort? left, int? right)

Performs bitwise AND operation on two given values.

#### uint? And(ushort? left, uint? right)

Performs bitwise AND operation on two given values.

#### long? And(ushort? left, long? right)

Performs bitwise AND operation on two given values.

#### ulong? And(ushort? left, ulong? right)

Performs bitwise AND operation on two given values.

#### int? And(int? left, byte? right)

Performs bitwise AND operation on two given values.

#### int? And(int? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### int? And(int? left, short? right)

Performs bitwise AND operation on two given values.

#### int? And(int? left, ushort? right)

Performs bitwise AND operation on two given values.

#### int? And(int? left, int? right)

Performs bitwise AND operation on two given values.

#### uint? And(int? left, uint? right)

Performs bitwise AND operation on two given values.

#### long? And(int? left, long? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, uint? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, byte? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, short? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, ushort? right)

Performs bitwise AND operation on two given values.

#### uint? And(uint? left, int? right)

Performs bitwise AND operation on two given values.

#### ulong? And(uint? left, long? right)

Performs bitwise AND operation on two given values.

#### ulong? And(uint? left, ulong? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, long? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, byte? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, sbyte? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, short? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, ushort? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, int? right)

Performs bitwise AND operation on two given values.

#### long? And(long? left, uint? right)

Performs bitwise AND operation on two given values.

#### ulong? And(ulong? left, byte? right)

Performs bitwise AND operation on two given values.

#### ulong? And(ulong? left, ushort? right)

Performs bitwise AND operation on two given values.

#### ulong? And(ulong? left, uint? right)

Performs bitwise AND operation on two given values.

#### ulong? And(ulong? left, ulong? right)

Performs bitwise AND operation on two given values.

#### byte? Or(byte? left, byte? right)

Performs bitwise OR operation on two given values.

#### int? Or(byte? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### int? Or(byte? left, short? right)

Performs bitwise OR operation on two given values.

#### int? Or(byte? left, ushort? right)

Performs bitwise OR operation on two given values.

#### int? Or(byte? left, int? right)

Performs bitwise OR operation on two given values.

#### uint? Or(byte? left, uint? right)

Performs bitwise OR operation on two given values.

#### long? Or(byte? left, long? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(byte? left, ulong? right)

Performs bitwise OR operation on two given values.

#### int? Or(sbyte? left, byte? right)

Performs bitwise OR operation on two given values.

#### sbyte? Or(sbyte? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### int? Or(sbyte? left, short? right)

Performs bitwise OR operation on two given values.

#### int? Or(sbyte? left, ushort? right)

Performs bitwise OR operation on two given values.

#### int? Or(sbyte? left, int? right)

Performs bitwise OR operation on two given values.

#### uint? Or(sbyte? left, uint? right)

Performs bitwise OR operation on two given values.

#### long? Or(sbyte? left, long? right)

Performs bitwise OR operation on two given values.

#### int? Or(short? left, byte? right)

Performs bitwise OR operation on two given values.

#### int? Or(short? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### short? Or(short? left, short? right)

Performs bitwise OR operation on two given values.

#### int? Or(short? left, ushort? right)

Performs bitwise OR operation on two given values.

#### int? Or(short? left, int? right)

Performs bitwise OR operation on two given values.

#### uint? Or(short? left, uint? right)

Performs bitwise OR operation on two given values.

#### long? Or(short? left, long? right)

Performs bitwise OR operation on two given values.

#### int? Or(ushort? left, byte? right)

Performs bitwise OR operation on two given values.

#### int? Or(ushort? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### int? Or(ushort? left, short? right)

Performs bitwise OR operation on two given values.

#### ushort? Or(ushort? left, ushort? right)

Performs bitwise OR operation on two given values.

#### int? Or(ushort? left, int? right)

Performs bitwise OR operation on two given values.

#### uint? Or(ushort? left, uint? right)

Performs bitwise OR operation on two given values.

#### long? Or(ushort? left, long? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(ushort? left, ulong? right)

Performs bitwise OR operation on two given values.

#### int? Or(int? left, byte? right)

Performs bitwise OR operation on two given values.

#### int? Or(int? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### int? Or(int? left, short? right)

Performs bitwise OR operation on two given values.

#### int? Or(int? left, ushort? right)

Performs bitwise OR operation on two given values.

#### int? Or(int? left, int? right)

Performs bitwise OR operation on two given values.

#### uint? Or(int? left, uint? right)

Performs bitwise OR operation on two given values.

#### long? Or(int? left, long? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, uint? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, byte? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, short? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, ushort? right)

Performs bitwise OR operation on two given values.

#### uint? Or(uint? left, int? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(uint? left, long? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(uint? left, ulong? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, long? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, byte? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, sbyte? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, short? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, ushort? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, int? right)

Performs bitwise OR operation on two given values.

#### long? Or(long? left, uint? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(ulong? left, byte? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(ulong? left, ushort? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(ulong? left, uint? right)

Performs bitwise OR operation on two given values.

#### ulong? Or(ulong? left, ulong? right)

Performs bitwise OR operation on two given values.

#### byte? Xor(byte? left, byte? right)

Performs bitwise OR operation on two given values.

#### int? Xor(byte? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(byte? left, short? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(byte? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(byte? left, int? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(byte? left, uint? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(byte? left, long? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(byte? left, ulong? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(sbyte? left, byte? right)

Performs bitwise Xor operation on two given values.

#### sbyte? Xor(sbyte? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(sbyte? left, short? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(sbyte? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(sbyte? left, int? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(sbyte? left, uint? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(sbyte? left, long? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(short? left, byte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(short? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### short? Xor(short? left, short? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(short? left, ushort? right)

Performs bitwise XOR operation on two given values.

#### int? Xor(short? left, int? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(short? left, uint? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(short? left, long? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(ushort? left, byte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(ushort? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(ushort? left, short? right)

Performs bitwise Xor operation on two given values.

#### ushort? Xor(ushort? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(ushort? left, int? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(ushort? left, uint? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(ushort? left, long? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(ushort? left, ulong? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(int? left, byte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(int? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(int? left, short? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(int? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### int? Xor(int? left, int? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(int? left, uint? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(int? left, long? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, uint? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, byte? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, short? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### uint? Xor(uint? left, int? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(uint? left, long? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(uint? left, ulong? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, long? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, byte? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, sbyte? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, short? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, int? right)

Performs bitwise Xor operation on two given values.

#### long? Xor(long? left, uint? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(ulong? left, ulong? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(ulong? left, byte? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(ulong? left, ushort? right)

Performs bitwise Xor operation on two given values.

#### ulong? Xor(ulong? left, uint? right)

Performs bitwise Xor operation on two given values.

#### byte[] GetBytes(string content)

Gets the bytes from the given string.

#### byte[] GetBytes(string content, int length, int offset)

Gets the bytes from the given string within given offset and length.

#### byte[] GetBytes(char? character)

Gets the bytes from the given character.

#### byte[] GetBytes(bool? bit)

Gets the bytes from the given boolean.

#### byte[] GetBytes(long? value)

Gets the bytes from the given long.

#### byte[] GetBytes(int? value)

Gets the bytes from the given int.

#### byte[] GetBytes(short? value)

Gets the bytes from the given short.

#### byte[] GetBytes(ulong? value)

Gets the bytes from the given ulong.

#### byte[] GetBytes(ushort? value)

Gets the bytes from the given ushort.

#### byte[] GetBytes(uint? value)

Gets the bytes from the given uint.

#### byte[] GetBytes(Decimal? value)

Gets the bytes from the given decimal.

#### byte[] GetBytes(double? value)

Gets the bytes from the given double.

#### byte[] GetBytes(float? value)

Gets the bytes from the given float.

#### string ToBin(byte[] bytes, string delimiter)

Converts given bytes to binary with defined delimiter

#### string ToBin\<T\>(T value)

Converts given value to binary

#### string ToOcta\<T\>(T value)

Converts given value to octal

#### string ToDec\<T\>(T value)

Converts given value to decimal

#### string ToBase64(byte[] value)

Converts given value to string

#### string ToBase64(byte[] value, int offset, int length)

Converts given array of bytes to string

#### byte[] FromBase64(string value)

Converts given string to bytes array

#### bool FromBytesToBool(byte[] value)

Converts bytes to boolean value.

#### short FromBytesToInt16(byte[] value)

Converts bytes to a 16-bit signed integer.

#### ushort FromBytesToUInt16(byte[] value)

Converts bytes to a 16-bit unsigned integer.

#### int FromBytesToInt32(byte[] value)

Converts bytes to a 32-bit signed integer.

#### uint FromBytesToUInt32(byte[] value)

Converts bytes to a 32-bit unsigned integer.

#### long FromBytesToInt64(byte[] value)

Converts bytes to a 64-bit signed integer.

#### ulong FromBytesToUInt64(byte[] value)

Converts bytes to a 64-bit unsigned integer.

#### float FromBytesToFloat(byte[] value)

Converts bytes to a float value.

#### double FromBytesToDouble(byte[] value)

Converts bytes to a float value.

#### string FromBytesToString(byte[] value)

Converts bytes to a string.

#### byte[] ToBytes(bool value)

Converts boolean value to bytes.

#### byte[] ToBytes(short value)

Converts short value to bytes.

#### byte[] ToBytes(ushort value)

Converts ushort value to bytes.

#### byte[] ToBytes(int value)

Converts int value to bytes.

#### byte[] ToBytes(uint value)

Converts uint value to bytes.

#### byte[] ToBytes(long value)

Converts long value to bytes.

#### byte[] ToBytes(ulong value)

Converts long value to bytes.

#### string ToHex(byte[] bytes, string delimiter)

Converts given bytes to hex with defined delimiter

#### string ToHex\<T\>(T value)

Converts given value to binary

#### int ExtractFromDate(string date, string partOfDate)

Extracts part of the date from the date

#### int ExtractFromDate(string date, string culture, string partOfDate)

Extracts part of the date from the date based on given culture

#### DateTimeOffset? GetDate()

Gets the current datetime

#### DateTimeOffset? UtcGetDate()

Gets the current datetime in UTC

#### int? Month(DateTimeOffset? value)

Gets the month from DateTimeOffset

#### int? Year(DateTimeOffset? value)

Gets the year from DateTimeOffset

#### int? Day(DateTimeOffset? value)

Gets the day from DateTimeOffset

#### int? Hour(DateTimeOffset? value)

Gets the hour from DateTimeOffset

#### int? Minute(DateTimeOffset? value)

Gets the minute from DateTimeOffset

#### int? Second(DateTimeOffset? value)

Gets the second from DateTimeOffset

#### int? Milliseconds(DateTimeOffset? value)

Gets the millisecond from DateTimeOffset

#### int? DayOfWeek(DateTimeOffset? value)

Gets the day of week from DateTimeOffset

#### TimeSpan? ExtractTimeSpan(DateTimeOffset? dateTimeOffset)

Extracts time from DateTimeOffset

#### DateTimeOffset? ToDateTimeOffset(string value)

Converts the given value to DateTimeOffset using the current culture.

#### DateTimeOffset? ToDateTimeOffset(string value, string culture)

Converts the given value to DateTimeOffset using the specified culture.

#### TimeSpan? SubtractDateTimeOffsets(DateTimeOffset? first, DateTimeOffset? second)

Subtracts the first DateTimeOffset from the second DateTimeOffset.

#### IEnumerable\<T\> Skip\<T\>(IEnumerable\<T\> values, int skipCount)

Skips elements from the beginning of the sequence.

#### IEnumerable\<T\> Take\<T\>(IEnumerable\<T\> values, int takeCount)

Takes elements from the beginning of the sequence.

#### IEnumerable\<T\> SkipAndTake\<T\>(IEnumerable\<T\> values, int skipCount, int takeCount)

Skip and takes elements from the beginning of the sequence.

#### T[] EnumerableToArray\<T\>(IEnumerable\<T\> values)

Turn array arguments of T into a single array.

#### T[] MergeArrays\<T\>(T[][] values)

Turn array arguments of T into a single array.

#### IEnumerable\<T\> LongestCommonSequence\<T\>(IEnumerable\<T\> source, IEnumerable\<T\> pattern)

Computes longest common sequence of two given sequences

#### T GetElementAtOrDefault\<T\>(IEnumerable\<T\> enumerable, int? index)

Gets the element at the specified index in a sequence

#### int? Length\<T\>(IEnumerable\<T\> enumerable)

Gets the length of the sequence

#### int? Length\<T\>(T[] array)

Gets the length of the array

#### T Choose\<T\>(int index, T[] values)

Gets the value of an array at the specified index

#### T If\<T\>(bool expressionResult, T a, T b)

Chose a or b value based on the expression result

#### bool? Match(string regex, string content)

Determine whether content matches the specified pattern

#### byte? Coalesce(byte?[] array)

Gets the first non-null value in a list

#### sbyte? Coalesce(sbyte?[] array)

Gets the first non-null value in a list

#### short? Coalesce(short?[] array)

Gets the first non-null value in a list

#### ushort? Coalesce(ushort?[] array)

Gets the first non-null value in a list

#### int? Coalesce(int?[] array)

Gets the first non-null value in a list

#### Decimal? Coalesce(uint?[] array)

Gets the first non-null value in a list

#### Decimal? Coalesce(long?[] array)

Gets the first non-null value in a list

#### Decimal? Coalesce(ulong?[] array)

Gets the first non-null value in a list

#### Decimal? Coalesce(Decimal?[] array)

Gets the first non-null value in a list

#### T Coalesce\<T\>(T[] array)

Gets the first non-null value in a list

#### IEnumerable\<T\> Distinct\<T\>(IEnumerable\<T\> values)

Returns distinct elements from a collection.

#### T FirstOrDefault\<T\>(IEnumerable\<T\> values)

Returns the first element of a sequence, or a default value if the sequence contains no elements.

#### T LastOrDefault\<T\>(IEnumerable\<T\> values)

Returns the last element of a sequence, or a default value if the sequence contains no elements.

#### T NthOrDefault\<T\>(IEnumerable\<T\> values, int index)

Returns the element at a specified index in a sequence or a default value if the index is out of range.

#### T NthFromEndOrDefault\<T\>(IEnumerable\<T\> values, int index)

Returns the element at a specified index from the end of a sequence or a default value if the index is out of range.

#### string Md5(string content)

Gets the md5 hash of the given string.

#### string Md5(byte[] content)

Gets the md5 hash of the given bytes array.

#### string Sha1(string content)

Gets the sha256 hash of the given string.

#### string Sha1(byte[] content)

Gets the sha256 hash of the given bytes array.

#### string Sha256(string content)

Gets the sha256 hash of the given string.

#### string Sha256(byte[] content)

Gets the sha256 hash of the given bytes array.

#### string Sha512(string content)

Gets the sha256 hash of the given string.

#### string Sha512(byte[] content)

Gets the sha256 hash of the bytes array.

#### string ToJson\<T\>(T obj)

Converts object to json.

#### string[] ExtractFromJsonToArray(string json, string path)

Extracts values from json by path.

#### string ExtractFromJson(string json, string path)

Extracts values from json by path and joins them with comma.

#### Decimal? Abs(Decimal? value)

Gets the absolute value

#### long? Abs(long? value)

Gets the absolute value

#### int? Abs(int? value)

Gets the absolute value

#### Decimal? Ceil(Decimal? value)

Gets the ceiling value

#### Decimal? Floor(Decimal? value)

Gets the floor value

#### Decimal? Sign(Decimal? value)

Determine whether value is greater, equal or less that zero

#### long? Sign(long? value)

Determine whether value is greater, equal or less that zero

#### Decimal? Round(Decimal? value, int precision)

Rounds the value within given precision

#### Decimal? PercentOf(Decimal? value, Decimal? max)

Gets the percentage of the value

#### int Rand()

Gets the random integer value

#### int? Rand(int? min, int? max)

Gets the random integer value

#### double? Pow(Decimal? x, Decimal? y)

Computes the pow between two values

#### double? Pow(double? x, double? y)

Computes the pow between two values

#### double? Sqrt(Decimal? x)

Computes the sqrt of a given value

#### double? Sqrt(double? x)

Computes the sqrt of a given value

#### double? Sqrt(long? x)

Computes the sqrt of a given value

#### double? PercentRank\<T\>(IEnumerable\<T\> window, T value)

Computes the percent rank of a given window

#### double? Log(Decimal? base, Decimal? value)

Calculates the logarithm of a value with a specified base.

#### Decimal? Sin(Decimal? value)

Calculates sine of a value.

#### double? Sin(double? value)

Calculates sine of a value.

#### float? Sin(float? value)

Calculates sine of a value.

#### Decimal? Cos(Decimal? value)

Calculates cosine of a value.

#### double? Cos(double? value)

Calculates cosine of a value.

#### float? Cos(float? value)

Calculates cosine of a value.

#### string NewId()

Gets the new identifier

#### string Trim(string value)

Removes leading and trailing whitespace from a string.

#### string TrimStart(string value)

Removes leading whitespace from a string.

#### string TrimEnd(string value)

Removes trailing whitespace from a string.

#### string Substring(string value, int? index, int? length)

Gets the substring from the string.

#### string Substring(string value, int? length)

Gets the substring from the string

#### string Concat(string[] strings)

Concatenates the specified values

#### string Concat(char[] characters)

Concatenates the specified characters

#### string Concat(string firstString, char[] chars)

Concatenates specified string first characters

#### string Concat(char? firstChar, string[] strings)

Concatenate specific character with strings

#### string Concat(object[] objects)

Concatenates the specified strings

#### string Concat\<T\>(T[] objects)

Concatenates the specified strings

#### bool? Contains(string content, string what)

Determine whether the string contains the specified value

#### int? IndexOf(string value, string text)

Position of the first occurrence of the specified value

#### int? NthIndexOf(string value, string text, int index)

Position of the nth occurrence of the specified value

#### int? LastIndexOf(string value, string text)

Position of the last occurrence of the specified pattern

#### string Soundex(string value)

Computes soundex for the specified value

#### bool HasFuzzyMatchedWord(string text, string word, string separator)

Matches the specified text by splitting it with separator and applying fuzzy comparison

#### bool HasWordThatHasSmallerLevenshteinDistanceThan(string text, string word, int distance, string separator)

Matches the specified text by splitting it with separator and applying fuzzy comparison with a given distance

#### bool HasWordThatSoundLike(string text, string word, string separator)

Matches whether the specified word is present after being fuzzified within the specified text

#### bool HasTextThatSoundLikeSentence(string text, string sentence, string separator)

Matches whether the specified text is present in sentence after being fuzified

#### string ToUpper(string value)

Makes the string uppercase

#### string ToUpper(string value, string culture)

Makes the string uppercase within specified culture

#### string ToUpperInvariant(string value)

Makes the string uppercase

#### string ToLower(string value)

Makes the string lowercase

#### string ToLower(string value, string culture)

Makes the string lowercase within specified culture

#### string ToLowerInvariant(string value)

Makes the string lowercase

#### string PadLeft(string value, string character, int? totalWidth)

Returns a new string that right-aligns the characters in this instance by padding them on the left with a specified Unicode character, for a specified total lengt

#### string PadRight(string value, string character, int? totalWidth)

Returns a new string that left-aligns the characters in this instance by padding them on the right with a specified Unicode character, for a specified total length

#### string Head(string value, int? length)

Gets the first N characters of the string

#### string Tail(string value, int? length)

Gets the last N characters of the string

#### int? LevenshteinDistance(string firstValue, string secondValue)

Computes the Levenshtein distance between two strings

#### char? GetCharacterOf(string value, int index)

Gets the character at specified index

#### string Reverse(string value)

Reverses the string

#### string[] Split(string value, string[] separators)

Splits the string into an array of substrings based on the specified separators

#### char[] ToCharArray(string value)

Splits the string into an array of characters

#### string LongestCommonSubstring(string source, string pattern)

Computes the longest common subsequence between two source and pattern

#### string Replicate(string value, int integer)

Clones the value n times

#### string Translate(string value, string characters, string translations)

Returns the string from the first argument after the characters specified in the second argument are translated into the characters specified in the third argument.

#### string Replace(string text, string lookFor, string changeTo)

Replaces the first occurrence of a specified string in this instance with another specified string

#### string ToTitleCase(string value)

Capitalizes the first letter of the string

#### string GetNthWord(string text, int wordIndex, string separator)

Gets the nth word of the string

#### string GetFirstWord(string text, string separator)

Gets the first word of the string

#### string GetSecondWord(string text, string separator)

Gets the second word of the string

#### string GetThirdWord(string text, string separator)

Gets the third word of the string

#### string GetLastWord(string text, string separator)

Gets last word of the string

#### bool IsNullOrEmpty(string value)

Determines whether the string is null or empty

#### bool IsNullOrWhiteSpace(string value)

Determines whether the string is null or whitespace

#### string UrlEncode(string value)

Encodes the value

#### string UrlDecode(string value)

Decodes the value

#### string UriEncode(string value)

Encodes the value

#### string UriDecode(string value)

Decodes the value

#### bool? StartsWith(string value, string prefix)

Determines whether the string starts with the specified prefix

#### bool? StartsWith(string value, string prefix, string comparison)

Determines whether the string starts with the specified prefix

#### bool? EndsWith(string value, string suffix)

Determines whether the string ends with the specified suffix

#### bool? EndsWith(string value, string suffix, string comparison)

Determines whether the string ends with the specified suffix

#### string RegexReplace(string value, string pattern, string replacement)

Replace the specified value part that matches the pattern with the replacement

#### string[] SplitByLinuxNewLines(string input)

Split string by Linux-style newlines (\n)

#### string[] SplitByWindowsNewLines(string input)

Split string by Windows-style newlines (\r\n)

#### string[] SplitByNewLines(string input)

Smart split that handles both Windows (\r\n) and Linux (\n) newlines

#### string StringsJoin(string separator, string[] values)

Joins the specified values with the separator

#### string StringsJoin(string separator, IEnumerable\<string\> values)

Joins the specified values with the separator

#### TimeSpan? AddTimeSpans(TimeSpan?[] timeSpans)

Adds a given set of time spans.

#### TimeSpan? SubtractTimeSpans(TimeSpan?[] timeSpans)

Subtracts a given set of time spans.

#### TimeSpan? FromString(string timeSpan)

Turns a string into a time span.

#### char? ToChar(string value)

Converts given value to character

#### char? ToChar(int? value)

Converts given value to character

#### char? ToChar(short? value)

Converts given value to character

#### char? ToChar(byte? value)

Converts given value to character

#### char? ToChar(object value)

Converts given value to character

#### DateTime? ToDateTime(string value)

Converts given value to DateTime

#### DateTime? ToDateTime(string value, string culture)

Converts given value to DateTime

#### TimeSpan? SubtractDates(DateTime? date1, DateTime? date2)

Subtracts two DateTime values and returns the difference as a TimeSpan

#### Decimal? ToDecimal(string value)

Converts given value to decimal

#### Decimal? ToDecimal(string value, string culture)

Converts given value to decimal withing given culture

#### Decimal? ToDecimal(byte? value)

Converts given value to Decimal

#### Decimal? ToDecimal(sbyte? value)

Converts given value to Decimal

#### Decimal? ToDecimal(short? value)

Converts given value to Decimal

#### Decimal? ToDecimal(ushort? value)

Converts given value to Decimal

#### Decimal? ToDecimal(long? value)

Converts given value to Decimal

#### Decimal? ToDecimal(ulong? value)

Converts given value to Decimal

#### Decimal? ToDecimal(float? value)

Converts given value to Decimal

#### Decimal? ToDecimal(double? value)

Converts given value to Decimal

#### Decimal? ToDecimal(object value)

Converts given value to Decimal

#### double? ToDouble(object value)

Converts given value to double

#### double? ToDouble(string value)

Converts given value to double

#### double? ToDouble(byte? value)

Converts given value to double

#### double? ToDouble(sbyte? value)

Converts given value to double

#### double? ToDouble(short? value)

Converts given value to double

#### double? ToDouble(ushort? value)

Converts given value to double

#### double? ToDouble(int? value)

Converts given value to double

#### double? ToDouble(uint? value)

Converts given value to double

#### double? ToDouble(long? value)

Converts given value to double

#### double? ToDouble(ulong? value)

Converts given value to double

#### double? ToDouble(float? value)

Converts given value to double

#### double? ToDouble(double? value)

Converts given value to double

#### double? ToDouble(Decimal? value)

Converts given value to double

#### float? ToFloat(string value)

Converts given value to float

#### float? ToFloat(byte? value)

Converts given value to float

#### float? ToFloat(sbyte? value)

Converts given value to float

#### float? ToFloat(short? value)

Converts given value to float

#### float? ToFloat(ushort? value)

Converts given value to float

#### float? ToFloat(int? value)

Converts given value to float

#### float? ToFloat(uint? value)

Converts given value to float

#### float? ToFloat(long? value)

Converts given value to float

#### float? ToFloat(ulong? value)

Converts given value to float

#### float? ToFloat(float? value)

Converts given value to float

#### float? ToFloat(Decimal? value)

Converts given value to float

#### int? ToInt32(string value)

Converts given value to int

#### int? ToInt32(byte? value)

Converts given value to int

#### int? ToInt32(sbyte? value)

Converts given value to int

#### int? ToInt32(short? value)

Converts given value to int

#### int? ToInt32(ushort? value)

Converts given value to int

#### int? ToInt32(int? value)

Converts given value to int

#### int? ToInt32(uint? value)

Converts given value to int

#### int? ToInt32(long? value)

Converts given value to int

#### int? ToInt32(ulong? value)

Converts given value to int

#### int? ToInt32(float? value)

Converts given value to int

#### int? ToInt32(double? value)

Converts given value to int

#### int? ToInt32(Decimal? value)

Converts given value to int

#### int? ToInt32(object value)

Converts given value to int

#### long? ToInt64(string value)

Converts given value to Int64

#### long? ToInt64(byte? value)

Converts given value to long

#### long? ToInt64(sbyte? value)

Converts given value to long

#### long? ToInt64(short? value)

Converts given value to long

#### long? ToInt64(ushort? value)

Converts given value to long

#### long? ToInt64(int? value)

Converts given value to long

#### long? ToInt64(uint? value)

Converts given value to long

#### long? ToInt64(long? value)

Converts given value to long

#### long? ToInt64(ulong? value)

Converts given value to long

#### long? ToInt64(float? value)

Converts given value to long

#### long? ToInt64(double? value)

Converts given value to long

#### long? ToInt64(Decimal? value)

Converts given value to long

#### long? ToInt64(object value)

Converts given value to long

#### string ToString(char? value)

Converts given value to string

#### string ToString(DateTimeOffset? value)

Converts given value to string

#### string ToString(DateTimeOffset? value, string format)

Converts given value to string

#### string ToString(DateTimeOffset? value, string format, string culture)

Converts given value to string

#### string ToString(byte? value)

Converts given value to string

#### string ToString(byte? value, string format)

Converts given value to string

#### string ToString(sbyte? value)

Converts given value to string

#### string ToString(sbyte? value, string format)

Converts given value to string

#### string ToString(int? value)

Converts given value to string

#### string ToString(int? value, string format)

Converts given value to string

#### string ToString(uint? value)

Converts given value to string

#### string ToString(uint? value, string format)

Converts given value to string

#### string ToString(long? value)

Converts given value to string

#### string ToString(long? value, string format)

Converts given value to string

#### string ToString(ulong? value)

Converts given value to string

#### string ToString(ulong? value, string format)

Converts given value to string

#### string ToString(float? value)

Converts given value to string

#### string ToString(float? value, string format)

Converts given value to string

#### string ToString(double? value)

Converts given value to string

#### string ToString(double? value, string format)

Converts given value to string

#### string ToString(Decimal? value)

Converts given value to string

#### string ToString(Decimal? value, string format)

Converts given value to string

#### string ToString(bool? value)

Converts given value to string

#### string ToString(object value)

Converts given value to string

#### string ToString\<T\>(T value)

Converts given value to string

#### string ToString(string[] value)

Converts given value to string

#### string ToString\<T\>(T[] value)

Converts given value to string

#### TimeSpan? ToTimeSpan(string value)

Converts given value to TimeSpan

#### TimeSpan? ToTimeSpan(string value, string culture)

Converts given value to TimeSpan

