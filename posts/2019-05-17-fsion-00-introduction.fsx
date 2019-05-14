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

[SetSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs)
[SetSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs)
[MapSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs)
[MapSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)

`SetSlim<T>` and `MapSlim<K,V>` are low memory, high performance replacements for
the mutable collections `HashSet<T>` and `Dictionary<K,V>`.
A previous [post]({% post_url 2018-12-14-mapslim %}) covers their design and
~50% performance improvement.

An important additional feature of these collections is that they do not need to be locked for
read while updates are being applied.
This combined with [IO]({% post_url 2019-03-29-io %}) will hopefully result
in a completely lock-free multi-theaded read model for Fsion.

## DataSeries

[DataSeries.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/DataSeries.fs)
[DataSeriesTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/DataSeriesTests.fs)

DataSeries represent an ordered table of Date, Transaction Id and `int64` Encoded Values
with the latest values at the the top.
The next [Fsion: 1. Size]({% post_url 2019-05-17-fsion-01-size %}) post demonstrates the data
compression that can be achieved using this data structure. 

DateSeries also naturally support sets with add and remove operations.
This will come in handy when automatic indexes are added. 

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

## Views

`Selector.Store -> Tx -> Entity -> Result<T,ValidationErrors>`


## Why F#

* Type-safety - typed value and views.
* Functional design - pure repeatable functions.  
* Testing - property based testing for serialization, stress testing for threading.  

## Conclusion

This project continues to be work in progress.
Next steps are to make the API as type-safe as possible in terms of Attributes, Transactions and Selections.
Work will also be done to make sure the API is resilient and optimised for performance.

The ultimate aim is provide functional fully type-safe database and cache functionality with a set of best
practice meta data driven satellite projects.


## Ideas

Traditional databases not efficient at normalised stores or audit queries.
Simple, embedded no query translations.

*)