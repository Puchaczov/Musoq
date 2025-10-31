# Table Definitions

## Overview

Table definitions in Musoq allow you to create custom table schemas that can be coupled with data sources. This feature enables type-safe data processing and provides a way to define structured interfaces for your data.

## Basic Table Definition Syntax

### Simple Table Definition
```sql
table TableName {
    ColumnName 'DataType'
};
```

### Multiple Column Table
```sql
table PersonTable {
    Name 'System.String',
    Age 'System.Int32',
    Email 'System.String'
};
```

## Supported Data Types

### Primitive Types
```sql
table DataTypesExample {
    StringColumn 'System.String',
    IntColumn 'System.Int32',
    LongColumn 'System.Int64',
    DecimalColumn 'System.Decimal',
    DoubleColumn 'System.Double',
    FloatColumn 'System.Single',
    BoolColumn 'System.Boolean',
    DateTimeColumn 'System.DateTime',
    DateTimeOffsetColumn 'System.DateTimeOffset',
    TimeSpanColumn 'System.TimeSpan',
    GuidColumn 'System.Guid'
};
```

### Nullable Types
```sql
table NullableTypesExample {
    RequiredName 'System.String',
    OptionalAge 'System.Int32?',
    OptionalEmail 'System.String?'
};
```

### Collection Types
```sql
table CollectionExample {
    Tags 'System.String[]',
    Numbers 'System.Int32[]',
    NestedData 'System.Object[]'
};
```

## Coupling Tables with Data Sources

### Basic Coupling Syntax
```sql
table UserTable {
    Username 'System.String',
    Email 'System.String'
};

couple #some.datasource with table UserTable as SourceOfUsers;

select Username, Email from SourceOfUsers();
```

### Complete Example with CSV Processing
```sql
-- Define table structure for CSV data
table EmployeeTable {
    Name 'System.String',
    Department 'System.String',
    Salary 'System.Decimal',
    HireDate 'System.DateTime'
};

-- Couple with CSV data source
couple #separatedvalues.csv with table EmployeeTable as EmployeeSource;

-- Query using the coupled table
select 
    Name,
    Department,
    Salary,
    HireDate
from EmployeeSource('/path/to/employees.csv', true, 0)
where Salary > 50000
order by HireDate desc;
```

## Advanced Table Definition Patterns

### Complex Data Processing Example
```sql
-- Define structure for JSON processing
table ProductTable {
    ProductId 'System.Int32',
    Name 'System.String',
    Price 'System.Decimal',
    Category 'System.String',
    InStock 'System.Boolean'
};

couple #json.objects with table ProductTable as ProductSource;

-- Query with aggregation
select 
    Category,
    count(*) as ProductCount,
    avg(Price) as AveragePrice,
    sum(case when InStock then 1 else 0 end) as InStockCount
from ProductSource('/data/products.json')
group by Category
having count(*) > 5
order by AveragePrice desc;
```

### Archive Processing with Table Definition
```sql
-- Define table for CSV files within archives
table PeopleDetails {
    Name 'System.String',
    Surname 'System.String',
    Age 'System.Int32'
};

couple #separatedvalues.comma with table PeopleDetails as SourceOfPeopleDetails;

-- Process CSV files from within ZIP archive
with Files as (
    select a.Key as InZipPath
    from #archives.file('./archive.zip') a
    where a.IsDirectory = false 
      and a.Contains(a.Key, '/') = false 
      and a.Key like '%.csv'
)
select 
    f.InZipPath,
    b.Name,
    b.Surname,
    b.Age
from #archives.file('./archive.zip') a
inner join Files f on f.InZipPath = a.Key
cross apply SourceOfPeopleDetails(a.GetStreamContent(), true, 0) as b;
```

## Dynamic Schema Coupling

### Runtime Table Definitions
```sql
-- Table definition can be created dynamically based on data discovery
table DynamicTable {
    Field1 'System.String',
    Field2 'System.Object',
    Field3 'System.Decimal?'
};

couple #dynamic.source with table DynamicTable as DynamicSource;

select * from DynamicSource(@runtimeParameter);
```

### Flexible Object Processing
```sql
-- Handle semi-structured data
table FlexibleTable {
    Id 'System.String',
    Data 'System.Object',
    Metadata 'System.String?'
};

couple #json.lines with table FlexibleTable as FlexibleSource;

select 
    Id,
    Data,
    Metadata
from FlexibleSource('/path/to/jsonlines.jsonl')
where Data is not null;
```

