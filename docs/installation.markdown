---
layout: page
title: Installation
permalink: /installation/
---

Musoq is available as a `CLI` (Command Line Interface) tool. This tool can be downloaded from the repository linked [here](https://github.com/Puchaczov/Musoq.CLI). It serves both as a server executing queries and a client that allows for running queries.

# Repository of Musoq CLI

The repository contains the Musoq command line interface. The program consists of two main parts:

- `Musoq agent`: The server part that allows loading the Musoq runtime and running your queries.
- `Musoq cli`: A thin client that enables you to run the queries on the server.

## How to try it out

1. Download the zipped program for your target architecture (check the release assets).
2. Unzip it to a directory of your choice.
3. Open a first console window in the directory.
4. For Windows, run `./Musoq.exe serve --wait-until-exit`. For Linux, run `./Musoq serve --wait-until-exit`.
5. Open a second console window in the same directory.
6. For Windows, run `./Musoq.exe run query "select 1 from #system.dual()"`. For Linux, run `./Musoq run query "select 1 from #system.dual()"`.

## Does it need any additional dependencies?

No. It is self-contained and should be ready to go immediately.

## Explore

You can explore CLI options with the `--help` helper command.

## Future

In the future, it's expected that the installation process will be fully automated, allowing for software installation through package managers like `snap` for Linux or `chocolatey` for Windows.