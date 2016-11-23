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
#r @"..\packages\MathNet.Numerics\lib\net40\MathNet.Numerics.dll"
module Hidden =
    let x = 1
(**
One of the highlights of the year for me was the [farewell to frp](http://elm-lang.org/blog/farewell-to-frp) post by Evan Czaplicki.
For a long time I've been looking for a simple functional alternative to the MVC UI models.
There were a number of [FRP](https://en.wikipedia.org/wiki/Functional_reactive_programming) alternatives but they all had limitations.
They heavily used Signals and many had inherent memory leak issues.
Evan removed this simplifying the model dramatically.
It resulted in something truly beautiful.
A simple and composible way of building UIs.


## Event Sourcing

Event - something that happens at a specific point (of space and time)
type Event = EventID * 'agg list

what does Agg ID mean? for transaction is it when transaction occured, for others it when created
*)