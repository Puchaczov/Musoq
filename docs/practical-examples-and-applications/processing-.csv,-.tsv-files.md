---
title: Processing .CSV, .TSV Files
layout: default
parent: Practical Examples and Applications
nav_order: 3
---

# Processing Structured Files

`.csv`, `.tsv` are simple text files in which columns are separated by special separators (e.g., comma or tab), and the end of a row is marked by an end-of-line character. Such files may, but don't have to, have a header describing what each column is. The header is always the first row in the file. Below is a sample table generated that we will use to perform transformations and calculations for our queries.

| Category | Product | Quantity | UnitPrice |
| ---- | ---- | ---- | ---- |
| Home Appliances | Vacuum Cleaner | 82 | 163.77 |
| Books | Science | 5 | 47.56 |
| Computer Accessories | Webcam | 1 | 113.5 |
| Fashion | T-shirt | 74 | 181.97 |
| Fashion | Jeans | 8 | 159.87 |
| Fashion | Hat | 53 | 136.61 |
| Home Appliances | Refrigerator | 22 | 130.62 |
| Computer Accessories | Speaker | 98 | 103.57 |
| Home Appliances | Refrigerator | 17 | 121.75 |
| Books | Biography | 23 | 10.07 |
| Computer Accessories | Speaker | 6 | 109.77 |
| Fashion | Jeans | 21 | 195.95 |
| Fashion | Sneakers | 69 | 34.7 |
| Electronics | Smartwatch | 86 | 151.0 |
| Electronics | Laptop | 7 | 189.76 |
| Electronics | Tablet | 20 | 115.77 |
| Computer Accessories | Mouse | 44 | 178.51 |
| Home Appliances | Blender | 62 | 85.69 |
| Home Appliances | Vacuum Cleaner | 48 | 14.64 |
| Electronics | Headphones | 49 | 185.01 |
| Electronics | Laptop | 15 | 101.91 |
| Fashion | Jeans | 90 | 67.94 |
| Computer Accessories | Keyboard | 74 | 12.86 |
| Fashion | Hat | 34 | 120.21 |
| Books | History | 1 | 98.95 |
| Computer Accessories | Speaker | 32 | 135.16 |
| Electronics | Tablet | 32 | 86.54 |
| Electronics | Laptop | 42 | 54.91 |
| Electronics | Headphones | 12 | 192.94 |
| Electronics | Smartwatch | 40 | 83.4 |
| Home Appliances | Blender | 65 | 50.17 |
| Fashion | Hat | 28 | 82.16 |
| Home Appliances | Washing Machine | 50 | 77.21 |
| Electronics | Smartphone | 47 | 185.91 |
| Books | Biography | 42 | 24.57 |
| Fashion | Jeans | 13 | 73.85 |
| Computer Accessories | Speaker | 34 | 69.99 |
| Electronics | Laptop | 66 | 198.1 |
| Books | Science | 18 | 188.15 |
| Fashion | Sneakers | 60 | 131.19 |
| Books | Mystery | 81 | 73.11 |
| Fashion | Jeans | 51 | 30.24 |
| Home Appliances | Refrigerator | 14 | 161.9 |
| Computer Accessories | Keyboard | 50 | 121.49 |
| Fashion | Jacket | 54 | 146.29 |
| Computer Accessories | Webcam | 12 | 133.3 |
| Computer Accessories | Monitor | 30 | 5.32 |
| Home Appliances | Blender | 43 | 157.01 |
| Books | Mystery | 59 | 58.53 |
| Computer Accessories | Speaker | 8 | 156.03 |
| Computer Accessories | Mouse | 17 | 132.75 |
| Fashion | Jeans | 33 | 43.96 |
| Electronics | Laptop | 43 | 132.83 |
| Computer Accessories | Webcam | 96 | 68.64 |
| Electronics | Laptop | 30 | 125.86 |
| Fashion | T-shirt | 67 | 198.49 |
| Books | Science | 9 | 135.57 |
| Books | Science | 31 | 8.89 |
| Fashion | T-shirt | 89 | 197.07 |
| Electronics | Smartwatch | 17 | 107.64 |
| Computer Accessories | Monitor | 9 | 171.51 |
| Fashion | Sneakers | 79 | 168.12 |
| Fashion | T-shirt | 65 | 78.55 |
| Fashion | Hat | 22 | 73.59 |
| Computer Accessories | Webcam | 31 | 164.67 |
| Fashion | Jeans | 83 | 20.63 |
| Books | History | 50 | 186.23 |
| Electronics | Camera | 18 | 183.47 |
| Computer Accessories | Speaker | 44 | 136.75 |
| Books | Novel | 23 | 183.86 |
| Home Appliances | Refrigerator | 26 | 155.14 |
| Books | Science | 87 | 120.53 |
| Electronics | Smartphone | 26 | 91.9 |
| Computer Accessories | Speaker | 12 | 27.0 |
| Fashion | Hat | 48 | 112.67 |
| Fashion | Hat | 78 | 23.1 |
| Books | History | 10 | 91.04 |
| Electronics | Laptop | 78 | 168.49 |
| Books | Mystery | 58 | 65.09 |
| Home Appliances | Microwave | 78 | 115.89 |
| Books | Biography | 1 | 155.56 |
| Books | History | 18 | 174.27 |
| Computer Accessories | Mouse | 23 | 130.46 |
| Home Appliances | Blender | 19 | 70.96 |
| Books | Novel | 92 | 118.97 |
| Home Appliances | Washing Machine | 17 | 147.19 |
| Books | History | 32 | 194.87 |
| Home Appliances | Vacuum Cleaner | 42 | 140.49 |
| Fashion | Jacket | 34 | 134.91 |
| Electronics | Smartphone | 82 | 167.1 |
| Home Appliances | Vacuum Cleaner | 96 | 191.72 |
| Fashion | Hat | 78 | 58.36 |
| Books | Mystery | 27 | 96.87 |
| Electronics | Camera | 5 | 158.05 |
| Books | Novel | 74 | 170.65 |
| Fashion | Jacket | 13 | 199.69 |
| Computer Accessories | Mouse | 66 | 93.49 |
| Home Appliances | Vacuum Cleaner | 88 | 155.43 |
| Home Appliances | Microwave | 10 | 71.59 |
| Computer Accessories | Mouse | 3 | 26.24 |

