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
Simplicity and the flexibility it brings increases the chance of discovering the best abstraction for your domain.
The classic example of this is 'everything is a file' in Unix.
If you find this for your domain it will put you streets ahead.
Now you are also minimising the inherent complexity of the domain.

> Simplicity is the ultimate sophistication.  
> <cite>Leonardo da Vinci</cite>

Software languages and frameworks bring with them different degrees of accidental complexity.

Is the problem you are solving simple enough that you can handle the complexity brought by your language and frameworks?
Are you sure this will always be the case?

> The primary cause of software project failure is complexity.  
> <cite>Roger Sessions</cite>

## Accidental Complexity

The more I use a functional language the longer the list of frameworks and patterns I consider to have excessive accidental complexity:

- Object oriented programming
- GOF patterns
- SOLID patterns
- Object relational mapping
- Dependency Injection
- Dynamic or weak type systems
- Mutable domain model
- Circular references
- Databinding & MVVM (since learning the Elm architecture)

<img style="border:1px solid black" src="{{site.baseurl}}public/twitter/NoDI.png" title="No DI"/>

Building systems out of a batch of data transformation scripts or moving to Microservices doesn't reduce complexity. It dramatically increases it.
Distributed systems are harder to reason about and change. Of course it brings the ability to scaling out but has to be done with great care.

### Short term gain, long term pain

Rich Hickey has a great [presentation](https://www.infoq.com/presentations/Simple-Made-Easy) explaining the difference between simple and easy.
Short term development speed gains from picking the easy option pails in the long term compared to aiming for simplicity.

<img style="border:1px solid black" src="{{site.baseurl}}public/twitter/DevSpeed.png" title="Dev Speed"/>

### Performance of low vs high level languages 

C can be say 20% faster than F# for a given algorithm.
In my experience getting to the best algorithm produces an order of magnitude increase (if not more) in performance.
Using a high level language provides simplicity to explorer these and use generic performance techniques such as asynchronous programming and memoization.      
Performance is complicated. It is often more about the movement of data than the calculation itself.
I prefer to start in F# (the highest level) and move an algorithm to C (the lowest level) as a last resort.
How often do I need to do this? Very rarely. Only for access to chip optimised linear algebra, optimisation libraries and encoding. 

## Why F#?

Functional programming is simple-first programming.  

Why is functional so simple? Because it comes from mathematics as the simplest possible programming model.
You don't have to understand category theory to benefit from that.

> Functional languages were discovered, not invented. Many of you work in languages that were invented. And it shows.  
> <cite>Philip Wadler</cite>

### Pick data type safety and functions over objects

Data is simple. This is especially true in a strong type system that supports union types. Illegal state can be made unrepresentable.  

Pure functions are simple. They always give the same output for the same input. They are easy to reason about and test.  

Objects are complex. They fuse data and functions with side effects. They are hard to almost impossible to reason about. Testing is painful.
Software developers have to become familiar with the codebase and hold a large model of the system in their head. Don't let them go on holiday.   

<img style="border:1px solid black" src="{{site.baseurl}}public/twitter/UnionTypes.png" title="Union Types"/>

### Pick immutability over mutability

How do you handle long running queries and calculations on a mutable domain model? Concurrent collections? Cross domain locking?
What you have created is a bug paradise. They will get cosy and settle in for the long term.  

In my domain a number of statistics (risk attribution, backtesting, what if analysis) are about changing the state slightly and comparing the results of a calculation.
How would you do this in an object orientated language? Locking and transactions? Clone the world? Visitor pattern? I've been there and wouldn't wish it on anyone.

## Conclusion

Start simple it's the least risky option.
*)