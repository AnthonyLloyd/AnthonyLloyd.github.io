(**
\---
layout: post
title: "MapSlim - From DictionarySlim to Fsion"
tags: [mapslim,setslim,listslim,dictionaryslim,fsion]
description: "MapSlim - From DictionarySlim to Fsion"
keywords: F#, mapslim, setslim, listslim, dictionyslim, fsion
\---
*)

(**

PLEASE NOTE SOURCE CODE IS NO LONGER AVAILABLE AND THE POST IS HERE JUST FOR INFORMATION

This post is part of the [F# Advent Calendar 2018](https://sergeytihon.com/2018/10/22/f-advent-calendar-in-english-2018/) series.
Many thanks again to Sergey Tihon for organizing these.

Recently while working on [DictionarySlim](https://github.com/dotnet/corefxlab/blob/master/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs)
and [Fsion](https://github.com/AnthonyLloyd/Fsion) it became clear it is possible to create useful lock-free for read collections.

[DictionarySlim](https://github.com/dotnet/corefxlab/blob/master/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs)
is a `Dictionary` replacement that can be up to 2.5 times faster for the common `TryGetValue` & `Add` pattern.
To do this it returns `ref` values.
It is also more efficient with hash codes using `&` instead of the more costly `%`.
It came from ideas used in the [Benchmarks Game](https://benchmarksgame-team.pages.debian.net/benchmarksgame/program/knucleotide-csharpcore-9.html).

[Fsion](https://github.com/AnthonyLloyd/Fsion) is a [[WIP]](https://dictionary.cambridge.org/dictionary/english/wip) bi-temporal database for F#.
It stores a compressed set of historic values (with audit) for each entity-attribute.
The idea is that functional techniques can be used to translate unstructured data to fully type safe representations.
Compression and using attribute functions like Excel aims to keep database sizes minimal and possibly in memory.

## Fsion locking model

For the in memory version the value `DataSeries` are stored in a `Dictionary<EntityAttribute,DataSeries>` collection.
Each immutable `DataSeries` can be atomically replaced.
All queries are executed up to a given transaction id or time.
This simplifies the locking model as entries can be updated at the same time as a consistent set of reads are made.

The problem is that a `ReaderWriterLock` would still need to be used on the `Dictionary` for the read and write side.
By creating lock-free for read collections the database can be simplified to fully lock-free for read access.

## MapSlim, SetSlim and ListSlim

Three new collections [MapSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs),
[SetSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs)
and [ListSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/ListSlim.fs) have been created in F#.

The collections are lock-free for read for immutable reference types or types that can be updated atomically.
They are based on and show similar performance to `DictionarySlim`.
Internally care must be taken to add to and resize the collection atomically.
`List` would only require a small change for this to be true.

The limitation is the API for these collections is slimmed down and has no `Remove` or `Clear`.
One benefit that comes from this is that the collection entries can also be indexed into.

Calling the possibly updating `GetRef` method must be done using a lock while manipulating the `ref` value.

These collections have great potential for caches and functionality such as memoize.
The threading model becomes much simpler and eliminates the need to switch to concurrent collections.

## Testing

Performance and multithreading testing provides a good demonstration of [Expecto's](https://github.com/haf/expecto) abilities.

In under 250 lines of code the [MapSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)
have unit tests, property tests, performance tests and threading stress tests.

![mapslim_stress](/{{site.baseurl}}public/mapslim/mapslim_stress.png)

The other collections tests are [SetSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs) and
[ListSlimTests](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/ListSlimTests.fs)

![mapslim_perf](/{{site.baseurl}}public/mapslim/mapslim_perf.png)

The tests show large performance improvements over equivalent mutable collections.
In multithreading situations such as server caches this difference will be even greater.

## Conclusion

[Fsion](https://github.com/AnthonyLloyd/Fsion) makes use of immutable `DataSeries` and lock-free read collections like
[MapSlim](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs)
to create a lock-free row versioning style concurrency model.

This produces an interesting hybrid approach possible in a functional-first language.
It simplifies often complex and error prone locking required in server caches.

In future posts I hope to demonstrate how combining all of this can lead to a
simple yet high performance database and server.

Happy holidays!

## References

[Datomic](https://www.datomic.com/)  
[Datomic: Event Sourcing without the hassle](https://vvvvalvalval.github.io/posts/2018-11-12-datomic-event-sourcing-without-the-hassle.html)  
[Data-First Architecture](http://anthonylloyd.github.io/blog/2018/02/01/architecture-data-first)  
[FASTER](https://github.com/Microsoft/FASTER)

*)