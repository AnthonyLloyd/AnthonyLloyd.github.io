(**
\---
layout: post
title: "Data-First Architecture - Asset Management"
tags: [functional,architecture,data]
description: "Data-First Architecture - Asset Management"
keywords: functional, architecture, data
exclude: true
\---

I recently had a light bulb moment when I saw a [tweet](https://twitter.com/etodd_/status/936587511580844032) from Evan Todd.
It helped bring together some ideas I've had for a while on software architecture.

> Data characteristics excluding software functionality should dictate the system architecture.

The shape, size and rate of change of the data is the most important factor when starting to architect a system.
The first thing that needs to be done is estimate these characteristics in average and extreme cases.

Functional programming encourages this mindset since the data and functions are kept separate.
Data is simple and pure functions are simple. Combining them leads to needless complexity.

I'm going to make the case with an example.
I will argue that most asset management systems store and use the wrong data.
This limits functionality and increases the complexity of these systems.

## Traditional Approach

Most asset management systems consider `positions`, `profit` and `returns` to be their main data.
You can see this as they normally have an overnight batch process that generates and saves `positions` for the next day.

This produces an enormous amount of duplicated data.
Databases are large and grow rapidly.
What is being saved is essentially a chosen set of calculation results.

What's worse is that other data processes are built on the top of this position data such as adjustments, lock-down and fund aggregation.

I think this architecture comes from not investigating the characteristics of the data first and jumping straight to thinking about system entities and functionality.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/10_servers.png" title="10 Servers"/>

## Data-First Approach

The primary data is `trades`, asset `terms` and `time series`.
`Positions`, `profit` and `returns` are just calculations based the primary data.
We can ignore these for now and consider caching of results at a later stage.

`Terms` data is complex in structure but relatively small in size and changes infrequently. Event sourcing works well here for audit and a changing schema.
`Time series` data is simple in structure and can be efficiently compressed down to 10-20% of its original size.
`Trades` data is a simple list of asset quantities from one entity to another.
The data is effectively all numeric and fixed size.
An append only ledger style structure works well here.

We can use the [iShares](https://www.ishares.com/uk/intermediaries/en/products/etf-product-list#!type=emeaIshares&tab=overview&view=list) fund range as a fairly extreme example.
Downloading these funds over a period of time and analysing the data gives us some useful statistics.

280 funds, 100-1000 positions per fund, 1000 trades per year per fund. Per day?
from 4, to 4, instrument 4, quantity 4, flow type 2, trade date 8, settle days 2, trade id 8, usertime 8 ~ 50 bytes
Data size for a trade is 50 bytes * 5 flows = 256 bytes.
Fund for 1 year 250 KB
Fund for 10 years 2.4 MB
280 funds for 10 years 700 MB

Now we have a feel for the data we can start to make some decisions about the architecture.

Given the size of data we can decide to load and cache by whole fund.
This will simplify the code and give us greater flexibilty on the various types of profit and return measures we can offer.
The majority of these calculations are ideally done as a single pass through the ordered trades.
It turns out with in memory data this is a negligable processing cost and can just be done on screen refresh.

We can also look at a hierarchy of funds and perform the calculations at a parent fund level.
Since most of the data is append only we can keep a cache saved (encryped of course) on the client to further save cloud costs.

## Conclusion

People are suprised when I say you can just hold this data in memory.
Infinitely scalable by default leads to bad perf + complexity.

In the days of cloud computing where architectural costs are more obvious right sizing the architecture to the data is more important.

Most articles titled architecture jump straight in to some feature of the codebase.

So we can build a system that is simpler, faster, more flexible and cheaper to run because we first understood the data.

## Todo

Because data cost many powers of 10 more time to retrieve. And also data shape is a constant in the system. Code changes.

We are not google, our extreme cases will be easier to estimate.

To wrap up: think about the actual problem and the data it needs. Then write functions to manage that data. Don't think about classes and interfaces and closures and reflection and RAII and exceptions and polymorphism and who knows what else.

## References

[The One Weird Trick: data first, not code first - Even Todd](http://etodd.io/2015/09/28/one-weird-trick-better-code/)  
[Data first, not code first - Hacker News](https://news.ycombinator.com/item?id=10291688)  
[Practical Examples in Data Oriented Design - Niklas Frykholm](http://gamedevs.org/uploads/practical-examples-in-data-oriented-design.pdf)  
[Queues and their lack of mechanical sympathy - Martin Fowler](https://martinfowler.com/articles/lmax.html#QueuesAndTheirLackOfMechanicalSympathy)  

*)