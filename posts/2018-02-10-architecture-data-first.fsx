(**
\---
layout: post
title: "Data-First Architecture - Asset Management"
tags: [functional,architecture,data]
description: "Data-First Architecture - Asset Management"
keywords: functional, architecture, data
exclude: true
\---

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/to_sum_up.png" title="To sum up"/>

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

The primary data for asset management is asset `terms`, price `time series` and `trades`.
All other position data are just calculations based these.
We can ignore these for now and consider caching of calculations at a later stage.

- `terms` data is complex in structure but relatively small in size and changes infrequently. Event sourcing works well here for audit and a changing schema.
- `time series` data is simple in structure and can be efficiently compressed down to 10-20% of its original size.
- `trades` data is a simple list of asset quantities from one entity to another. The data is effectively all numeric and fixed size. An append only ledger style structure works well here.

We can use the [iShares](https://www.ishares.com/uk/intermediaries/en/products/etf-product-list#!type=emeaIshares&tab=overview&view=list) fund range as a fairly extreme example.
Downloading these funds over a period of time and analysing the data gives us some useful statistics.

280 funds, 100-1000 positions per fund, 1000 trades per year per fund. Per day?
from 4, to 4, instrument 4, quantity 4, flow type 2, trade date 8, settle days 2, trade id 8, usertime 8 ~ 50 bytes
Data size for a trade is 50 bytes * 5 flows = 256 bytes.
Fund for 1 year 250 KB
Fund for 10 years 2.4 MB
280 funds for 10 years 700 MB

Now we have a feel for the data we can start to make some decisions about the architecture.

Given the size we can decide to load and cache by whole fund history.
This will simplify the code and give us greater flexibilty on the various types of profit and return measures we can offer.
The majority of these calculations are ideally done as a single pass through the ordered trades.
It turns out with in memory data this is a negligable processing cost and can just be done on screen refresh.

We can also look at a hierarchy of funds and perform calculations at a parent level.
Since most of the data is append only we can just download latest additions to the client and save cloud costs.
As the data is bitemporal we can look at any previous time and easily ask questions such as what was responsible for the change in a result.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/10_servers.png" title="10 Servers"/>

## Conclusion

People are often suprised when I say full fund history can be just held in memory.

We are not google. Our extreme cases will be easier to estimate.
Infinitely scalable by default leads to complexity and bad performance.

In the current time of cloud computing where architectural costs are more obvious, right sizing the architecture is all the more important.

Most articles titled architecture jump straight in to some feature of the codebase.

So we can build a system that is simpler, faster, more flexible and cheaper to run because we first understood the data.

## References

[The One Weird Trick: data first, not code first - Even Todd](http://etodd.io/2015/09/28/one-weird-trick-better-code/)  
[Data first, not code first - Hacker News](https://news.ycombinator.com/item?id=10291688)  
[Practical Examples in Data Oriented Design - Niklas Frykholm](http://gamedevs.org/uploads/practical-examples-in-data-oriented-design.pdf)  
[Queues and their lack of mechanical sympathy - Martin Fowler](https://martinfowler.com/articles/lmax.html#QueuesAndTheirLackOfMechanicalSympathy)  

*)