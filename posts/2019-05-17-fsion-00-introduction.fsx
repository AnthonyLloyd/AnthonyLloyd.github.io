(**
\---
layout: post
title: "Fsion - 0. Introduction"
tags: [f#,fsion,database,timeseries]
description: "Fsion - 0. Introduction"
keywords: F#, fsion, database, timeseries
\---

[Fsion](https://github.com/AnthonyLloyd/Fsion) is an EAVT (Entity, Attribute, Value, Time) time-series database for F#.

The main idea is by storing value updates as compressed data series, an embedded in memory
database can be created that is orders of magnitude smaller and faster than traditional
database solutions.
A functional in memory database can also be made more type-safe with a simpler API.
Other key ideas can be found [here](https://github.com/AnthonyLloyd/Fsion#key-ideas).

[Fsion](https://github.com/AnthonyLloyd/Fsion) has been designed with the finance domain in mind but could be equally applicable to
domains that requires a degree of time-series, historic query or audit functionality.

This series of blogs and the [repo](https://github.com/AnthonyLloyd/Fsion) have been submitted
for the [Applied F# Challenge](http://foundation.fsharp.org/applied_fsharp_challenge) in
the category of F# in your domain (finance).

The following sections outline the main [Fsion](https://github.com/AnthonyLloyd/Fsion) components.

## SetSlim and MapSlim

[SetSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs) / [SetSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs) / [MapSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs) / [MapSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)

`SetSlim<T>` and `MapSlim<K,V>` are low memory, high performance replacements for the mutable
collections `HashSet<T>` and `Dictionary<K,V>`.
A previous [post]({% post_url 2018-12-14-mapslim %}) covers their design and ~50% performance
improvement.

An important additional feature of these collections is that they do not need to be locked for
read while updates are being applied.
This combined with [IO]({% post_url 2019-03-29-io %}) will hopefully result
in a completely lock-free multi-threaded read model for [Fsion](https://github.com/AnthonyLloyd/Fsion).

## DataSeries

[DataSeries.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/DataSeries.fs) / [DataSeriesTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/DataSeriesTests.fs)

DataSeries represent an ordered table of Reporting Date, Transaction Id and `int64` encoded
Values with the latest values at the top. This forms a [bitemporal](https://www.networkworld.com/article/3186634/what-is-bitemporal-and-why-should-the-enterprise-care.html)
representation of value in the database.

The next post [Fsion - 1. Size]({% post_url 2019-05-17-fsion-01-size %}) demonstrates the
compression that can be achieved using this data structure. 

DateSeries also naturally support sets with add and remove operations.
This is much easier than managing foreign keys sets in traditional databases and will come in
handy when automatic indexes are added. 

## Transactor

[Transactor.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Transactor.fs) / [TransactorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/TransactorTests.fs)

The Transactor is responsible for making concurrent transactions consistent before persisting
and notifying subscribed Selectors.

Transactions are created from a Selector Store with any new Entity Ids and Transaction Id
following on from the Stores Ids.
The Transactor if necessary (due to concurrent transactions) corrects these.
Any corrected transactions have `transaction_based_on` set to the original Transaction Id.
Other processes would need to resolve data conflicts based on required business logic.

Transactions are themselves Entities in the database so any context data such as time, user, process
or source can be added.

## Selector

[Selector.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Selector.fs) / [SelectorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SelectorTests.fs)

Selector is responsible for applying Transactions to a Store, saving and loading a snapshot
and has an API for selecting data.

The Selector takes the Transaction Id in all API functions and can be performed at any past
Transaction Id.
The same results will be returned for the same parameters any time in the future.
This means Selection API calls are pure.
Consistency of data over a number of database or cache calls is often overlooked.

## View
_WIP_

Views are functions that can be passed to the Selector API to make selection easier and
more type-safe.

`Store -> Tx -> Entity -> Result<T,ValidationErrors>`

They can be thought of as both the schema definition (when run with an empty store) and
validation function.
Multiple views can be defined for the same Entity Type for different areas of the business
logic.

## Why F#

* Type-Safe - native typed values and marshalling unstructured data into type-safe views.
* Functional - pure, consistent and repeatable API calls.
* Simple - simple code will allow us to add more sophisticated functionality.
* Robust - in handling of errors and multi-threaded code.
* Testing - property based testing for serialization, stress testing for threading.  

## Conclusion

Traditional databases are not efficient as highly normalised stores with full history.
A compressed in memory store is smaller, faster and offers simpler functionality with no 'mapping'.

The design is flexible and can scale with the Transactor and Selector as separate processes.
The Selector Store can be based on [FASTER](https://github.com/Microsoft/FASTER) if the size of the
database when compressed is larger than memory.
Multiple Selector Stores can be run and filtered as a cache on the client.

The project continues to be work in progress.
Next steps are to make the API as type-safe as possible in terms of Attributes, Transactions
and Selection.
Work will also be done to make sure the API is resilient and performance optimised.
The roadmap can be found [here](https://github.com/AnthonyLloyd/Fsion/#roadmap).

Ultimately the aim is to provide a functional fully type-safe database and cache functionality
with a set of best practice meta data driven satellite projects.

Next, we will look at database size in [Fsion - 1. Size]({% post_url 2019-05-17-fsion-01-size %}).

*)