## Type Safety and Validation

### Automatic Type Conversion
```sql
-- Musoq performs automatic type conversion when possible
table NumericTable {
    StringNumber 'System.String',
    IntNumber 'System.Int32'
};

couple #csv.source with table NumericTable as NumericSource;

-- This query will attempt to convert string to int
select 
    StringNumber,
    IntNumber + 100 as AdjustedNumber
from NumericSource('/numbers.csv', true, 0)
where ToInt32(StringNumber) > 0;  -- Explicit conversion for validation
```

### Nullable Handling
```sql
table OptionalDataTable {
    RequiredField 'System.String',
    OptionalField 'System.String?',
    NumericField 'System.Int32?'
};

couple #data.source with table OptionalDataTable as OptionalSource;

select 
    RequiredField,
    coalesce(OptionalField, 'Default Value') as SafeOptionalField,
    coalesce(NumericField, 0) as SafeNumericField
from OptionalSource(@sourcePath)
where RequiredField is not null;
```

## Common Patterns and Best Practices

### 1. Consistent Naming Conventions
```sql
-- Use PascalCase for table and column names
table UserProfileTable {
    UserId 'System.Int32',
    FirstName 'System.String',
    LastName 'System.String',
    CreatedDate 'System.DateTime'
};
```

### 2. Appropriate Nullable Usage
```sql
-- Make optional fields nullable
table ContactInfoTable {
    Name 'System.String',           -- Required
    Email 'System.String?',         -- Optional
    Phone 'System.String?',         -- Optional
    Address 'System.String?'        -- Optional
};
```

### 3. Descriptive Coupling Names
```sql
couple #api.endpoint with table ContactInfoTable as ContactInfoSource;
couple #csv.reader with table ContactInfoTable as CsvContactSource;
couple #json.parser with table ContactInfoTable as JsonContactSource;
```

## Error Handling and Troubleshooting

### Common Table Definition Errors

1. **Invalid Type Names**
```sql
-- Wrong
table BadTable {
    Field 'string'  -- ERROR: Use 'System.String'
};

-- Correct
table GoodTable {
    Field 'System.String'
};
```

2. **Missing Type Quotes**
```sql
-- Wrong
table BadTable {
    Field System.String  -- ERROR: Type must be quoted
};

-- Correct
table GoodTable {
    Field 'System.String'
};
```

3. **Coupling Mismatches**
```sql
-- Table and data source must have compatible schemas
table MismatchTable {
    Field1 'System.String'
};

-- This may fail if CSV has different structure
couple #separatedvalues.csv with table MismatchTable as Source;
```

### Debugging Tips

1. **Test table definitions separately**
```sql
-- Test the table definition first
table TestTable {
    Name 'System.String'
};
```

2. **Use desc to understand data source schema**
```sql
desc #separatedvalues.csv('/test.csv', true, 0);
```

3. **Start with simple coupling**
```sql
-- Begin with basic single-column tables
table SimpleTable {
    Data 'System.String'
};
```

## Integration with Other Features

### With Common Table Expressions (CTEs)
```sql
table ProcessedTable {
    OriginalData 'System.String',
    ProcessedData 'System.String',
    ProcessingDate 'System.DateTime'
};

couple #processing.engine with table ProcessedTable as ProcessingSource;

with ProcessedData as (
    select 
        OriginalData,
        ProcessedData,
        ProcessingDate
    from ProcessingSource(@inputPath)
    where ProcessingDate > @cutoffDate
)
select * from ProcessedData
order by ProcessingDate desc;
```

### With Cross Apply Operations
```sql
table DocumentTable {
    FileName 'System.String',
    Content 'System.String'
};

couple #documents.reader with table DocumentTable as DocumentSource;

select 
    d.FileName,
    w.Word,
    count(*) as WordCount
from DocumentSource(@documentsPath) d
cross apply Split(d.Content, ' ') w
group by d.FileName, w.Word
having count(*) > 5;
```

## Next Steps

- Learn about [coupling syntax](./coupling-syntax.md) for advanced data source integration
- Explore [cross apply operations](./cross-outer-apply.md) for complex data relationships
- See [practical examples](./examples-data-transformation.md) of table definitions in action