# Coupling Syntax

## Overview

Coupling syntax in Musoq provides a powerful way to bind custom table definitions to data sources, creating reusable, type-safe interfaces for data processing. This feature allows you to create strongly-typed abstractions over various data sources.

## Basic Coupling Syntax

### Standard Coupling Pattern
```sql
couple #schema.method with table TableName as AliasName;
```

**Components:**
- `couple` - Keyword to initiate coupling
- `#schema.method` - The data source schema and method
- `with table` - Connecting phrase
- `TableName` - Previously defined table structure
- `as AliasName` - Alias for the coupled source

### Complete Example
```sql
-- 1. Define table structure
table PersonTable {
    Name 'System.String',
    Age 'System.Int32',
    Email 'System.String'
};

-- 2. Couple with data source
couple #csv.reader with table PersonTable as PersonSource;

-- 3. Use in queries
select Name, Age from PersonSource('/data/people.csv', true, 0);
```

## Coupling with Different Data Sources

### CSV Files
```sql
table EmployeeTable {
    EmployeeId 'System.Int32',
    FirstName 'System.String',
    LastName 'System.String',
    Department 'System.String',
    Salary 'System.Decimal'
};

couple #separatedvalues.csv with table EmployeeTable as EmployeeData;

select 
    FirstName + ' ' + LastName as FullName,
    Department,
    Salary
from EmployeeData('/hr/employees.csv', true, 0)
where Salary > 50000;
```

### JSON Data
```sql
table ProductTable {
    ProductId 'System.String',
    Name 'System.String',
    Price 'System.Decimal',
    Category 'System.String',
    InStock 'System.Boolean'
};

couple #json.objects with table ProductTable as ProductCatalog;

select 
    Category,
    count(*) as ProductCount,
    avg(Price) as AvgPrice
from ProductCatalog('/data/products.json')
group by Category;
```

### Custom Data Sources
```sql
table ApiResponseTable {
    Id 'System.String',
    Status 'System.String',
    Timestamp 'System.DateTime',
    Data 'System.String'
};

couple #api.endpoint with table ApiResponseTable as ApiData;

select Status, count(*) as StatusCount
from ApiData(@endpointUrl, @apiKey)
group by Status;
```

## Advanced Coupling Patterns

### Multiple Couplings for Different Sources
```sql
-- Define common table structure
table CommonDataTable {
    Id 'System.String',
    Value 'System.String',
    Category 'System.String'
};

-- Couple with multiple sources
couple #csv.reader with table CommonDataTable as CsvSource;
couple #json.reader with table CommonDataTable as JsonSource;
couple #xml.reader with table CommonDataTable as XmlSource;

-- Use in union operations
select 'CSV' as Source, * from CsvSource('/data.csv', true, 0)
union all
select 'JSON' as Source, * from JsonSource('/data.json')
union all  
select 'XML' as Source, * from XmlSource('/data.xml');
```

### Parameterized Coupling
```sql
table ConfigurableTable {
    Field1 'System.String',
    Field2 'System.Object',
    Field3 'System.Decimal?'
};

couple #dynamic.source with table ConfigurableTable as DynamicData;

-- Parameters passed at query time
select * 
from DynamicData(@sourcePath, @format, @encoding)
where Field3 is not null;
```

### Coupling with Archive Processing
```sql
table ArchiveContentTable {
    FileName 'System.String',
    Content 'System.String',
    Size 'System.Int64'
};

couple #archives.content with table ArchiveContentTable as ArchiveData;

-- Process files within archives
with ArchiveFiles as (
    select FileName, Content, Size
    from ArchiveData('/path/to/archive.zip')
    where FileName like '%.txt'
)
select 
    FileName,
    Length(Content) as ContentLength,
    Size
from ArchiveFiles
where Size > 1000;
```

## Type Safety and Schema Validation

### Automatic Type Conversion
```sql
table TypedTable {
    StringField 'System.String',
    IntField 'System.Int32',
    DecimalField 'System.Decimal',
    BoolField 'System.Boolean'
};

couple #flexible.source with table TypedTable as TypedData;

-- Musoq handles type conversion automatically
select 
    StringField,
    IntField * 2 as DoubledInt,
    DecimalField / 100 as Percentage,
    case when BoolField then 'Yes' else 'No' end as BoolText
from TypedData(@dataSource);
```

### Nullable Field Handling
```sql
table OptionalFieldsTable {
    RequiredId 'System.String',
    OptionalName 'System.String?',
    OptionalValue 'System.Decimal?'
};

couple #data.source with table OptionalFieldsTable as OptionalData;

select 
    RequiredId,
    coalesce(OptionalName, 'Unknown') as Name,
    coalesce(OptionalValue, 0.0) as Value
from OptionalData(@sourcePath)
where RequiredId is not null;
```

## Integration with Query Features

### Coupling with CTEs
```sql
table TransactionTable {
    TransactionId 'System.String',
    Amount 'System.Decimal',
    Date 'System.DateTime',
    Category 'System.String'
};

couple #financial.data with table TransactionTable as TransactionData;

with MonthlyTotals as (
    select 
        Year(Date) as Year,
        Month(Date) as Month,
        Category,
        sum(Amount) as MonthlyTotal
    from TransactionData(@transactionFile)
    group by Year(Date), Month(Date), Category
)
select 
    Year,
    Month,
    Category,
    MonthlyTotal,
    avg(MonthlyTotal) over (partition by Category) as AvgMonthlyForCategory
from MonthlyTotals
order by Year, Month, Category;
```

