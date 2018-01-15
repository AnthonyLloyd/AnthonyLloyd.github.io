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

I'm going to make the case with an example from asset management.
I will argue that most systems store and use the wrong data.
This limits functionality and increases the complexity of these systems.

## Traditional Approach

Most asset management systems consider positions, profit and returns to be their main data.
You can see this as they normally have an overnight batch process that generates and saves positions for the next day.

This produces an enormous amount of duplicated data.
Databases are large and grow rapidly.
What is being saved is essentially a chosen set of calculation results.

Worse than this other data processes are built on the top of this position data such as adjustments, lock-down and fund aggregation.

## Data-First Approach

Premature micro-optimization is wasteful. Yet this waste is utterly insignificant compared to implementing algorithms with non-viable complexity.
In this specific case, you aren't gonna need YAGNI—complexity analysis matters, even in the very first version you write.

Exactly, also need to upfront estimate the size of data worst case. Infinitely scalable by default leads to bad perf + complexity.

Cache by fund simple. Ask any question simple code.

Hierarchy of funds to look at things from whole asset manager.

We can keep a cache of the data (encryped of course) on the client to further save cloud cost. Append only.


## Conclusion

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/10_servers.png" title="10 Servers"/>

In the days of cloud computing where architectural costs are more obvious right sizing the architecture to the data is more important.

Most articles titled architecture jump straight in to some feature of the codebase.

So we can build a system that is simpler, more flexible, faster and cheaper because we first fully understood the data.


## Todo

Because data cost many powers of 10 more time to retrieve. And also data shape is a constant in the system. Code changes.

We are not google, our extreme cases will be easier to estimate.

To wrap up: think about the actual problem and the data it needs. Then write functions to manage that data. Don't think about classes and interfaces and closures and reflection and RAII and exceptions and polymorphism and who knows what else.

Can't find any description of this apart from the gaming industry.
This philosophy is called data-oriented design, by the way. For those interested, here are some videos!

## References

[The One Weird Trick: data first, not code first - Even Todd](http://etodd.io/2015/09/28/one-weird-trick-better-code/)
[Data first, not code first - Hacker News](https://news.ycombinator.com/item?id=10291688)
[Queues and their lack of mechanical sympathy - Martin Fowler](https://martinfowler.com/articles/lmax.html#QueuesAndTheirLackOfMechanicalSympathy)
[Practical Examples in Data Oriented Design - Niklas Frykholm](http://gamedevs.org/uploads/practical-examples-in-data-oriented-design.pdf)

*)