## Enriching the Table with Calculations

First, we will enrich our calculations by determining the total price `UnitPrice` * `Quantity`.

```sql
select 
	*, 
	ToInt32(Quantity) * ToDecimal(UnitPrice) as TotalPrice 
from @separatedvalues.csv('@qfs/category_product_data.csv', true, 0)
```

We begin by reading the file from the query space named `category_product_data.csv` which has a header `true` and we are supposed to start reading from row zero `0`. Because our data source reads all columns as string types, we have to convert them to numbers. With each row read, a transformation will be performed on the relevant columns followed by multiplication.

Table after our transformations:

| Category | Product | Quantity | UnitPrice | TotalPrice |
| ---- | ---- | ---- | ---- | ---- |
| Home Appliances | Vacuum Cleaner | 82 | 163.77 | 13429.14 |
| Books | Science | 5 | 47.56 | 237.8 |
| Computer Accessories | Webcam | 1 | 113.5 | 113.5 |
| .... | ... | ... | ... | ... |
| Computer Accessories | Mouse | 3 | 26.24 | 78.72 |

## When the File Has No Header

If our file doesn’t have a header or the names are very irregular, we can simply skip the header row, indicating that the file doesn't have a header. This will result in automatically assigned column names `Column1`, `Column2`, ... `ColumnN`.

```sql
select 
	*, 
	ToInt32(Column3) * ToDecimal(Column4) as Column5 
from @separatedvalues.csv('@qfs/category_product_data.csv', false, 1)
```

Table after our transformations:

| Column1 | Column2 | Column3 | Column4 | Column5 |
| ---- | ---- | ---- | ---- | ---- |
| Home Appliances | Vacuum Cleaner | 82 | 163.77 | 13429.14 |
| Books | Science | 5 | 47.56 | 237.8 |
| Computer Accessories | Webcam | 1 | 113.5 | 113.5 |
| .... | ... | ... | ... | ... |
| Computer Accessories | Mouse | 3 | 26.24 | 78.72 |

## Let's Now Calculate the Total Price for Products in a Given Category and the Total Price for a Product in a Given Category

To get the desired effect in one query, we will use grouping, aggregate methods in the parent group (`TotalPriceForCategory`), and the current group's aggregate method (`TotalPriceForProduct`).

```sql
select 
    Category, 
    Product, 
    Sum(ToInt32(Quantity) * ToDecimal(UnitPrice), 1) as TotalPriceForCategory, 
    Sum(ToInt32(Quantity) * ToDecimal(UnitPrice)) as TotalPriceForProduct 
from @separatedvalues.csv('@qfs/category_product_data.csv', true, 0) 
group by Category, Product
```

This query works by creating subgroups of categories, then creating further subgroups of products within each of these. The expression `Sum(ToInt32(Quantity) * ToDecimal(UnitPrice))` accesses the innermost subgroup (products), while `Sum(ToInt32(Quantity) * ToDecimal(UnitPrice), 1)` accesses the parent subgroup, the subgroup of the given category, and performs its calculations there. The results of this query are the aggregated calculations:

| Category            | Product         | TotalPriceForCategory | TotalPriceForProduct |
|---------------------|-----------------|-----------------------|----------------------|
| Home Appliances     | Vacuum Cleaner  | 96150.58              | 52115.4              |
| Books               | Science         | 80287                 | 15606.33             |
| ...                 | ...             | ...                   | ...                  |

These calculations can be verified using an auxiliary Python script:

```python
df['TotalPrice'] = df['Quantity'] * df['UnitPrice']

total_price_per_category = df.groupby('Category')['TotalPrice'].sum().reset_index()

total_price_per_product_in_category = df.groupby(['Category', 'Product'])['TotalPrice'].sum().reset_index()

total_price_per_category, total_price_per_product_in_category
```

## Sentiment Analysis of Posts Using GPT

Suppose we received the following data:

| PostId | Comment                                             | Date       |
|--------|-----------------------------------------------------|------------|
| 1      | This product is amazing! Highly recommend.          | 2023-01-01 |
| 2      | Absolutely terrible service, I'm very disappointed. | 2023-01-02 |
| 3      | The product is okay, nothing special.               | 2023-01-03 |
| ...    | ...                                                 | ...        |
| 6      | It's a decent product, but I had higher expectations.| 2023-01-06 |

We need to determine the sentiment with which the posts were written, and to this end, we can use a data source such as the `GPT` model from `OpenAI`. The query in a simple way will return whether a given post has a positive, negative, or neutral sentiment.

```sql
select 
    csv.PostId,
    csv.Comment,
    gpt.Sentiment(csv.Comment) as Sentiment,
    csv.Date
from @separatedvalues.csv('@qfs/comments_sample.csv', true, 0) csv inner join @openai.gpt('gpt-4-1106-preview') gpt on 1 = 1
```

In this query, we use an `inner join` because we want to use a method that belongs to the calculated **gpt** table. This table always returns a single row; we are not specifically interested in the row value but want it to be available for each message. When initializing, we use the exact model name which is supposed to respond to our sentiment query. The data source `@openai.gpt` has more interesting methods with which I encourage you to become familiar in the source documentation.