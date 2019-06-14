(**
\---
layout: post
title: "Managing Complexity - Or \"Why do you code in F#?\""
tags: [complexity,simplicity]
description: ""
keywords: f#, fsharp, functional, complexity, simplicity
\---

This post outlines my views on the often overlooked and misunderstood topic of managing complexity in software development.

It answers questions I'm asked sometimes on why I prefer to develop systems in F#, a strongly typed functional-first language.

The next time someone asks I can refer them here!

## The Questions

- Why don't you program in a language like C++ that has better performance?  

- What is this functional programming and why would you want to use it?  

- Isn't it hard to hire F# developers? Wouldn't it be easier to stick to more standard C#?  

- Why not use something like Python that has lots of libraries to quickly build things?  

## The Answer

The answer is I want to reduce and control complexity.  

Simplicity and the flexibility it brings increases the chance of discovering the best abstraction for the domain.
The classic example of this is 'everything is a file' in Unix.
If you find this for your domain, it will put you streets ahead.
Now you are minimising the inherent complexity of the domain.

> Simplicity is the ultimate sophistication.  
> <cite>Leonardo da Vinci</cite>

Software languages and frameworks bring with them different degrees of accidental complexity.

Is the problem you are solving simple enough that you can handle this additional complexity?
Are you sure that will always be the case?

> The primary cause of software project failure is complexity.  
> <cite>Roger Sessions</cite>

## Accidental Complexity

The more I use a functional language the longer the list of patterns I consider to have excessive accidental complexity:

- Object oriented programming
- GOF patterns
- SOLID patterns
- Null
- Exceptions
- Circular dependencies
- Tests rather than types
- Object relational mapping
- Dependency injection
- Dynamic or weak type systems
- Mutable domain model
- Framework rather than library
- Databinding & MVVM (since learning the [Elm Architecture]({% post_url 2016-06-20-fsharp-elm-part1 %}))

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/NoDI.png" title="No DI"/>

In an attempt to reduce complexity, there is a trend of building systems out of batches of data transformation scripts or moving to microservices.
These don't reduce complexity; they dramatically increase it.
Distributed systems are harder to reason about and change. Doing this to be able to scale out can make sense but it has to be done with great skill.

### Short term gain, long term pain

> Simplicity is prerequisite for reliability.  
> <cite>Edsger W Dijkstra</cite>

Rich Hickey has a brilliant [presentation](https://www.infoq.com/presentations/Simple-Made-Easy) explaining the difference between simple and easy in software development.
Every software developer needs to watch this. It is also very entertaining.

Some key takeaways are:

- Simplicity is a choice.
- Ignoring complexity will slow you down over the long term. On throwaway or trivial projects, nothing much matters.
- Information is simple. Don't ruin it by hiding it behind a micro-language.
- Ability to reason about your program is critical.
- Guard rail programming, i.e. tests rather than types, is not simple. It does not help you get to where you want to go.  


<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/DevSpeed.png" title="Dev Speed"/>

### Performance of low vs high level languages 

Let's say C can be 20%-50% faster than F# for a given algorithm. In my experience, getting to the best algorithm can produce an order of magnitude or more increase in performance.
I've seen systems that have become so complicated that even though a better algorithm is known it can't be used because it would require a large rewrite.
The complexity limit had been reached. This is particularly true of many C++ quant libraries.  

Using a high level functional language with ten times less code provides simplicity to explore better algorithms and use generic performance techniques such as asynchronous programming and memoization.   

<a href="http://flyingfrogblog.blogspot.com/2015/11/c-vs-c-performance-deleted.html"><img style="border:1px solid black" src="/{{site.baseurl}}public/perf/cpp.png" title="C++ vs C# - Jon Harrop"/></a>

Performance is complicated. It is often more about the movement of data than the calculation itself.
I prefer to start in the highest level language (F#) and move an algorithm to the lowest level language (C) as a last resort.
How often do I need to do this? Very rarely. Really only for access to chip optimised linear algebra and optimisation libraries. 

## Why F#?

Functional programming is simple-first programming.  

Why is functional programming so simple? Because it comes from mathematics as the simplest possible programming construct.
You don't have to understand category theory to benefit from this.

> Functional languages were [discovered](https://www.youtube.com/watch?v=IOiZatlZtGU&t=27m52s), not invented. Many of you work in languages that were invented. And it shows.  
> <cite>Prof Philip Wadler</cite>

Functional programming is not a fad the profession can ignore. Its rigorous mathematical foundation means that it will be around forever.
Software developers should be encouraged to learn the benefits it provides. 

### Choose data type safety and functions over objects

Data is simple. This is especially true in a strong type system that supports union types. Illegal state can be made unrepresentable.  

Pure functions are simple. They always give the same output for the same input. They are easy to reason about and test.  

Objects are complex. They fuse data and functions with side effects. They hold links to other objects. They are hard to almost impossible to [reason about](https://fsharpforfunandprofit.com/posts/is-your-language-unreasonable/). Testing is painful.
Software developers have to become familiar with the codebase and hold a large model of the system in their head. Don't let them go on holiday.   

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/UnionTypes.png" title="Union Types"/>

### Choose immutability over the mutant

How do you handle queries and calculations (possibly long running) on a mutable domain model? Concurrent collections? Cross domain locking?
What you have created is a bug paradise. They will get cosy and settle in for the long term.  

In the domain I work in a number of statistics (risk attribution, backtesting, what if analysis) are about changing the state slightly and comparing the results of a calculation.
How would you do this in a mutable domain model? Locking and transactions? Clone the world? Visitor pattern? I've been there and wouldn't wish it on anyone.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/testable.png" title="Testable"/>

### Choose linear composition over spaghetti

There is something missing between perfect data, pure functions and beautiful systems.

Functional programming allows functions to be passed around just like data. Functions can accept other functions as an input in a generic way. They are called higher-order functions.

This may sound alien but it provides a quantum leap in terms of code reuse and assembling systems.
In fact, I didn't understand the full power of code reuse until I started programming in a functional language.

Object orientated programming has a poorer method of assembling systems. Objects are given to other objects. Dependency injection has been invented to make this easier.
This helps but results in systems that are harder to reason about and increases complexity.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/OOP.png" title="OOP"/>

## Conclusion

We need to talk about object oriented programming. We have patterns to work around its deficiencies. These, taken to the limit, make it look more functional.
Every release of Java and C# add more functional features. Unfortunately, this will not ultimately fix these languages, it just increases their complexity.
As an industry we need to decide how we should move on.

For a one off, short term, or simple project you can handle additional complexity in exchange for some quick productivity gains. This is what Python, Rails, R etc are great at.

For long term critical systems that evolve over time the focus must be on simplicity. The programming language and tools need to help us do this. This is why I code in F#.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/complexity_ship.png" title="Complexity Ship"/>
*)