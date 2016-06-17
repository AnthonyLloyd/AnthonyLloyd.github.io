(**
\---
layout: post
title: "F# Elm like UI model"
tags: [Elm,UI]
description: ""
keywords: elm, ui
\---
*)

(*** hide ***)
namespace Main

open System
open System.Diagnostics
        
(**
This is the...

## Background

To performance test code...

## Statistics

Lazy evaluation can be used to produce an [online](https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Online_algorithm) sample statistics sequence.

## Statistics code
*)
type SampleStatistics = {N:int;Mean:float;Variance:float}
                        member s.StandardDeviation = sqrt s.Variance
                        member s.MeanStandardError = sqrt(s.Variance/float s.N)
(**
## Performance testing

Three ...

## Performance testing code
*)
module Performance =
    /// Find the iteration count to get to at least the metric target.
    let inline private targetIterationCount metric metricTarget f =
        let rec find n =
            let item = metric n f
            if (item<<<3)>=metricTarget then n*int metricTarget/int item+1
            else find (n*10)
        find 1
        

(**
## Conclusion

The performance testing functions have a very simple signature.
The statistics functions just take the function to be measured.
The compare functions just take the two functions to be compared.

The statistics functions give an overview of a function's performance.
These can easily be combined to produce a useful performance report.

The compare functions can be used in unit tests since they are a relative test and hence should be independent of the machine.
They are also fast since they stop as soon as the given confidence level is achieved.
The compare functions could also be extended to test if a function is a given percentage better than another.  

Modularity from higher-order functions and lazy evaluation together with a little maths have produced a simple yet powerful performance testing library.
*)