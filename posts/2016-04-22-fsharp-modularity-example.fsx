(**
\---
layout: post
title: "Modularity from Lazy Evaluation - Richardson Extrapolation F# Example"
tags: [modularity,higher-order,lazy evaluation]
description: "An F# example of how higher-order functions together with lazy evaluation can reduce complexity and lead to more modular software"
keywords: f#, fsharp, functional, higher-order functions, lazy evaluation, modularity
\---
*)

(*** hide ***)
module Main

open System.Collections.Generic

module Seq =
    /// Returns an infinite sequence that contains the elements generated by the given computation.
    let rec unfoldInf f s =
        seq {
            yield s
            yield! unfoldInf f (f s)
        }
    /// Returns a sequence of each element in the input and its two predecessors.
    let triplewise (s:_ seq) =
        seq { use e = s.GetEnumerator() 
            if e.MoveNext() then
                let i = ref e.Current
                if e.MoveNext() then
                    let j = ref e.Current
                    while e.MoveNext() do
                        let k = e.Current 
                        yield (!i, !j, k)
                        i := !j
                        j := k }
/// Returns the third item from a 3-tuple.
let inline trd (_,_,i) = i

(**
This is an F# example of how higher-order functions together with lazy evaluation can reduce complexity and lead to more modular software.

## Background

Richardson extrapolation is a method of combining multiple function estimates to increase estimate accuracy.
This post will cover estimating the derivative and the integral of a function to an arbitrary accuracy.

*)

/// Derivative estimate.
let derivativeEstimate f x h = (f(x+h)-f(x-h))/h*0.5

/// Integral estimate (h = (b-a)/n where n is an integer).
let integralEstimate f a b h = h*(f a+f b)*0.5 + h*Seq.sumBy f {a+h..h*2.0..b}

