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

> Data characteristics excluding system functionality should dictate the architecture of the system.

First thing you should do is estimate the size (and rate of change).
Then pure functions complete the picture.

Funtional programming encourages this mindset.
In FP we keep the data and functions separate.
We do this because data is simple and pure functions are simple.
Combining them leads to needless complexity.

I'm going to make the case with an example from my industry.
I will argue most asset management systems store and use the wrong data.
This limits functionality and increases the complexity of these systems.

## Traditional Approach

Asset management systems what is the data?
They think the data is positions and p&l. It bloats them and limits functionality.

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

To wrap up: think about the actual problem and the data it needs. Then write functions to manage that data. Don't think about classes and interfaces and closures and reflection and RAII and exceptions and polymorphism and who knows what else.

Can't find any description of this apart from the gaming industry.
This philosophy is called data-oriented design, by the way. For those interested, here are some videos!

*)