### About:

Implementation of sorting of large text files (>1gb)

Also you can create test randomly generated file according to pattern **Number. String** to test implementation.


#### How to build:

* Ensure that .Net fFramework 4.6.2 installed
* Build with Visual Studio 2015/2017

or

* Download MSBuild 2015 <https://www.microsoft.com/en-us/download/details.aspx?id=48159> and build with command:

``` Batchfile
msbuild LargeTextFilesSorting.sln
```


#### Input file:

* We have text file with with set of lines every of each has mandatory format **Number. String**
* File can contain duplicates of lines
* Lines in output file should be sorted by criteria: first by **String** part of line and then by **Number** part of line
* Example:
```
415. Apple
30432. Something something something
1. Apple
32. Cherry is the best
2. Banana is yellow
```


#### Output result file:

* We will get a output file with sorted lines 
    * Sorting criteria - first sort by **String** part of line and then sort by **Number** part of line
* Example:
```
1. Apple
415. Apple
2. Banana is yellow
32. Cherry is the best
30432. Something something something
```


#### Prerequisites for normal work:

* At least **FileSize * 3** free space at hard drive (to save temporary chunks and output file)
* At least 1-2Gb of free memory to create buffers during a work (depends on max count of lines of buffer and chunk)


#### How it work:


* At first stage (Step 1) initial file will iterated, splitted to 2 chunks (string part and number part), and sorted
* At second stage all sorted chunks will be merged info output file
* Application works in STA mode (performance can be improved with adding Parallel operations and some improvements)
* Application uses standard String.Ordinal comparision for both parts (string and number)


#### How to run 

* Run app without arguments to get help 
* Generation (default 100Mb file), sorting and checking can be executed with command: 
``` shell
dotnet run --configuration Release --generate --run --check
```
* To run basic performance tests for memory and file system: 
``` shell
dotnet run --configuration Release --perftest
```
* To specify detailed arguments for generation or other flags use format: --generate=Gb1 or --generate=Gb10, etc