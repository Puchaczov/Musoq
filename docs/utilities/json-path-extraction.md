---
title: JSON Path Extraction
layout: default
parent: Utilities
nav_order: 1
---

# JSON Path Extraction
## Overview

Two methods are provided for extracting values from JSON using path expressions:

1. `ExtractFromJsonToArray` - Returns extracted values as a string array
2. `ExtractFromJson` - Returns extracted values joined with commas as a single string

## Path Syntax

### Basic Structure

- Paths can start with `$` (optional) representing the root
- Properties can be accessed using dot notation or bracket notation
- Property names with special characters must use bracket notation with quotes

### Supported Notations

1. **Dot Notation**

   ```
   $.property
   $.nested.property
   ```

2. **Bracket Notation**

   ```
   $['property']
   $["property"]
   ```

3. **Array Access**

   ```
   $.array[0]        // Access specific index
   $.array[*]        // Access all elements
   ```

4. **Mixed Notation**

   ```
   $.nested['property']
   $['nested'].property
   ```

### Special Cases

1. **Properties Containing Dots**

   ```
   $['property.with.dots']
   $.nested['complex.name']
   ```

2. **Properties Containing Brackets**

   ```
   $['property[with]brackets']
   $.nested['array[0]name']
   ```

3. **Complex Nested Paths**

   ```
   $.nested['complex[name].with.dot']
   $.nested.array[0]['special.name[0]']
   ```

## Usage Examples

### 1. Simple Property Access

```csharp
var json = @"{
    ""name"": ""John"",
    ""age"": 30
}";

// Both return ["John"]
ExtractFromJsonToArray(json, "$.name")
ExtractFromJsonToArray(json, "$['name']")

// Returns "John"
ExtractFromJson(json, "$.name")
```

### 2. Nested Properties

```csharp
var json = @"{
    ""user"": {
        ""details"": {
            ""name"": ""John""
        }
    }
}";

// Returns ["John"]
ExtractFromJsonToArray(json, "$.user.details.name")

// Returns "John"
ExtractFromJson(json, "$.user.details.name")
```

### 3. Array Access

```csharp
var json = @"{
    ""users"": [
        { ""name"": ""John"" },
        { ""name"": ""Jane"" }
    ]
}";

// Returns ["John", "Jane"]
ExtractFromJsonToArray(json, "$.users[*].name")

// Returns "John,Jane"
ExtractFromJson(json, "$.users[*].name")
```

### 4. Special Characters in Property Names

```csharp
var json = @"{
    ""user.name"": ""John"",
    ""special[key]"": ""value"",
    ""nested"": {
        ""complex[name].with.dot"": ""data""
    }
}";

// All valid path expressions:
ExtractFromJson(json, "$['user.name']")
ExtractFromJson(json, "$['special[key]']")
ExtractFromJson(json, "$.nested['complex[name].with.dot']")
```

## Return Value Handling

1. **Null or Empty Inputs**

   - If either JSON or path is null, return null, if empty then return empty array / string

2. **Non-existent Paths**

   - Returns empty array/string

3. **Value Types**

   - String values: Returned as-is
   - Numbers: Converted to string representation
   - Boolean: Converted to lowercase string ("true"/"false")
   - Null: Skipped in the output
   - Objects: Converted to compact JSON string
   - Arrays: Converted to compact JSON string when directly accessed

## Error Handling

1. **Invalid JSON**

   - Returns empty array/string

2. **Invalid Path Syntax**

   - Returns empty array/string

3. **Array Index Out of Bounds**

   - Returns empty array/string

## Best Practices

1. Use bracket notation with quotes for properties containing special characters
2. Use dot notation for simple property names
3. Use `ExtractFromJsonToArray` when you need to process individual values
4. Use `ExtractFromJson` when you need a comma-separated string of values

## Limitations

1. Path must be valid according to the supported syntax
2. Array wildcards (`[*]`) cannot be used with object properties
3. No support for advanced JSON path features like filters or recursive descent
4. All numeric values are converted to their string representation