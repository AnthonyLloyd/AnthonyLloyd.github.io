(**
\---
layout: post
title: "System Architecture - Data First"
tags: [functional,architecture,data]
description: "System Architecture - Data First"
keywords: functional, architecture, data
\---

Its about the data stupid

FP-oriented architecture

## Results

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


*)