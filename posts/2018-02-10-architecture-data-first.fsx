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

## Data-First Approach

1. The primary data is `trades`, asset `terms` and `time series`.
2. `Positions`, `profit` and `returns` are just calculations based the primary data. We can ignore these for now and consider caching of results at a later stage.
3. `Terms` data is complex in structure but relatively small in size and changes infrequently. Event sourcing works well here for audit and changing schema.
4. `Time series` data is simple in structure and can be efficiently compressed down to 10-20% of its original size.
5. `Trades` data is a simple list of asset quantity movements from one entity to another. The data is effectively all numeric and fixed size. An append only ledger style structure works well here.
6. We can use the [iShares](https://www.ishares.com/uk/intermediaries/en/products/etf-product-list#!type=emeaIshares&tab=overview&view=list) fund range as a fairly extreme example.
7. Downloading these funds over a period of time and analysing the data gives us some useful statistics.
8. 280 funds, 100-1000 positions per fund, 1000 trades per year per fund.
9. Data size for a trade is 50 bytes * 5 flows = 256 bytes

fund for 1 year 250 KB
fund for 10 years 2.4 MB
280 funds for 10 years 700 MB


from 4, to 4, instrument 4, quantity 4, flow type 2, trade date 8, settle days 2, trade id 8, usertime 8 ~ 50 bytes



Cache by fund simple. Ask any question simple code.

Hierarchy of funds to look at things from whole asset manager.

We can keep a cache of the data (encryped of course) on the client to further save cloud cost.

## Conclusion

Infinitely scalable by default leads to bad perf + complexity.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/10_servers.png" title="10 Servers"/>

In the days of cloud computing where architectural costs are more obvious right sizing the architecture to the data is more important.

Most articles titled architecture jump straight in to some feature of the codebase.

So we can build a system that is simpler, faster, more flexible and cheaper to run because we first understood the data.

## Todo

People are suprised when I say you can just hold this data in memory.

Premature micro-optimization is wasteful. Yet this waste is utterly insignificant compared to implementing algorithms with non-viable complexity.
In this specific case, you aren't gonna need YAGNI—complexity analysis matters, even in the very first version you write.

Because data cost many powers of 10 more time to retrieve. And also data shape is a constant in the system. Code changes.

We are not google, our extreme cases will be easier to estimate.

To wrap up: think about the actual problem and the data it needs. Then write functions to manage that data. Don't think about classes and interfaces and closures and reflection and RAII and exceptions and polymorphism and who knows what else.

Can't find any description of this apart from the gaming industry.
This philosophy is called data-oriented design, by the way. For those interested, here are some videos!

## References

[The One Weird Trick: data first, not code first - Even Todd](http://etodd.io/2015/09/28/one-weird-trick-better-code/)  
[Data first, not code first - Hacker News](https://news.ycombinator.com/item?id=10291688)  
[Practical Examples in Data Oriented Design - Niklas Frykholm](http://gamedevs.org/uploads/practical-examples-in-data-oriented-design.pdf)  
[Queues and their lack of mechanical sympathy - Martin Fowler](https://martinfowler.com/articles/lmax.html#QueuesAndTheirLackOfMechanicalSympathy)  

*)