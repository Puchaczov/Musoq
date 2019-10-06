---
layout: page
title: Installation
permalink: /installation/
---

**Musoq.Server** is a server that runs queries and **Musoq.Server.Client** is a thin client that 
participates through sending the queries and receiving results, printing them in console or saving to directory.
Both applications have auto-update feature and **Musoq.Server** will update automatically at startup once the update appear. You can set the application to do not update itself by modifying powershell script with additional argument `--ignoreUpdate`
**Musoq.Server.Console** has update on user demand policy setted up automatically so it won't try to update itself until you specify argument `--updateOnly`.

### Prerequisitives

1. `.Net Core 3.0` must be installed both on client and target machine.

### Downloading and running Musoq.Server

1. Download **Musoq.Server** from [soupinf.net](https://soupinf.net/published/a7fc86ba-3b5c-48d7-b0df-6657020e9028/latest). `Soupinf.net` is supporting service that provides on demand update infrastructure.
2. Unpack downloaded zip to destination server directory.
3. Run powershell script `.\run.ps1` that runs the server.

### Downloading and running Musoq.Sever.Console

1. Download **Musoq.Server.Console** from [soupinf.net](https://soupinf.net/published/b3080332-19a8-433e-ae9f-3562e0db5fdc/latest).
2. Unpack downloaded zip to destination client directory.
3. Go to destination client directory and open `cmd` or `powershell` at that directory.
4. Type command dotnet Musoq.Server.Console.dll --open help. If your default browser will new page, then everything works fine.

### Licenses

1. **Musoq.Server** is propietary software.
2. **Musoq.Server.Console** is propietary software.
3. **Musoq** evaluation engine is `MIT`.

### First try

After the installation, you have an `Musoq.Server` application that does not contains any plugin so that it cannot do any queries as all Musoq abilities are provided through plugins system. To get the plugin, we will use again `soupinf.net`
as it is a storage place for plugins and their updates too. To install new plugin you have to open console with `Musoq.Server.Console` and type a query 
`dotnet Musoq.Server.Console.dll --plugins --add {plugin_id}`. Installing new plugin requires `Musoq.Server` to reboot so install all plugins you would like to work with and then, restart `Musoq.Server` itself by pressing `Ctrl+C` and run it again. After that, your plugin should be ready to work. Enough to say that, to remove or update plugin later you have to just replace `--add` with `--update` or `--remove` command part.

Here is the table of plugins with their identifiers to replace with `{plugin_id}`:

|Name|Identifier|
|----|----------|
|Musoq.Text|c817e81d-65ee-4ca4-9fbf-186b21970bc6|
|Musoq.System |9048a2b6-40aa-4741-b98b-1c373a3fc8ee|
|Musoq.Time |c2eeae21-e104-43d9-b76c-76927b30f8fc|
|Musoq.Xml |129ed4da-6793-41d0-9135-77b41503b792|
|Musoq.Csv |c3dc9dbc-d08e-479c-8043-861e7af8d585|
|Musoq.FlatFile |f7f45209-1756-41fe-8dbb-9f660d98c133|
|Musoq.Os |09f85f5a-cddc-410d-9483-b0061c3195eb|
|Musoq.Media |f68edcac-0daf-4912-a876-be137d8dd525|
|Musoq.Ocr|01d063ec-b32f-4807-8c98-f033ec6fac46|