### Coupling with Cross Apply
```sql
table DocumentTable {
    DocumentId 'System.String',
    Content 'System.String',
    Author 'System.String'
};

couple #documents.reader with table DocumentTable as DocumentData;

select 
    d.DocumentId,
    d.Author,
    w.Word,
    count(*) as WordCount
from DocumentData(@documentsPath) d
cross apply Split(d.Content, ' ') w
where Length(Trim(w.Word)) > 3
group by d.DocumentId, d.Author, w.Word
having count(*) > 2
order by count(*) desc;
```

### Coupling with Joins
```sql
table UserTable {
    UserId 'System.String',
    Name 'System.String',
    Email 'System.String'
};

table OrderTable {
    OrderId 'System.String',
    UserId 'System.String',
    Amount 'System.Decimal',
    OrderDate 'System.DateTime'
};

couple #users.data with table UserTable as UserData;
couple #orders.data with table OrderTable as OrderData;

select 
    u.Name,
    u.Email,
    count(o.OrderId) as OrderCount,
    sum(o.Amount) as TotalSpent
from UserData(@usersFile) u
inner join OrderData(@ordersFile) o on u.UserId = o.UserId
group by u.UserId, u.Name, u.Email
having sum(o.Amount) > 1000
order by sum(o.Amount) desc;
```

## Best Practices

### 1. Descriptive Coupling Names
```sql
-- Good - descriptive and purpose-specific
couple #csv.reader with table EmployeeTable as EmployeeFromCsv;
couple #api.endpoint with table EmployeeTable as EmployeeFromApi;

-- Avoid - generic names
couple #csv.reader with table EmployeeTable as Source1;
couple #api.endpoint with table EmployeeTable as Data;
```

### 2. Consistent Table Definitions
```sql
-- Define common interfaces for similar data
table StandardPersonTable {
    Id 'System.String',
    FirstName 'System.String',
    LastName 'System.String',
    Email 'System.String'
};

-- Reuse across multiple sources
couple #csv.reader with table StandardPersonTable as CsvPersons;
couple #json.reader with table StandardPersonTable as JsonPersons;
couple #api.endpoint with table StandardPersonTable as ApiPersons;
```

### 3. Error-Resilient Coupling
```sql
table RobustTable {
    Id 'System.String',
    Data 'System.String?',           -- Optional field
    Timestamp 'System.DateTime?',    -- Optional timestamp
    IsValid 'System.Boolean'         -- Validation flag
};

couple #unreliable.source with table RobustTable as RobustData;

select * 
from RobustData(@source)
where IsValid = true
  and Data is not null;
```

## Error Handling and Troubleshooting

### Common Coupling Errors

1. **Table Not Defined**
```sql
-- ERROR: UndefinedTable not declared
couple #csv.reader with table UndefinedTable as Source;

-- FIX: Define table first
table UndefinedTable { Field 'System.String' };
couple #csv.reader with table UndefinedTable as Source;
```

2. **Schema Mismatch**
```sql
table StrictTable {
    ExactFieldName 'System.String'
};

-- May fail if CSV has different column names
couple #csv.reader with table StrictTable as StrictSource;

-- Consider flexible approach
table FlexibleTable {
    Field1 'System.String',
    Field2 'System.String',
    Field3 'System.String'
};
```

3. **Type Conversion Errors**
```sql
table NumericTable {
    NumericField 'System.Int32'
};

couple #csv.reader with table NumericTable as NumericSource;

-- Handle potential conversion failures
select 
    case 
        when IsNumeric(NumericField) then ToInt32(NumericField)
        else 0
    end as SafeNumericField
from NumericSource(@csvFile, true, 0);
```

### Debugging Strategies

1. **Test Table Definitions Separately**
```sql
-- Verify table definition syntax
table TestTable {
    Field1 'System.String',
    Field2 'System.Int32'
};
```

2. **Use DESC to Understand Source Schema**
```sql
desc #csv.reader('/test.csv', true, 0);
```

3. **Start with Simple Coupling**
```sql
-- Begin with single-field tables
table SimpleTable {
    Data 'System.String'
};

couple #csv.reader with table SimpleTable as SimpleSource;
```

## Performance Considerations

### Efficient Coupling Patterns
```sql
-- Pre-filter data in coupling when possible
table FilteredTable {
    RelevantField 'System.String',
    ImportantValue 'System.Decimal'
};

couple #large.dataset with table FilteredTable as FilteredData;

-- Apply filters early
select * 
from FilteredData(@largePath)
where ImportantValue > @threshold;  -- Filter applied efficiently
```

### Memory-Efficient Processing
```sql
-- Stream processing for large datasets
table StreamingTable {
    BatchId 'System.String',
    Data 'System.String'
};

couple #streaming.source with table StreamingTable as StreamingData;

-- Process in batches
select BatchId, count(*) as RecordCount
from StreamingData(@streamSource)
group by BatchId
having count(*) > 100;
```

## Next Steps

- Learn about [cross apply operations](./cross-outer-apply.md) for advanced data relationships
- Explore [Common Table Expressions](./common-table-expressions.md) for complex query structures
- See [practical examples](./examples-data-transformation.md) of coupling in real-world scenarios