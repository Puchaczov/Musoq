---
layout: page
title: Documentation
permalink: /documentation/
---

### Constructors

#### Musoq.Schema.Ocr.OcrLibrary

 - #ocr.single()

#### Musoq.Schema.Media.MediaLibrary

 - #media.audio(pathToDirectory: string, recursive: bool)
 - #media.video(pathToDirectory: string, recursive: bool)
 - #media.photo(pathToDirectory: string, recursive: bool)
 
#### Musoq.Schema.Json.JsonLibrary

#### Musoq.Schema.Csv.CsvLibrary

 - #csv.file(pathToFile: string, delimiter: string, hasHeader: boolean, skipLines: integer)

#### Musoq.Schema.Text.TextLibrary

 - #text.file(pathToFile: string)

### Methods

#### Musoq.Plugins.LibraryBase

 - T GetElementAt (IEnumerable<T> enumerable, int index)
 - T Choose (int index, T[] values)
 - T If (bool expresionResult, T a, T b)
 - bool Match (string regex, string content)
 - Decimal? Coalesce (Decimal?[] array)
 - Int64? Coalesce (Int64?[] array)
 - T Coalesce (T[] array)
 - Decimal? Abs (Decimal? value)
 - Int64? Abs (Int64? value)
 - Int32? Abs (Int32? value)
 - Decimal? Ceil (Decimal? value)
 - Decimal? Floor (Decimal? value)
 - Decimal? Sign (Decimal? value)
 - Int64? Sign (Int64? value)
 - Decimal? Round (Decimal? value, int precision)
 - Decimal? PercentOf (Decimal? value, Decimal? max)
 - int Rand ()
 - int Rand (int min, int max)
 - double Pow (decimal x, decimal y)
 - double Pow (double x, double y)
 - double Sqrt (decimal x)
 - double Sqrt (double x)
 - double PercentRank (IEnumerable<T> window, T value)
 - string Substring (string value, int index, int length)
 - string Substring (string value, int length)
 - string Concat (string[] strings)
 - string Concat (System.Char[] strings)
 - string Concat (string firstString, System.Char[] chars)
 - string Concat (System.Char firstChar, string[] strings)
 - string Concat (object[] objects)
 - bool Contains (string content, string what)
 - int IndexOf (string value, string text)
 - string Soundex (string value)
 - bool HasWordThatSoundLike (string text, string word, string separator)
 - bool HasTextThatSoundLikeSentence (string text, string sentence, string separator)
 - string ToUpperInvariant (string value)
 - string ToLowerInvariant (string value)
 - string PadLeft (string value, string character, int totalWidth)
 - string PadRight (string value, string character, int totalWidth)
 - string Head (string value, int length)
 - string Tail (string value, int length)
 - Int32? LevenshteinDistance (string firstValue, string secondValue)
 - Char? GetCharacterOf (string value, int index)
 - string Reverse (string value)
 - string[] Split (string value, string[] separators)
 - string LongestCommonSubstring (string source, string pattern)
 - string Replicate (string value, int integer)
 - string Translate (string value, string characters, string translations)
 - string CapitalizeFirstLetterOfWords (string value)
 - int RowNumber ()
 - string Sha512 (string content)
 - string Sha256 (string content)
 - string Md5 (string content)
 - string GetTypeName (object obj)
 - System.Byte[] GetBytes (string content)
 - System.Byte[] GetBytes (string content, int length, int offset)
 - System.Byte[] GetBytes (System.Char character)
 - System.Byte[] GetBytes (bool bit)
 - System.Byte[] GetBytes (long value)
 - System.Byte[] GetBytes (int value)
 - System.Byte[] GetBytes (short value)
 - System.Byte[] GetBytes (System.UInt64 value)
 - System.Byte[] GetBytes (System.UInt16 value)
 - System.Byte[] GetBytes (System.UInt32 value)
 - System.Byte[] GetBytes (decimal value)
 - System.Byte[] GetBytes (double value)
 - System.Byte[] GetBytes (System.Single value)
 - string ToHex (System.Byte[] bytes, string delimiter)
 - string ToHex (T value)
 - string ToBin (T value)
 - string ToOcta (T value)
 - string ToDec (T value)
 - Decimal? ToDecimal (string value)
 - Decimal? ToDecimal (string value, string culture)
 - Decimal? ToDecimal (Int64? value)
 - Int64? ToLong (string value)
 - Int32? ToInt (string value)
 - Char? ToChar (string value)
 - Char? ToChar (Int32? value)
 - Char? ToChar (Int16? value)
 - Char? ToChar (Byte? value)
 - string ToString (Char? character)
 - string ToString (DateTimeOffset? date)
 - string ToString (Decimal? value)
 - string ToString (Int64? value)
 - string ToString (object obj)
 - string ToString (T obj)
 - string ToBase64 (System.Byte[] array)
 - string ToBase64 (System.Byte[] array, int offset, int length)
 - System.Byte[] FromBase64 (string base64String)
 - int ExtractFromDate (string date, string partOfDate)
 - int ExtractFromDate (string date, string culture, string partOfDate)
 - DateTimeOffset? GetDate ()
 - DateTimeOffset? UtcGetDate ()
 - Int32? Month (DateTimeOffset? value)
 - Int32? Year (DateTimeOffset? value)
 - Int32? Day (DateTimeOffset? value)
 - IEnumerable<T> Skip (IEnumerable<T> values, int skipCount)
 - IEnumerable<T> Take (IEnumerable<T> values, int takeCount)
 - IEnumerable<T> SkipAndTake (IEnumerable<T> values, int skipCount, int takeCount)
 - T[] ToArray (IEnumerable<T> values)
 - IEnumerable<T> LongestCommonSequence (IEnumerable<T> source, IEnumerable<T> pattern)
 - IEnumerable<T> Window (T value, int parent) [This is aggregation method]
 - decimal Max (Int64? value, int parent) [This is aggregation method]
 - decimal Max (Decimal? value, int parent) [This is aggregation method]
 - decimal Min (Decimal? value, int parent) [This is aggregation method]
 - decimal StDev (Double? value, int parent) [This is aggregation method]
 - decimal StDev (Decimal? value, int parent) [This is aggregation method]
 - decimal Sum (Decimal? number, int parent) [This is aggregation method]
 - decimal Sum (Int64? number, int parent) [This is aggregation method]
 - decimal SumIncome (Int64? number, int parent) [This is aggregation method]
 - decimal SumIncome (Decimal? number, int parent) [This is aggregation method]
 - decimal SumOutcome (Decimal? number, int parent) [This is aggregation method]
 - string AggregateValues (string value, int parent) [This is aggregation method]
 - string AggregateValues (Decimal? value, int parent) [This is aggregation method]
 - string AggregateValues (Int64? value, int parent) [This is aggregation method]
 - string AggregateValues (DateTimeOffset? value, int parent) [This is aggregation method]
 - string AggregateValues (DateTime? value, int parent) [This is aggregation method]
 - decimal Avg (Int64? value, int parent) [This is aggregation method]
 - decimal Avg (Decimal? value, int parent) [This is aggregation method]
 - int Count (string value, int parent) [This is aggregation method]
 - int Count (Decimal? value, int parent) [This is aggregation method]
 - int Count (DateTimeOffset? value, int parent) [This is aggregation method]
 - int Count (DateTime? value, int parent) [This is aggregation method]
 - int Count (Int64? value, int parent) [This is aggregation method]
 - int Count (Int32? value, int parent) [This is aggregation method]
 - int Count (Boolean? value, int parent) [This is aggregation method]
 
