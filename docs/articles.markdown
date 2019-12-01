---
layout: page
title: Articles
permalink: /articles/
---
### Analyzing space consumption on partition with Windows and SQL

Once upon a time in my computer… I faced a problem that appears to all of us from time to time. 
I was rumming in the system settings and I suddenly realized that my primary partition is almost full. 
There were only 30 gigabytes left and I really don’t know where all of my empty space disappeared as I haven’t installed anything big lately. 
To be honest, it’s not that it just disappeared. 
It was long term process that I was just ignoring for a long period of time. 
I’m aware of how Windows loves to consume all space left so I decided to analyze it and figure out what those files are and can I delete them?

This is what we want to achieve:

![image]({{site.baseurl}}/assets/images/executed_query.png)

Clear table with listed directories and the space they occupies (including sub directories!). 
As there is a tremendous amount of files in the file system we need something that does quick overview of where to look for lost space.

It's my personal preference to continuously go deeper into tree only for that directories that have high level of used space. 
This way, I can visit only those directories that have something big inside (or aggregated size of files is big) as I don’t want to waste of time to look over lowly occupied folders. 
Let’s look a query then:

```
select 
	::1 as RootDirectory, 
	Round(Sum(Length) / 1024 / 1024 / 1024, 1) 
from #os.files('C:/', true) 
group by RelativeSubPath(1)
```

Looks easy, huh? It is so simple because the query does only few things and you don't see all the underlying operations that evaluator does for you. <br/>
First of all, all files are visited on every nested directory of `C` with `C` included. 
We get the starting location from `#os.files('C:/', true)`. 
The literal value `true` is passed to instruct query evaluator to visit sub directories.
To be precise I have to say we don't have to be scared that all the files as it visits, will be loaded into memory. 
Query evaluator will just read the metadata of the file.<br/>
By going further, we would like to visit all of the files in partition and on every of that file apply grouping operator. 
In our example, it will work on the result of `RelativeSubPath(1)`.
If you asked me what this method does I would be obligated to say: it gives a relative sub path of the file that is processed. 
In fact, the whole method `string RelativeSubPath ([InjectSource] ExtendedFileInfo, int nesting)` is a shorthand for two different methods:

 - `string SubPath(string directory, int nesting)` 
 - `string GetRelativePath(ExtendedFileInfo fileInfo, string basePath)`
 
Result of that method is string that contains the relative path to one of the main directories we started to traverse from so in our case it will be `C:/Some`. 
Let me explain it on a simple example, if your path is:

```
C:\Some\Very\Long\Path
```

and you start from 

```
C:\
```

then your relative path will be `Some\Very\Long\Path`. 
If your path is the same but you starts from

```
C:\Some
```

then your relative path will be `Very\Long\Path`. 

Did you get it? There is still literal argument `1` passed to this method. 
With these argument, we can limit the depth of the relative path `Some\Very\Long\Path` by setting it some numeric. 
With argument value `1` we end up with `Some`, with `2` it’s gonna be `Some\Very`. 
Based on that, we are able to flattening the whole tree to small subset of main directories. 
We can match the file from nested directory with the top directory as if it would belong to him directly. 
Every single file will belong to a single group - group that describes one of main directories.

#### Conclusion

I hope it will be usefull you. 
In the future I will probably write about something more advanced using Musoq. 
Primarily I will describe my peripeteia with the tool as it is my swiss army knife I use with combination of other tools. 
If you enjoyed the reading and wants to ask something, please contact me through email or just make an issue within the github project.

