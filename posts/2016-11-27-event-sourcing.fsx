(**
\---
layout: post
title: "Functional Event Sourcing meets the Elm Architecture"
tags: [elm,event,sourcing]
description: ""
keywords: event sourcing, elm
\---
*)
(*** hide ***)
#r @"..\..\Event\Core\bin\Debug\Lloyd.Core.dll"
namespace Main
open System
open Lloyd.Core

/// A non-empty list
type 'a list1 = List1 of 'a list
type UserID = User of int
(**
One of the highlights of the year for me was the [a farewell to FRP](http://elm-lang.org/blog/farewell-to-frp) post by Evan Czaplicki.
For a long time I've been looking for a simple functional alternative to the MVC UI models.

There were a number of [FRP](https://en.wikipedia.org/wiki/Functional_reactive_programming) alternatives but they all had limitations.
They heavily used signals and many had inherent memory leak issues.

Evan removed this simplifying the model dramatically. It resulted in something truly beautiful. A simple and composible way of building UIs.

Functional Event Sourcing is also a compelling pattern. In some domains like finance it is a game changer.

This post explores how Functional Event Sourcing fits with [the Elm Architecture](http://guide.elm-lang.org/architecture/index.html).
A combined festive example application is developed. The source can be found [here](https://github.com/AnthonyLloyd/Event).

## Functional Event Sourcing

### Event - something that happens at a specific point (of space and time)

Event ID is just time and user / location enough to make it unique.
As well as being a unique identifier of the the event the `EventID` also satisfies all the data required for audit.

### Store

- Can be memory, database or disconected
- Different concurrency model, linear event sourcing, optimistic concurrency. CRDTs


### Features

- the only model that does not lose data
- preserves history, questions not yet asked
- view any previously generated report
- built in audit log
- temporal querying
- easier testing - time travel, regression

*)

type EventID = EventID of time:DateTime * user:UserID

type 'Aggregate ID = Created of EventID

type 'Aggregate Events = (EventID * 'Aggregate list1) list1

type 'Aggregate Stream = IObservable<'Aggregate ID * 'Aggregate Events>

(**

## Demo Application

### Model

*)

type Work = uint16
type Age = byte
type Behaviour = Good | Mixed | Bad

type Toy =
    | Name of string
    | AgeRange of lo:Age * hi:Age
    | WorkRequired of Work

type Elf =
    | Name of string
    | WorkRate of Work
    | Making of Toy ID option

type Kid =
    | Name of string
    | Age of Age
    | Behaviour of Behaviour
    | WishList of Toy ID SetEvent
(**

## Conclusion

https://www.infoq.com/news/2016/04/event-sourcing-anti-pattern


*)