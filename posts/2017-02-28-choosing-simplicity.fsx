(**
\---
layout: post
title: "Choosing Simplicity - not the easy option"
tags: [simplicity,easy]
description: "This is a short post on a few recent events that in my mind share a common idea"
keywords: f#, fsharp, simplicity, easy
\---
*)

(*** hide ***)
module Main

(**
This is a short post on a few recent events that are examples of a common idea.

### Expecto - testing library vs testing framework

[Expecto](https://github.com/haf/expecto) is a new testing library that has chosen a different approach to existing testing frameworks.
It's designed as a library to be used in a testing exe project instead of code written to run inside a framework.
Tomas Petricek hits the nail on head with his [post](http://tomasp.net/blog/2015/library-frameworks/) on why frameworks are limited compared to libraries.

In [Expecto](https://github.com/haf/expecto), tests are constructed as values, so normal code is used to filter, parameterise, reuse and compose them.
By backing up and choosing a simpler evolutionary path it's hoped the library can go further than current testing frameworks.
This was not the easy option and several components like the Visual Studio Plugin and Visual Studio Code integration have had to be built.
There has also been some scepticism that a new approach is needed.

Because of its simplicity [Expecto](https://github.com/haf/expecto) already has some unique features:

- The library itself is easy to test since it can be run inside tests.
- Structuring tests in lists and trees enables more flexible configuration.
- Tests can be run in parallel, some can be globally sequential, and some can be sequential in small groups.
- Stress testing can be used to randomly run a test suite in parallel for a long period to catch rare bugs and memory leaks.
- Fast statistical relative performance tests can be run as part of normal testing.

### Serialization - library vs hand coding

On one of my own event sourcing projects I've taken the decision to hand code the serialization and not use a library.

You've done what? You are crazy.

I need to make sure the serialization will cope with schema migration and always be backwardly compatible.
I also have specific serialization compression I want to make use of e.g. [FastPFOR](https://github.com/Genbox/CSharpFastPFOR).

I took inspiration from the Haskell [Data.Serialize](https://hackage.haskell.org/package/cereal-0.5.4.0/docs/Data-Serialize.html) library. 

*)

type Resize = byte[] -> int -> byte[]
type State = byte[] * int
type 'a SerializeGet = State -> 'a * int
type SerializePut = Resize -> State -> State

type 'a Serialize =
    {
        Put: 'a -> SerializePut
        Get: 'a SerializeGet
    }

(**

Monads can be made for `SerializePut` and `SerializeGet`.
This makes composing a type serializer from more primitive serializers very easy.
Essentially after the primitives have been built it takes only two lines of simple code per field. 

Using a great testing library (see what I did there) serialization is surprisingly easy to test thoroughly.
Property based testing is used to ensure all serialization roundtrips correctly. 
This includes tests to cover schema migration and backward compatibility.

For the cost of a little extra code on schema change a simple serialization library can be built.
It has the advantage of not needing any reflection or code generation.
Also, because it is bespoke it should have great performance and produce smaller messages. 

### Support for type classes and HKTs in F#

Don Syme recently [commented](https://github.com/fsharp/fslang-suggestions/issues/243#issuecomment-282455245) on adding type classes and HKTs to F#.

Like many others I've never designed a programming language but that is not going to stop me commenting on its evolution.
From what I can see language design has a greater proportion of irreversible decisions than other areas of software engineering.
It's well known that people spend too much time on reversible decisions and too little on irreversible ones.

Sometimes you need to simmer an idea down and add it at the right point to get the tastiest result.

There is some pressure from the community to get something in after it was announced that C# was exploring adding type classes.
The decision to hold off can't be an easy one.

Personally, I'm happy to wait if it ensures F# is kept as simple and coherent as possible.

## Conclusion

Simple solutions are easier to understand, generalise more naturally, and are more amenable to change.

Simplicity is not the easy option but it is worth fighting for.

*)