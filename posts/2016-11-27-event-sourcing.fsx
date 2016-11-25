(**
\---
layout: post
title: "Functional Event Sourcing meets The Elm Architecture"
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
type 'a SetEvent =
    | SetAdd of 'a
    | SetRemove of 'a

(**
One of the highlights of the year for me was the [a farewell to FRP](http://elm-lang.org/blog/farewell-to-frp) post by Evan Czaplicki.
For a long time I've been looking for a simple functional alternative to the MVC UI models.

There were a number of [FRP](https://en.wikipedia.org/wiki/Functional_reactive_programming) alternatives but they all had limitations.
They heavily used signals and many had inherent memory leak issues.

Evan removed this simplifying the model dramatically. It resulted in something truly beautiful. A simple and composible way of building UIs.

Event Sourcing is also a compelling pattern. In some domains like accounting it is a perfect fit.

This post explores how Functional Event Sourcing fits with [the Elm Architecture](http://guide.elm-lang.org/architecture/index.html).
A combined festive application is developed to streamline Santa's workload. The application can be found [here](https://github.com/AnthonyLloyd/Event).

## Functional Event Sourcing

### Event

*)
type EventID = EventID of time:DateTime * user:UserID
(**

An event is something that happens at a specific point.
In physics an event is a change that happens at a point in spacetime.
In event sourcing the point is a unique identifier of the event.

In most systems this can just be the time and user who created the event.
This `EventID` can also include additional data required to make it unique.

As well as being a unique identifier of the the event the `EventID` also satisfies all the data requirement for audit.

### Aggregate

*)
type 'Aggregate ID = Created of EventID

type 'Aggregate Events = (EventID * 'Aggregate list1) list1
(**

An aggregate is a collection of events that are bound together by a root entity.
External entities can only hold a reference to the root entity identifier.
An aggregate is a unit of consistency that has atomicity and autonomy.

In the example application we have the following domain model.
Each case represents a possible change to the aggregate.

Most of the events are simple field changes but we can also have events such as `Recalled` for `Toy` for example.

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

The rules for domain model schema migration and data serialization are

- Cases cannot be removed or their data type changed.
- Cases cannot be reordered.
- Cases can be renamed.
- Cases can be added. Legacy code will ignore these. 

FsPickler can be configured to comply with these rules making it easy to serialize events.

### Store

*)
type 'Aggregate MemoryStore =
    {
        Latest: Map<'Aggregate ID, 'Aggregate Events>
        Observers: IObserver<Map<'Aggregate ID, 'Aggregate Events>> list
    }
(**

Stores are the database of event sourcing.
They can be in memory, remote or disconnected for example.

Many different concurrency models are possible.
In the example application we have linear event sourcing with optimistic concurrency which is the simplest and corresponds to most relational database applications.

More fine grained concurrency is possible and `Making` on `Elf` would be a good candidate as only the santa process changes this.
Advanced concurrency models are also possible with event sourcing where events are designed to commute such as [CRDTs](https://en.wikipedia.org/wiki/Conflict-free_replicated_data_type).
These enable disconnected systems. Git is a good example of a successful disconnected system.

### Benefits of event sourcing

- the only model that does not lose data
- view any previously generated report
- built in audit log
- temporal querying
- preserves history, questions not yet asked
- well defined, simple schema migration
- easier testing - regression, time travel debug

## Example Application

The application has two background processes running continuously.
The first is the kids process that adds and removes toys to the kids Christmas wishlists.
The second is the santa process that assigns free elfs to make toys in the priority order of kid behaviour and request time.
All the screens update in realtime and changes can be made to any of the entities in the domain.

![Santa's Summary]({{site.baseurl}}public/event/Santa.png "Santa's Summary")

<table style="border:0px">
	<tr>
		<td style="background-color:white;border:0px"><img src="{{site.baseurl}}public/event/Kid.png" title="Kid Screen" width="340px" height="434px"/></td>
		<td style="background-color:white;border:0px"><img src="{{site.baseurl}}public/event/Toy.png" title="Toy Screen" width="340px" height="434px"/></td>
		<td style="background-color:white;border:0px"><img src="{{site.baseurl}}public/event/Elf.png" title="Elf Screen" width="340px" height="434px"/></td>
	</tr>
</table>

This turns out to be quite a complicated problem we have solved.
I would be interested to see a more traditional solution to this problem in OO and a relational data model.
I can only imagine that both the domain model and codebase would become much more complicated.

Editor reused across fields
Full validation
expand elm for commands and subscriptions

## Conclusion

event sourcing fits naturally with the subscirpiton model in elm.

time travel debug is a feature of both and work well together.

type safe and testible system.

leads me to the conclusion that below is wrong.

https://www.infoq.com/news/2016/04/event-sourcing-anti-pattern


*)