#### Musoq.Schema.Text.TextLibrary
 
 - bool IsZipArchive (string extension)
 - bool IsZipArchive (System.IO.FileInfo fileInfo)
 - bool IsArchive (string extension)
 - bool IsArchive (System.IO.FileInfo fileInfo)
 - bool IsAudio (string extension)
 - bool IsAudio (System.IO.FileInfo fileInfo)
 - bool IsBook (string extension)
 - bool IsBook (System.IO.FileInfo fileInfo)
 - bool IsDoc (string extension)
 - bool IsDoc (System.IO.FileInfo fileInfo)
 - bool IsImage (string extension)
 - bool IsImage (System.IO.FileInfo fileInfo)
 - bool IsSource (string extension)
 - bool IsSource (System.IO.FileInfo fileInfo)
 - bool IsVideo (string extension)
 - bool IsVideo (System.IO.FileInfo fileInfo)
 - string GetFileContent (System.IO.FileInfo fileInfo)
 - string GetRelativeName (System.IO.FileInfo fileInfo, string basePath)
 - System.Byte[] Head (System.IO.FileInfo file, int length)
 - System.Byte[] Tail (System.IO.FileInfo file, int length)
 - System.Byte[] GetFileBytes (System.IO.FileInfo file, long bytesCount, long offset)
 - string Sha1File (System.IO.FileInfo file)
 - string Sha256File (System.IO.FileInfo file)
 - string Md5File (System.IO.FileInfo file)
 - bool HasContent (System.IO.FileInfo file, string pattern)
 - bool HasAttribute (System.IO.FileInfo file, long flags)
 - string GetLinesContainingWord (System.IO.FileInfo file, string word)
 - long Format (System.IO.FileInfo context, string unit)
 - long CountOfLines (System.IO.FileInfo context)
 - long CountOfNotEmptyLines (System.IO.FileInfo context)
 - string Compress (IReadOnlyList<System.IO.DirectoryInfo> directories, string path, string method)
 - string Compress (IReadOnlyList<System.IO.FileInfo> files, string path, string method)
 - string Decompress (IReadOnlyList<System.IO.FileInfo> files, string path)
 - string Combine (string path1, string path2)
 - string Combine (string path1, string path2, string path3)
 - string Combine (string path1, string path2, string path3, string path4)
 - string Combine (string path1, string path2, string path3, string path4, string path5)
 - string Combine (string[] paths)
 - IReadOnlyList<System.IO.FileInfo> AggregateFiles (System.IO.FileInfo file) [This is aggregation method]
 - IReadOnlyList<System.IO.FileInfo> AggregateFiles (string name) [This is aggregation method]
 - IReadOnlyList<System.IO.DirectoryInfo> AggregateDirectories (string name) [This is aggregation method]
 
#### Musoq.Schema.Ocr.OcrLibrary

 - string GetText (System.IO.FileInfo fileInfo, string lang)
 - string GetText (string filePath, string lang)

#### Musoq.Schema.Media.MediaLibrary

 - System.IO.FileInfo GetFileInfo (Musoq.Schema.Media.MediaFile mediaFile)

#### Musoq.Schema.Json.JsonLibrary

 - int Length (Newtonsoft.Json.Linq.JArray array)
 - string MakeFlat (Newtonsoft.Json.Linq.JArray array)

#### Musoq.Schema.Csv.CsvLibrary

 - string ClusteredByContainsKey (string dictionaryFilename, string value)
