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

1. Use link below to download latest version of `Musoq.Server`
	<iframe style="border:0px;width:100%;height:55px;" src="https://soupinf.net/preview/releases/a7fc86ba-3b5c-48d7-b0df-6657020e9028"></iframe>
2. Unpack downloaded `Musoq.Server` zip file to destination server directory.
3. Type command `dotnet Musoq.Server`. Server will start at port **7823**. To change default port, open `Settings.sf` and amend line `$env:ASPNETCORE_URLS=https://*:7823` to different port. 

### Downloading and running Musoq.Sever.Console
1. Use link below to download latest version of `Musoq.Server.Console`
	<iframe style="border:0px;width:100%;height:55px;" src="https://soupinf.net/preview/releases/b3080332-19a8-433e-ae9f-3562e0db5fdc"></iframe>
2. Unpack downloaded `Musoq.Server.Console` zip file to destianation server directory.
3. Go to destination client directory and open `cmd` or `powershell` at that directory.
4. Type command `dotnet Musoq.Server.Console --open help` once the server is still runing. Your default browser should open new page on help page.

> If you wonders how does the `Musoq.Server.Console` knows how to connect to `Musoq.Server`, it works that way: when `Musoq.Server` starts, it creates configuration file in well known localization for both programs. `Musoq.Server.Console` just looks into that localization and get's the configuration to connect to.

### Licenses

1. **Musoq.Server** is propietary software.
2. **Musoq.Server.Console** is propietary software.
3. **Musoq** evaluation engine is `MIT`.

### First try

After the installation, you have an `Musoq.Server` application that does not contains any plugin so that it cannot do any queries as all Musoq abilities are provided through plugins system. To get the plugin, we will use again `soupinf.net`
as it is a storage place for plugins and their updates too. To install new plugin you have to open console within the directory of `Musoq.Server.Console` and type the query 
`dotnet Musoq.Server.Console.dll --plugins --add {plugin_id}`. Installing new plugin requires `Musoq.Server` to reboot so install all plugins you would like to work with and then, restart `Musoq.Server` itself by pressing `Ctrl+C` and run it again. After that, your plugin should be ready to work. Enough to say that, to remove or update plugin later you have to just replace `--add` with `--update` or `--remove` command part.

Here is the table of plugins with their identifiers to replace with `{plugin_id}`:

|Name|Identifier|
|----|----------|
|Musoq.Text|c817e81d-65ee-4ca4-9fbf-186b21970bc6|
|Musoq.System |9048a2b6-40aa-4741-b98b-1c373a3fc8ee|
|Musoq.Time |c2eeae21-e104-43d9-b76c-76927b30f8fc|
|Musoq.Xml |129ed4da-6793-41d0-9135-77b41503b792|
|Musoq.SeparateValues |ff226225-5996-40ec-a0fd-0d9e2162cb75|
|Musoq.FlatFile |f7f45209-1756-41fe-8dbb-9f660d98c133|
|Musoq.Os |09f85f5a-cddc-410d-9483-b0061c3195eb|
|Musoq.Media |f68edcac-0daf-4912-a876-be137d8dd525|
|Musoq.Ocr|01d063ec-b32f-4807-8c98-f033ec6fac46|

### Usage

So, we have two programs, `Musoq.Server` which generally you won't do anything with besides running it. It just must be turned on as it is server. All operations you will be doing are on `Musoq.Server.Console` side. Let's see all options client provides:

```
--version
--query [
	--command {query} | 
	--file {filePath}]
	--culture "en-EN"
--wait
	--save {fileName.csv}
	--output
	--summary
--command [
	--math "1 + 2"
	--findfile --fields "Put,Here,Fields" --folder "E:/Put/Here/Directory" --where "Put-Here-Where-Constraints"
	]
--plugins
	--add {packageId}
	--update {packageId}
	--remove {packageId}
--open ["help" | "constructors" | "methods"]
```

`--version` - prints current version.

`--query --command "select 1 from #system.dual()"` - runs the query specified in `--command` parameter. This command do not wait for the query to be finished. It just instructs execution engine to start processing the query, print it's registration id and quit. You can omit `--command` in this case.

`--query --file "Path/To/File/With/Query.txt"` - runs the query specified in file. This command also does not waits for the query to be completed.

`--query --command "select 1 from #system.dual()" --wait` - runs the query and waits until it's execution is completed.

`--query "select 1 from #system.dual()" --wait --save some_file.csv`. - runs the query, wait until execution has been completed and save returned table to `some_file.csv`. In case, your file extension won't be `.csv` or `.tsv`, `save` command will save only first cell from returned table.

`--plugins --add {packageId}` - downoad and install plugin identified by an id.

`--plugins --update {packageId}` - update plugin identified by an id.

`--plugins --remove {packageId}` - remove plugin identified by an id.

`--open {method}` - opens default browser in a pointed help page.