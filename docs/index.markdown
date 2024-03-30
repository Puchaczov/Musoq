---

layout: home
title: ""
---
## Welcome to the kingdom of Musoq!

Musoq stands as a versatile engine, enabling the application of SQL syntax across a myriad of data sources. It facilitates querying across diverse formats such as files, directories, structured files, and archives. Owing to its modular architecture, Musoq treats each data source as a plugin, rendering virtually anything a potential data source. Although this framework demands intricate implementation, it uniquely empowers users to devise their own data sources, whether for broad use or specific applications. This adaptability to interface with various data types and the flexibility in their processing constitute Musoq's distinctive appeal.

## Unique Features of SQL Syntax in Musoq

Musoq enriches traditional SQL with innovative extensions and alterations, offering features such as:

- Integrated Queries to Custom Data Sources: It supports executing SQL queries on unconventional sources, including file systems and pictures (via EXIF attributes), and even archived files.
- Integration with AI and External APIs: Musoq facilitates connections with AI tools like GPT-3/GPT-4 and other APIs, paving the way for sophisticated data analysis and processing.
- Custom Query Definition Flexibility: Users can craft custom data sources equipped with specialized functions and aggregations, catering to their precise requirements.
- Combining Data Sources in One Query: A standout capability of Musoq is its ability to amalgamate data from varied sources into a singular query.

## Available data sources

The arsenal of data sources Musoq can query is as versatile as a Swiss Army knife, mirroring the versatility of the problems that may arise during data processing tasks

- Airtable: Enables querying Airtable tables.
- Archives: Allows archives to be treated as tables.
- CANBus: Facilitates treating CAN .dbc files and corresponding .csv files, which contain records of a CAN bus, as tables.
- Docker (Experimental): Permits treating Docker containers, images, etc., as tables.
- FlatFile: Enables treating flat files as tables.
- JSON: Allows JSON files to be treated as tables.
- Kubernetes (Experimental): Facilitates treating Kubernetes pods, services, etc., as tables.
- OpenAI: Designed to enhance other plugins with fuzzy search capabilities via GPT models.
- SeparatedValues: Enables treating files with separated values as tables.
- System: Includes utilities, ranges, and the dual table.
- Time: Allows treating time as a table.