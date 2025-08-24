---
title: Setup & Usage
layout: home
nav_order: 7
---

# Setup & Use tool from CLI

## Setup

1. Download release for your platform at [Musoq CLI repository](https://github.com/Puchaczov/Musoq.CLI)
2. Extract the archive
3. Linux users: `chmod +x ./Musoq`

## Basic Usage

First, start the server to run queries:

```bash
Musoq serve
```

or quit the server if don't need it anymore:

```bash
Musoq quit
```

### Query

Once your server is up and running, run queries directly:

```bash
# Windows
Musoq.exe run query "select 1 from @system.dual()"

# Linux
Musoq run query "select 1 from @system.dual()"
```

### Help and Options

Check available options:

```bash
Musoq --help
Musoq run --help
Musoq serve --help
```

## More Advanced Things

### Server Operations

when you need to look at the server logs, run the server in the foreground:

```bash
# Start server and keep console open
Musoq serve --wait-until-exit
```

and then stop it with:

```bash
Musoq quit
```

### Specify Desired Output Format

```bash
# Specify output format
Musoq run query "select 1 from @system.dual()" --format [raw|csv|json|interpreted_json]
```

#### Raw

```bash
Musoq run query "select Value from @system.range(1, 3)" --format raw
```
Output:
```
Columns:
[{"name":"Value","type":"System.Int64","order":0}]
Rows:
[[{"value":1}],[{"value":2}],[{"value":3}]]
```

#### CSV
```bash
Musoq run query "select Value from @system.range(1, 3)" --format csv
```
Output:
```csv
Value
1
2
3
```

#### JSON
```bash
Musoq run query "select Value from @system.range(1, 3)" --format json
```
Output:
```json
[{"Value":1},{"Value":2},{"Value":3}]
```

#### Interpreted JSON
```bash
Musoq run query "select Value as 'obj.Number', NewId() as 'obj.Id' from @system.range(0, 10)" --format interpreted-json
```
Output:
```json
[{"obj":{"Number":0,"Id":"00666e1c-358b-424a-b1bd-2550bb3d3d1d"}},{"obj":{"Number":1,"Id":"fb391e2c-a5d6-479e-9008-a44adddb475a"}},...]
```

## Data Persistence with Buckets

Sometimes the process of data preparation for being queryable is really heavy. In such cases, it's possible that plugin might allow to use buckets to store and reuse data. Roslyn data source allows do that:

```bash
Musoq bucket create mybucket
```

Load solution to bucket

```bash
Musoq csharp solution load --solution "path/to/solution.sln" --bucket mybucket
```

Do the first query

```bash
Musoq run query "select p.Name from @csharp.solution('path/to/solution.sln') s cross apply s.Projects p" --bucket mybucket
```

Then do another

```bash
Musoq run query "select p.Name from @csharp.solution('path/to/solution.sln') s cross apply s.Projects p where p.Name like '%Tests'" --bucket mybucket
```

This way, you load the solution once and then reuse it in multiple queries. After you're done, you can clean up:

```bash
Musoq csharp solution unload --solution "path/to/solution.sln" --bucket mybucket
```

And delete whole bucket:

```bash
Musoq bucket delete mybucket
```

## Shell Integration

Musoq can process tables resulted from other commands. This allows for easy integration with other tools. You can treat table as a data source and process it with Musoq.

```powershell
wmic process get name,processid,workingsetsize | Musoq.exe run query "select t.Name, Count(t.Name) from @stdin.table(true) t group by t.Name having Count(t.Name) > 1"
```

Or join them with other data sources:

```powershell
& { 
    docker image ls; 
    .\Musoq.exe separator; 
    docker container ls 
} | ./Musoq.exe run query "select t.IMAGE_ID, t.REPOSITORY, t.SIZE, t.TAG, t2.CONTAINER_ID, t2.CREATED, t2.STATUS from @stdin.table(true) t inner join @stdin.table(true) t2 on t.IMAGE_ID = t2.IMAGE"
```

Or use AI models to extract data from text:

```text
Ticket #: 1234567
Date: 2024-09-07 14:30:22 UTC
Customer: Jane Doe (jane.doe@email.com)
Product: CloudSync Pro v3.5.2
OS: macOS 12.3.1

Subject: Sync Failure and Data Loss

Description:
Customer reported that CloudSync Pro failed to sync properly on 2024-09-06 around 18:45 local time. 
The sync process started but stopped at 43% completion with error code E-1010. 
After the failed sync, the customer noticed that approximately 250 MB of data was missing from their local drive.
The customer has tried restarting the application and their computer, but the issue persists.
They are using CloudSync Pro on 3 devices in total: MacBook Pro, iPhone 13, and iPad Air.

Steps to Reproduce:
1. Open CloudSync Pro v3.5.2 on macOS 12.3.1
2. Initiate a full sync
3. Observe sync progress halting at 43% with error E-1010

Impact: High - Customer cannot sync data and has lost important files

Troubleshooting Attempted:
- Restarted application: No effect
- Restarted computer: No effect
- Checked internet connection: Stable at 100 Mbps

Additional Notes:
Customer is a premium subscriber and requests urgent assistance due to lost data containing work-related documents.
```

```bash
Get-Content "C:\Tickets\ticket.txt" | ./Musoq.exe run query "select t.TicketNumber, t.TicketDate, t.CustomerName, t.CustomerEmail, t.Product, t.OperatingSystem, t.Subject, t.ImpactLevel, t.ErrorCode, t.DataLossAmount, t.DeviceCount, case when ToLowerInvariant(t.SubscriptionType) like '%premium%' then 'PREMIUM' else 'STANDARD' end from @stdin.text('Ollama', 'llama3.1') t"
```

Or use it to extract informations from receipt:

```bash
Musoq image encode "/some/image/receipt.jpg" | ./Musoq.exe run query "select s.Shop, s.ProductName, s.Price from @stdin.image('OpenAi', 'gpt-4o') s"
```