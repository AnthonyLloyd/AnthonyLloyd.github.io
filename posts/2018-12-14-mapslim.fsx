(**
\---
layout: post
title: "MapSlim - From DictionarySlim to Fsion"
tags: [mapslim,setslim,listslim,dictionaryslim,fsiom]
description: "MapSlim - From DictionarySlim to Fsion"
keywords: F#, mapslim, setslim, listslim, dictionyslim, fsion
exclude: true
\---
*)

(**

This post is part of the [F# Advent Calendar 2018](https://sergeytihon.com/2018/10/22/f-advent-calendar-in-english-2018/) series.
Many thanks to Sergey Tihon for organizing these.

Recently while working on [DictionarySlim](https://github.com/dotnet/corefxlab/blob/master/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs)
and [Fsion](https://github.com/AnthonyLloyd/Fsion) I realised it was possible to create useful lock free for read collections.

[DictionarySlim](https://github.com/dotnet/corefxlab/blob/master/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs)
is `Dictionary` replacement that can be up to 2.5 times faster for the common `TryGetValue` & `Add` pattern.
To do this it returns `ref` values and has a more efficient way of using hash codes. It came from ideas used in the
[Benchmarks Game](https://benchmarksgame-team.pages.debian.net/benchmarksgame/program/knucleotide-csharpcore-9.html).

[Fsion](https://github.com/AnthonyLloyd/Fsion) is a new [WIP](https://dictionary.cambridge.org/dictionary/english/wip) bi-temporal database for F#.
It stores a compressed set of historic values with audit for each entity-attribute.
The idea is that functional techniques can be used to translate unstructured data to a fully type safe representation.

## Fsion locking model

For the in memory version the value `DataSeries` are stored in a `Dictionary<EntityAttribute, DataSeries>` collection.
Each immutable `DataSeries` can be atomically replaced.
All queries are executed up to a given time/transaction id.
This simplifies the locking model as entries can be updated at the same time as a number of consistent reads are being made.

The problem is that a `ReaderWriterLock` would still need to be used on the Dictionary for the read and write side.
By creating lock free for read collections the database can be simplified to fully lock free for read access.

## MapSlim at al

Entry updates and collection resize can be made atomic.

[MapSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs)  
[SetSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs)  
[ListSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/ListSlim.fs)  

## Testing

Here's a recent example of the kind of thing you can do in less than 250 lines of code.
It includes unit tests, property tests, performance tests and threading stress tests.

![mapslim_perf](/{{site.baseurl}}public/mapslim/mapslim_perf.png)

[MapSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)  
[SetSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs)  
[ListSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/ListSlimTests.fs)  

![mapslim_stress](/{{site.baseurl}}public/mapslim/mapslim_stress.png)

## Conclusion

The Fsion database makes use of immutable (`DataSeries`), lock free read collections (`MapSlim`), and version concurrency model.


## References

[Data-First Architecture](http://anthonylloyd.github.io/blog/2018/02/01/architecture-data-first)  
[Datomic](https://www.datomic.com/)  
[Datomic: Event Sourcing without the hassle](https://vvvvalvalval.github.io/posts/2018-11-12-datomic-event-sourcing-without-the-hassle.html#disqus_thread)  
[FASTER](https://github.com/Microsoft/FASTER)

*)