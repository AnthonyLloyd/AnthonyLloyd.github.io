(**
\---
layout: post
title: "Managing Complexity - Or why do you code in F#?"
tags: [complexity,simplicity]
description: ""
keywords: f#, fsharp, functional, complexity, simplicity
\---

This post outlines my views on the often overlooked and misunderstood topic of managing complexity in software development.

The post answers questions I sometimes get on why I prefer to develop systems in F#, a strongly typed functional-first language.
The next time someone asks I can point them here!

## The Questions

- Why don't you program in a language like C++ that has better performance?  

- What is this functional programming and why would you want to use it?  

- Isn't it hard to hire F# developers? Wouldn't it be easier to stick to more standard C#?  

- Why not use something like Python that has lots of libraries to quickly build things?  

## The Answer

The answer is I want to reduce and control complexity.
Get to the best abstraction.

> Simplicity is the ultimate sophistication. <cite>Leonardo da Vinci</cite>

Software langauges and frameworks bring with them different degrees of accidental complexity.

Is the problem you are solving simple enough that you can handle the complexity brought by your language and framework?
Are you sure this will always be the case?

> The primary cause of software project failure is complexity. <cite>Roger Sessions</cite>

## High Complexity

Accidental complexity: ORM, OO, DI, MVVM, SOLID patterns, GOF patterns. Circular references.

> Replacing a dependency injection framework with plain old code. Less magic, no more runtime errors! <cite>Jan Stette</cite>

Building systems out of a batch of data transformation scripts or moving to Microservices doesn't reduce complexity. It dramatically increases it.
Distributed systems are harder to reason about and change. Of course it bring the ability to scaling out but has to be done with great care.
Table of simple vs complex.

### Short term gain long term pain

Key concept is simple vs easy.
[Simple Made Easy](https://www.infoq.com/presentations/Simple-Made-Easy)
Gain in performance vs gain in algorithm performance. High level language vs low level.
Gain in easy startup vs long term complexity
Graph of speed vs time.

> Current development speed is a function of past development quality. <cite>Brian McCallister</cite>

## Why F#?

> Functional languages were discovered, not invented. Many of you work in languages that were invented. And it shows. <cite>Philip Wadler</cite>

Functional programming is simple first. Pick immutable over mutable. Pick data type safety and functions over objects.

Why is functional so simple. Because it comes from pure maths as the simplest programming model.
Risky to pick F# vs risky not to.

Strange that FP doesn't use SOLID/GOF/DI? Its because they are not needed and accidental complexity!

> Languages without union types shouldn't be able to claim type safety as a feature. <cite>Richard Minerich</cite>
*)