[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Puchaczov/Musoq/graphs/code-frequency)
[![Nuget](https://img.shields.io/badge/Nuget%3F-yes-green.svg)](https://www.nuget.org/packages?q=musoq)
[![Build & Tests](https://github.com/Puchaczov/Musoq/workflows/Unit%20Tests/badge.svg)](https://github.com/Puchaczov/Musoq/workflows/Unit%20Tests/badge.svg)

# What is Musoq?

Musoq is a powerful query engine that allows you to use SQL syntax on a wide variety of data sources.

![Musoq In Action](https://github.com/Puchaczov/Musoq/blob/master/musoq_anim_3.gif)

# Features
Musoq exposes raw data sets as queryable sources. This means you can search these data sources using a variant of SQL syntax. The potential sources are limitless, but here are some ideas that have already been implemented:

- Directories
- Files
- Structured files (.csv, .json, .xml, logs)
- Photos (by exif attributes)
- Archived files (.zip)
- Version Control Systems (Git, Svn, TFS)
- Websites (tables, lists)
- Processes
- Time
- AI - **GPT 3 / 4 (requires API-KEY)**
- Airtable **(requires API-KEY)**, SQLite, Postgres

Musoq also allows you to mix different data sources in your queries.

## How Can I Access These Data Sources?

Visit the **[Musoq.DataSources](https://github.com/Puchaczov/Musoq.DataSources)** repository where all plugins are stored.

## Sample Queries

Musoq allows you to write simple or complex queries on your data. Here are a couple of examples:

1. Selecting all executable or image files from a directory:
    ```sql
    select * from #os.files('path/to/folder', false) where Extension = '.exe' or Extension = '.png'
    ```
    Or through reordered syntax:
    ```sql
    from #os.files('path/to/folder', false) where Extension = '.exe' or Extension = '.png' select *
    ```
...

[Check out more example queries here](https://github.com/Puchaczov/Musoq/wiki/Example-Queries)

## Compatibility

Musoq is highly compatible across different platforms. It's been successfully tested on Docker, Linux, and Windows systems. Both ARM-64 and x86_64 architectures are supported, which makes it versatile for a wide range of hardware.

While it hasn't been officially tested on macOS yet, feedback from users attempting to run it on this platform would be much appreciated. If you try Musoq on macOS, or any other untested platforms, please post an issue reporting your experience.

## Building Your Own Plugins

You can easily create your own data source plugins. For detailed instructions, check the [Plugins section in the wiki](https://github.com/Puchaczov/Musoq/wiki/Plugins).

## Performance

Musoq provides robust performance for data processing tasks. [See the performance test results here](https://github.com/Puchaczov/Musoq/blob/master/musoq_sim_agg_pict.png)

## Motivation

The need for a single tool to perform complex queries on various data sources, including personal bank account files, was the main motivation behind Musoq. Although you could hand-write multiple scripts to achieve the same result, Musoq simplifies and accelerates this process, making data querying fast and convenient.

## Note

Musoq is not fully SQL compliant. While it uses SQL syntax and replicates some SQL behaviours, there might be differences and it also implements some experimental syntax and behaviours not used by database engines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
