(**
\---
layout: post
title: "Kicking the Debugger habit"
tags: [debugger,debugging,bugs]
description: "How to kick the step through debugging habit"
keywords: f#, fsharp, debugger, debugging, bugs
\---
*)

(*** hide ***)
open System
open System.Text

module Main

(**
This post covers my experience of giving up breakpoint and step-through debugging.

### My Domain

There are some interesting features of the domain I work in that have led me here.
Financial analytics tend to compose many algorithms such as curve fitting, statistics, monte carlo and optimisation.
The public API on the other hand is very simple: given this portfolio, return the risks associated.

A bug report is more likely to be 'this risk number looks a little odd' rather than 'an exception was thrown and here is the stack trace'.
I can't agree with the idea that you should only unit test your public APIs.
Rather, this should be: you need to unit test your public APIs, but if your domain is sufficiently complex, you also need to unit test internal modules.
What is key is if a bug report queries an API result how quickly could you investigate and resolve any potential issue?

### What's wrong with debugging?

In my field:

- It's not scalable - for larger code paths setting breakpoints and stepping through is just not feasible. It's like finding a needle in a haystack.
- It's limited in power - even mature debugging frameworks such as in Visual Studio are limited in the kind of conditional logic you can use while debugging.  
- It's time consuming - many a good hour can be spent pressing F5/F10/F11 in a zombie like state only to restart and try again.

<img style="border:1px solid black" src="/{{site.baseurl}}public/twitter/debugging.png" title="debugging"/>

### What's the alternative?

Since starting to use [Expecto](https://github.com/haf/expecto), I've been using the command line to run unit tests.
[Expecto](https://github.com/haf/expecto) does integrate with Visual Studio and can even do live unit testing in VS Code.
I've found using a set of commands I've built up in FAKE to be more flexible and productive e.g.

*)
    test integration
    test all
    test 64 debug --stress 2
(**

[Expecto](https://github.com/haf/expecto) encourages using normal code for organisation, setup & teardown and parameterisation of tests, instead of a limited framework of attributes.

Now apply this concept to debugging.
With a debug module in the core of a codebase that is conditional on the debug configuration, code can be annotated with validation and some debug output.
The command line records a history of the test results and validation output.
Once complete, compiling in release ensures all diagnostic code is removed.

This started out as simple functions to `printfn` data being sequenced and piped, but expanded into functions to count calls, check for NaNs globally, serialize function inputs and outputs, test convergence of numbers etc.
This is normal code and there is huge scope for adding conditional logic.

### Conclusion

Kicking the debugger habit has given me a productivity boost.
It forces me to think more logically about how I validate and break down a problem.

It also reduces the complexity of the tooling.
Finding and fixing bugs feels more like coding and unit testing.
I can use a simpler code editor plus the command line.

The result is I now have more confidence that once I've created the initial bug test I will be able to resolve it quickly.

### Appendix

I've been asked for some sample code from the debug module.
The code below should hopefully start to give an idea of what can be done.  


*)

//#if DEBUG
[<AutoOpen>]
module OverflowAndNaNCheck =
    open Checked

    let inline private isNaN v = match box v with | :? float as v -> Double.IsNaN v | _ -> false
    let inline (/) a b = let c = a/b in if isNaN c then failwithf "NaN found: %A / %A = %A" a b c else c
    let inline (+) a b = let c = a+b in if isNaN c then failwithf "NaN found: %A + %A = %A" a b c else c
    let inline (-) a b = let c = a-b in if isNaN c then failwithf "NaN found: %A - %A = %A" a b c else c
    let inline (*) a b = let c = a*b in if isNaN c then failwithf "NaN found: %A * %A = %A" a b c else c
    let inline ( ** ) a b = let c = a**b in if isNaN c then failwithf "NaN found: %A ** %A = %A" a b c else c
    let inline sqrt a = let c = sqrt a in if isNaN c then failwithf "NaN found: sqrt %A = %A" a c else c
    let inline log a = let c = log a in if isNaN c then failwithf "NaN found: log %A = %A" a c else c
    let inline log10 a = let c = log10 a in if isNaN c then failwithf "NaN found: log10 %A = %A" a c else c
    let inline asin a = let c = asin a in if isNaN c then failwithf "NaN found: asin %A = %A" a c else c
    let inline acos a = let c = acos a in if isNaN c then failwithf "NaN found: acos %A = %A" a c else c
    let inline atan a = let c = atan a in if isNaN c then failwithf "NaN found: atan %A = %A" a c else c

module Dbg =
    let private rand = Random()
    let mutable private randN = None
    type atRandom(n:int) =
        do
            randN <- Some n
        interface IDisposable with
            member __.Dispose() = randN <- None
    let write fmt =
        let sb =
            let n = DateTime.Now
            let sb = StringBuilder("DEBUG ")
            sb.Append(n.ToString("dd MMM HH:mm:ss.fffffff")) |> ignore
            sb.Append("> ") |> ignore
            sb
        Printf.kbprintf (fun () ->
            if Option.isNone randN || Option.get randN |> rand.Next = 0 then
                let old = Console.ForegroundColor
                try
                    Console.ForegroundColor <- ConsoleColor.Red
                    sb.ToString() |> Console.WriteLine
                finally
                    Console.ForegroundColor <- old
        ) sb fmt
    let writeIf condition fmt = if condition() then write fmt
    let runIf condition fn = if condition() then fn()
    let pipe fmt = fun a -> write fmt a; a
    let seq desc s =
        let s = Seq.cache s
        Seq.iter (write "%s: %A" desc) s
        s
    let fun1 desc (f:'a->'b) = fun a -> let b = f a in write "%s - Input: %A\t\t Output: %A" desc a b; b
    let fun2 desc (f:'a->'b->'c) = fun a b -> let c = f a b in write "%s - Input: %A\t\t Output: %A" desc (a,b) c; c
    let fun3 desc (f:'a->'b->'c->'d) = fun a b c -> let d = f a b c in write "%s - Input: %A\t\t Output: %A" desc (a,b,c) d; d
    type counter(desc:string) =
        let mutable count = 0
        member __.Count = count
        member __.Increment() = count <- count + 1
        member inline m.Calls fn = fun a -> m.Increment(); fn a
        interface IDisposable with
            member __.Dispose() = write "%s count = %i" desc count
    let descendingChecker desc =
        let mutable last = infinity
        fun x ->
            if x>last then write "%s - should be descending but %A > %A" desc x last
            else last<-x
    let mutable private functionMap = Map.empty
    let addFun (key:string) (fn:unit->unit) = functionMap <- Map.add key fn functionMap
    let runFun (key:string) = Map.find key functionMap ()
//#endif

(**
*)