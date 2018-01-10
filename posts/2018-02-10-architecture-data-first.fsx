(**
\---
layout: post
title: "Data-First Architecture - Asset Management Case Study"
tags: [functional,architecture,data]
description: "Data-First Architecture - Asset Management Case Study"
keywords: functional, architecture, data
\---

I recently had a light bulb moment while read a [tweet](https://twitter.com/etodd_/status/936587511580844032) from Evan Todd.
It helped bring together some ideas I've had for a while on software architecture.

Data shape should dictate...
First thing you should do is estimate size.

Funtional programming really helps with this mindset. In FP we keep the data and functions separate. We do this because data is simple and pure functions are simple.

I'm going to make the case with an example from my industry. I will argue most asset management systems store and use the wrong data. This limits the functionality and increases the complexity of these systems.


## Case Study - Asset Management Systems

Asset management systems what is the data?


## Ideas

This is v important and FP encourages it. I have a good example in finance that I'm considering blogging in the new year. #fsharp

basically the data without behaviour should dictate the architecture of the system. Then pure functions complete the picture.

To wrap up: think about the actual problem and the data it needs. Then write functions to manage that data. Don't think about classes and interfaces and closures and reflection and RAII and exceptions and polymorphism and who knows what else.

This philosophy is called data-oriented design, by the way. For those interested, here are some videos!


Premature micro-optimization is wasteful. Yet this waste is utterly insignificant compared to implementing algorithms with non-viable complexity.
In this specific case, you aren't gonna need YAGNI—complexity analysis matters, even in the very first version you write.

Exactly, also need to upfront estimate the size of data worst case. Infinitely scalable by default leads to bad perf + complexity.

It is interesting. I'm definitely going to blog about this. My case study is asset management systems. Almost all get the data wrong. They think the data is positions and p&l. It bloats them and limits functionality.

## Conclusion

Can't find any description of this apart from the gaming industry.

In the days of cloud computing where architectural costs are more obvious right sizing the architecture to the data is more important.

Most articles titled architecture jump straight in to some feature of the codebase.

*)