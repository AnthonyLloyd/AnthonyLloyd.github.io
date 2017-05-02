(**
\---
layout: post
title: "Kicking the Debugger"
tags: [debugger,debugging,bugs]
description: "How to kick the step through debugging habit"
keywords: f#, fsharp, debugger, debugging, bugs
\---
*)

(*** hide ***)
module Main

(**
This post covers my experience of giving up break point and step through debugging.

### My Domain

There are some interesting features of the domain I work in that have lead me here.
Financial analytics tends to compose a number of algorithms such as curve fitting, statistics, monte carlo and optimisation.
The public API on the other hand is very simple: given this portfolio return the risks associated.

A bug report is more likely to be this risk number looks a little odd rather than an exception was thrown and here is the stack trace.
This means I can't really agree with the idea that you should only unit test your public APIs.
For me this should be: you definitely need to unit test your public APIs but if your domain is sufficiantly complex you also need to unit test internal modules.
The key is if a bug report comes in that queries an API result how quickly could you investigate and resolve any possible issue.

### What's wrong with debugging?

For the areas I work on:

- Its just not scalable
    For larger code paths setting break points and stepping through is just not feasable. Its like finding a needle in a haystack.
- Its limited in power
    Even mature debugging frameworks such as in Visual Studio are limited in the kind of conditional logic you can use while debugging.  
- Its time consuming
    Many a good hour can be spent pressing F5/F10/F11 in a zombie like state only to restart and try again.

### What's the alternative?

Since starting to use [Expecto](https://github.com/haf/expecto) I've been using the command line to run unit tests.
[Expecto](https://github.com/haf/expecto) does integrate with visual studio and can even do live unit testing.
I've just found using a set of commands I've built up in FAKE to be more flexible and productive e.g.

*)
    test integration
    test all
    test 64 debug --stress 2
(**

[Expecto](https://github.com/haf/expecto) encourages using normal code for organisation, setup & teardown and parameterisation of tests instead of a limited framework of attribute coding.  
I've applied this idea to debugging. I have a debug module in the core of my codebase that is conditional on the debug configuration.
I can annotate code with validation and some debug output.
The command line records a history of the test results and validation output.
Once complete compiling in release ensures all of this diagnostic code is completely removed.

This started out as simple functions to `printfn` data being sequenced and piped but expanded into functions to count calls, check for NaNs globally, serialize function inputs and outputs, test convergence of numbers etc.
This is normal code and there is huge scope for adding conditional logic to help validate and break down the problem.

### Conclusion

For me kicking the debugger habit has been a productivity boost.
It forces me to think more logically about how I validate and break down a problem.

It reduces the complexity of my tooling to just normal code.
Finding and fixing bugs feels more like coding and unit testings.
I can use a simpler code editor plus the command line.

The result is I now have more confidence that once I've created the initial failing unit test I will be able to resolve it quickly.
*)