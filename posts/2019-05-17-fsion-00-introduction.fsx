(**
\---
layout: post
title: "Fsion: 0. Introduction"
tags: [f#,fsion,database,timeseries]
description: "Fsion: 0. Introduction"
keywords: F#, fsion, database, timeseries
exclude: true
\---

Fsion is an EAVT (Entity, Attribute, Value, Time) time-series database for F#.

Fsion has been designed for the finance domain but could be equally applicable to
domains that requires a degree of time-series, historic query or audit functionality. 

This series of blogs and [repo](https://github.com/AnthonyLloyd/Fsion) have been submitted
for the Applied F# Challenge in the category of F# in your domain (finance).

## SetSlim and MapSlim

[MapSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs)
[MapSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)  
[SetSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs)
[SetSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs)

`SetSlim<T>`, `MapSlim<K,V>` and `ListSlim<T>` are low memory, high performance replacements for
the mutable collections `HashSet<T>`, `Dictionary<K,V>` and `List<T>`.
A previous [post](http://{{site.baseurl}}blog/2018/12/14/mapslim) covers their design and ~50%
performance improvement.

An important additional feature of these collections is that they do not need to be locked for
read while updates are being applied.
This combined with [IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/IO.fs) will
hopefully result in a completely lock-free multi-theaded read model for Fsion.

## DataSeries

[DataSeries.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/DataSeries.fs)
[DataSeriesTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/DataSeriesTests.fs)

DataSeries set check order value is sensible.
DateSeries also naturally support sets with add and remove operations. These will come in handy when automatic indexes are added. 

## Transactor

[Transactor.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Transactor.fs)
[TransactorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/TransactorTests.fs)

Tra

## Selector

[Selector.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Selector.fs)
[SelectorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SelectorTests.fs)

Selection takes the Transaction Id which can only from the Selector.
Selections can be performed at any historic Transaction Id.
The same result will be returned any time in the future. This means Selections are pure.

## Why F#

Type-safety - 
Functional design - pure repeatable functions.
Testing - property based testing for serialization, stress testing for threading.


## Views

`Selector.Store -> Entity -> Result<T,ValidationErrors>`




## Conclusion

This project continues to be work in progress.
Next steps are to make the API as type-safe as possible in terms of Attributes, Transactions and Selections.
Work will also be done to make sure the API is resilient and optimised for performance.

The ultimate aim is provide functional fully type-safe database and cache functionality with a set of best
practice meta data driven satellite projects.

**)