---
layout: page
title: Articles
permalink: /articles/
---
### Analyzing space consumption on partition with Windows and SQL

Once upon a time in my computer... I faced a problem that appears to all of us from time to time. I was rumming in the system settings and I suddenly realized that my primary partition is almost full. 
There were only 30 gigabytes left and I really don't know where all of my empty space disappear as I haven't installed anything big lately. To be honest, it's not that it just disapear, it was a long term process that I just ignored for a long period of time. 
I'm aware of how Windows loves to consume all space left so I decided to analyse it and figure out what those files are and can I delete them? 

This is what we want to achieve:

![image]({{site.baseurl}}/assets/images/executed_query.png)

Clear table with listed directories and the space they occupies (including subdirectories!). 
As there is a tremendous amount of files in the file system we need something that does quick overview of where to look for losted space.

I prefer to continuously go deeper into tree only for that directories that have high level of used space. 
This way, I can visit only those directories that have something big inside (or aggregated size of files is big) as I don't want to waste of time to look over lowly occupied folders. Let's look a query then:

```
select 
	::1 as RootDirectory, 
	Round(Sum(Length) / 1024 / 1024 / 1024, 1) 
from #os.files('C:/', true) 
group by RelativeSubPath(1)
```

Looks easy, huh? And it is as the query does only few things. 

First of all, it traverses all files on every nested directory of `C` with `C` included. 
It get's where to start from `#os.files('C:/', true)`. `True` literal is value of the argument that instructs to visit subdirectories.

Don't be scared, it doesn't read all the files as it traverse, evaluator will just read the metadata of the file. 
Let me explain the query a little more deeper. To achieve our goal, we would like to visit all of the files in partition and on every of that file 
apply grouping operator. In our example, operator will work on the result of `RelativeSubPath(1)`. 

You could ask what this method does and I would be obligated to say: the whole method `string RelativeSubPath (int nesting)` is a shorthand for two different methods:

 - `string SubPath(string directory, int nesting)` 
 - `string GetRelativePath(ExtendedFileInfo fileInfo, string basePath)`
 
Combination of these two methods gives a relative sub path of the file that is processed. 
You would ask relative to what? Relative to the directory we started to traverse from so in our case it's `C:/`. Let me explain it on a simple example, if your path is:

```
C:\Some\Very\Long\Path
```

and you start from 

```
C:\
```

then your relative path will be `Some\Very\Long\Path`. If your path is the same but you starts from

```
C:\Some
```

then your relative path will be `Very\Long\Path`. 

Did you get it? there is still literal argument `1` passed to this method. 
With these argument, we can limit the depth of the relative path (`Some\Very\Long\Path`) by setting it some numeric. 
With `1`, we end up with `Some`, with `2` it's gonna be `Some\Very`. Based on that, we are able to flattening the whole tree to small subset of top directories.
We can match the file from nested directory with the top directory as if it would belong to him directly. Every single file will belong to a single group - group that describes one of top directories.

#### Conclusion

I hope it gives you some basic insights how to use the tool and it will be usefull you. In the future I will probably write about something more advanced using *Musoq*. 
Primarily I will describe my peripeteia with the tool as it is my swiss army knife I use with combination of other tools. If you enjoyed the reading and wants to ask something, please contact me through email or just make an issue within the github project.