(**
Both the derivative and integral estimate can be shown to have an error term that has even powers of $h$.

$$$
Actual = Estimate(h) + e_1 h^2 + e_2 h^4 + \cdots

Richardson extrapolation combines multiple estimates to eliminate the lower power error terms.

$$$
R_{ij} =
\left\{
    \begin{align}
        & Estimate\left(\frac{h}{2^i}\right) & j=0 \\
        & \frac{4^j R_{i,j-1} - R_{i-1,j-1}}{4^j-1} & i \geq j, \, j > 0 \\
    \end{align}
\right.

For small $h$ this rapidly improves the accuracy of the estimate.

$$$
Actual = R_{ij} + \hat{e}_1 h^{2j+2} + \hat{e}_2 h^{2j+4} + \cdots

This produces a triangle of improving estimates.

$$$
\begin{matrix}
R_{00} & \\
& \searrow & \\
R_{10} & \rightarrow & R_{11} \\
& \searrow & & \searrow \\
R_{20} & \rightarrow & R_{21} & \rightarrow & R_{22} \\
\vdots & & \vdots & & \vdots & \ddots \\
\end{matrix}

The stopping criteria is usually that $|R_{n-2,n-2}-R_{n-1,n-1}|$ and $|R_{n-1,n-1}-R_{n,n}|$ are within a desired accuracy.  

## First attempt

The first implementation will be attempted without using functional techniques.

*)

/// Stopping criteria for a given accuracy and list of Richardson estimates.
let stoppingCriteriaNonFunctional tol (rows:List<float array>) =
    let c = rows.Count
    c>2 &&
    abs(rows.[c-3].[c-3]-rows.[c-2].[c-2])<=tol &&
    abs(rows.[c-2].[c-2]-rows.[c-1].[c-1])<=tol

/// The Richardson formula for a function estimate that has even power error terms.
let richardsonFormula currentRowR previousRowR pow4 =
     (currentRowR*pow4-previousRowR)/(pow4-1.0)

/// Derivative accurate to tol using Richardson extrapolation.
let derivativeNonFunctional tol h0 f x =
    let richardsonRows = List<float array>()
    let mutable h = h0*0.5
    richardsonRows.Add ([|derivativeEstimate f x h0|])
    let rec run () =
        let lastRow = Seq.last richardsonRows
        let row = Array.zeroCreate (Array.length lastRow+1)
        row.[0] <- derivativeEstimate f x h
        let mutable pow4 = 4.0
        for i = 0 to Array.length lastRow-1 do
            row.[i+1] <- richardsonFormula row.[i] lastRow.[i] pow4
            pow4 <- pow4*4.0
        if stoppingCriteriaNonFunctional tol richardsonRows then Array.last lastRow
        else
            richardsonRows.Add row
            h<-h*0.5
            run()
    run()
    
/// Iterative integral estimate (h is half the value used in the previous estimate).
let integralEstimateIterative f a b previousEstimate h =
    previousEstimate*0.5+h*Seq.sumBy f {a+h..h*2.0..b}
    
/// Integral accurate to tol using Richardson extrapolation.
let integralNonFunctional tol f a b =
    let richardsonRows = List<float array>()
    let mutable h = (b-a)*0.5
    richardsonRows.Add ([|(f a+f b)*h|])
    let rec run () =
        let lastRow = Seq.last richardsonRows
        let row = Array.zeroCreate (Array.length lastRow+1)
        row.[0] <- integralEstimateIterative f a b lastRow.[0] h
        let mutable pow4 = 4.0
        for i = 0 to Array.length lastRow-1 do
            row.[i+1] <- richardsonFormula row.[i] lastRow.[i] pow4
            pow4 <- pow4*4.0
        if stoppingCriteriaNonFunctional tol richardsonRows then Array.last lastRow
        else
            richardsonRows.Add row
            h<-h*0.5
            run()
    run()
    
(**

## The refactor

There is a lot of duplicate code in the functions above.

The object-oriented solution to this is the [Template Method](https://en.wikipedia.org/wiki/Template_method_pattern) design pattern.
The downside of this approach is that it results in a lot of boiler-plate code with state being shared across multiple classes.

Leif Battermann has a very good [post](http://blog.leifbattermann.de/2016/03/06/template-method-pattern-there-might-be-a-better-way/)
on how this can be solved in a functional way using higher-order functions. This results in much more modular and testable code.

Unfortunately, in this case higher-order functions alone will not solve the problem.
The integral estimate needs the previous estimate for its calculation.
This difference in state means the higher-order function would need different signatures for the derivative and integral.

The solution can be found in the excellent paper [Why Functional Programming Matters](http://www.cse.chalmers.se/~rjmh/Papers/whyfp.pdf) by John Hughes.
Lazy evaluation is a functional language feature that can greatly improve modularity.
  
Lazy evaluation allows us to cleanly split the implementation into three parts:

1. A function that produces an infinite sequence of function estimates.
2. A function that produces a sequence of Richardson estimates from a sequence of function estimates.
3. A function that iterates a sequence of Richardson estimates and stops at a desired accuracy.

Lazy evaluation can be achieved in F# by using the `Seq` collection and also the `lazy` keyword.

## Final code
*)

/// Infinite sequence of derivative estimates.
let derivativeEstimates f x h0 = Seq.unfoldInf ((*)0.5) h0 |> Seq.map (derivativeEstimate f x)        

/// Infinite sequence of integral estimates.
let integralEstimates f a b =
    let h0 = (b-a)*0.5
    let i0 = (f a+f b)*h0            
    Seq.unfoldInf ((*)0.5) h0 |> Seq.scan (integralEstimateIterative f a b) i0

/// Richardson extrapolation for a given estimate sequence.
let richardsonExtrapolation s =
    let createRow previousRow estimate_i =
        let richardsonAndPow4 (current,pow4) previous = richardsonFormula current previous pow4, pow4*4.0
        Seq.scan richardsonAndPow4 (estimate_i,4.0) previousRow |> Seq.map fst |> Seq.cache
    Seq.scan createRow Seq.empty s |> Seq.tail

/// Stopping criteria for a given accuracy and sequence of Richardson estimates.
let stoppingCriteria tol s =
    Seq.map Seq.last s |> Seq.triplewise |> Seq.find (fun (a,b,c) -> abs(a-b)<=tol && abs(b-c)<=tol) |> trd

/// Derivative accurate to tol using Richardson extrapolation.
let derivative tol f x h0 = derivativeEstimates f x h0 |> richardsonExtrapolation |> stoppingCriteria tol

/// Integral accurate to tol using Richardson extrapolation.
let integral tol f a b = integralEstimates f a b |> richardsonExtrapolation |> stoppingCriteria tol

(**
## Conclusion

Lazy evaluation makes it possible to modularise a program into a producer that constructs a large number of possible answers, and a consumer that chooses the appropriate one.

Without it, either state has to be fully generated upfront or production and consumption have to be done in the same place.

Higher-order functions and lazy evaluation can be applied to all software layers.
The [Why Functional Programming Matters](http://www.cse.chalmers.se/~rjmh/Papers/whyfp.pdf) paper has examples of their use in game artificial intelligence and other areas.
In my experience the complexity reduction it produces allows software functionality to be pushed further more easily.

Modularity is the most important concept in software design.
It makes software easier to write, understand, test and reuse.
The features of functional languages enable improved modularity.
*)