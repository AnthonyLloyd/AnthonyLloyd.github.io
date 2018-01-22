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

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/10_servers.png" title="10 Servers"/>

## Traditional Approach

Most asset management systems consider `positions`, `profit` and `returns` to be their primary data.
You can see this as they normally have an overnight batch process that generates and saves `positions` for the next day.

This produces an enormous amount of duplicated data.
Databases are large and grow rapidly.
What is being saved is essentially a chosen set of calculation results.

What's worse is that other data processes are built on the top of this position data such as adjustments, lock down and fund aggregation.

This architecture comes from not investigating the characteristics of the data first and jumping straight to thinking about system entities and functionality.

## Data-First Approach

The primary data for asset management is asset `terms`, price `timeseries` and `trades`.
All other position data are just calculations based on these.
We can ignore these for now and consider caching of calculations at a later stage.

- `terms` data is complex in structure but relatively small and changes infrequently. Event sourcing works well here for audit and a changing schema.
- `timeseries` data is simple in structure and can be efficiently compressed down to 10-20% of its original size.
- `trades` data is a simple list of asset quantity flows from one entity to another. The data is effectively all numeric and fixed size. An append only ledger style structure works well here.

We can use the [iShares](https://www.ishares.com/uk/intermediaries/en/products/etf-product-list#!type=emeaIshares&tab=overview&view=list) fund range as a fairly extreme example.
Downloading these funds over a period and analysing the data gives us some useful statistics.

- 280 funds.
- 50-5000 positions per fund.
- Average 50 trade per day per fund.
- {From:ID<int> 4;To:ID<int> 4;Asset:ID<int> 4;Quantity:int 4} list * TradeID 8 * UserTime 8 => 16 * 5 + 16 = 96 (lets say 128 bytes)
- A fund for 1 year is 1.5 MB
- A fund for 10 years is 15 MB
- 280 funds for 10 years is 4 GB

Now we have a good feel for the data we can start to make some decisions about the architecture.

Given the size we can decide to load and cache by whole fund history.
This will simplify the code and give us greater flexibility on the various types of profit and return measures we can offer.
Most of these calculations are ideally performed as a single pass through the ordered trades.
It turns out with in memory data this is negligible processing cost and can just be done as the screen refreshes.

We can also look at a hierarchy of funds and perform calculations at a parent level.
Since the data is append only we can just download latest additions to the client and save cloud costs.
Also as the data is bitemporal we can easily look at any previous time and ask questions such as what was responsible for a change in a result.

## Conclusion

So, we can build a system that is simpler, faster, more flexible and cheaper to host because we first understood the data.

People are often surprised that full fund history can be held in memory.
Software developers cannot always answer questions on the size of their systems data. It's been abstracted away from them.

We are not google.
Infinitely scalable by default leads to complexity and bad performance.
Our extreme cases will be easier to estimate.

In the current time of cloud computing, where architectural costs are obvious, right sizing is more important.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/to_sum_up.png" title="To sum up"/>

## References

[The One Weird Trick: data first, not code first - Even Todd](http://etodd.io/2015/09/28/one-weird-trick-better-code/)  
[Data first, not code first - Hacker News](https://news.ycombinator.com/item?id=10291688)  
[Practical Examples in Data Oriented Design - Niklas Frykholm](http://gamedevs.org/uploads/practical-examples-in-data-oriented-design.pdf)  
[Queues and their lack of mechanical sympathy - Martin Fowler](https://martinfowler.com/articles/lmax.html#QueuesAndTheirLackOfMechanicalSympathy)  

*)