(**
\---
layout: post
title: "F# Modularity Example - Richardson Extrapolation"
tags: [modularity,higher-order,lazy evaluation]
description: "An F# example of how higher-order functions and lazy evaluation can reduce complexity and lead to more modular reusable software"
keywords: f#, fsharp, functional, higher-order functions, lazy evaluation, modularity
\---
*)

(*** hide ***)
module Main

open System.Collections.Generic

module Seq =
    let rec unfoldInf f s =
        seq {
            yield s
            yield! unfoldInf f (f s)
        }
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
let inline trd (_,_,i) = i

(**
This is an F# example of how higher-order functions and lazy evaluation can reduce complexity and lead to more modular reusable software.

## Background

Richardson extrapolation is a method of combining multiple function estimates to increase estimate accuracy.
In this post we will cover estimating the derivative and the intergral of a function to an arbitrary accuracy.

*)

/// Derivative estimate
let derivativeEstimate f x h = (f(x+h)-f(x-h))/h*0.5

/// Integral estimate (h = (b-a)/n where n is an integer)
let integralEstimate f a b h = h*(f a+f b)*0.5 + h*Seq.sumBy f {a+h..h*2.0..b}

(**
Both the derivative and intergal estimate can be shown to have an error term that has even powers in h.

$$$
Actual = Estimate(h) + e_1 h^2 + e_2 h^4 + e_3 h^6 + \cdots

$$$
\begin{align}
R_{i0} &= Estimate\left(\frac{h}{2^i}\right) \\
R_{ij} &= \frac{4^j R_{i,j-1} - R_{i-1,j-1}}{4^j-1} \\
\end{align}

$$$
\begin{matrix}
\mathbf{R_{00}} & \\
& \searrow & \\
\mathbf{R_{10}} & \rightarrow & \mathbf{R_{11}} \\
& \searrow & & \searrow \\
\mathbf{R_{20}} & \rightarrow & \mathbf{R_{21}} & \rightarrow & \mathbf{R_{22}} \\
\vdots & & \vdots & & \vdots & \ddots \\
\end{matrix}

## First attempt

The first attempt at an implimentation will not use functional techniques.
*)

let richardsonStoppingCriteriaNF tol (rows:List<float array>) =
    let c = rows.Count
    c>2 && abs(rows.[c-3].[c-3]-rows.[c-2].[c-2])<=tol && abs(rows.[c-2].[c-2]-rows.[c-1].[c-1])<=tol

/// Derivative accurate to tol using Richardson extrapolation 
let derivativeNonFunctional tol h0 f x =
    let mutable h = h0
    let richardsonRows = List<float array>()
    let rec run () =
        let lastRow = if richardsonRows.Count=0 then Array.empty else richardsonRows.[richardsonRows.Count-1]
        let row = Array.zeroCreate (lastRow.Length+1)
        row.[0] <- derivativeEstimate f x h
        let mutable pow4 = 4.0
        for i = 0 to lastRow.Length-1 do
            row.[i+1] <- (row.[i]*pow4-lastRow.[i])/(pow4-1.0)
            pow4 <- pow4*4.0
        if richardsonStoppingCriteriaNF tol richardsonRows then lastRow.[lastRow.Length-1]
        else
            richardsonRows.Add row
            h<-h*0.5
            run()
    run()
    
/// Intergral accurate to tol using Richardson extrapolation 
let integralNonFunctional tol f a b =
    let richardsonRows = List<float array>()
    let mutable h = (b-a)*0.5
    richardsonRows.Add ([|(f a+f b)*h|])
    let rec run () =
        let lastRow = richardsonRows.[richardsonRows.Count-1]
        let row = Array.zeroCreate (lastRow.Length+1)
        row.[0] <- lastRow.[0]*0.5+h*Seq.sumBy f {a+h..h*2.0..b}
        let mutable pow4 = 4.0
        for i = 0 to lastRow.Length-1 do
            row.[i+1] <- (row.[i]*pow4-lastRow.[i])/(pow4-1.0)
            pow4 <- pow4*4.0
        if richardsonStoppingCriteriaNF tol richardsonRows then lastRow.[lastRow.Length-1]
        else
            richardsonRows.Add row
            h<-h*0.5
            run()
    run()
    
(**
## Implemetation improvements

Something
*)
        
/// Richardson extrapolation for sequence with an error term in even powers of h and halves on each sequence iteration.  
let richardsonExtrapolationEven s =
    let row prev n0 = Seq.scan (fun (njh,pow4) nj2h -> (pow4*njh-nj2h)/(pow4-1.0),pow4*4.0) (n0,4.0) prev |> Seq.map fst |> Seq.cache
    Seq.scan row Seq.empty s |> Seq.cache |> Seq.tail

let richardsonStoppingCriteria tol s = Seq.map Seq.last s |> Seq.triplewise |> Seq.find (fun (a,b,c) -> abs(a-b)<=tol && abs(b-c)<=tol) |> trd

let derivativeEstimateSeq f x h0 = Seq.unfoldInf ((*)0.5) h0 |> Seq.map (derivativeEstimate f x)

/// Derivative accurate to tol using Richardson extrapolation
let derivative tol h0 f x = derivativeEstimateSeq f x h0 |> richardsonExtrapolationEven |> richardsonStoppingCriteria tol

let integralEstimateSeq f a b =
    let h0 = (b-a)*0.5
    let i0 = (f a+f b)*h0            
    Seq.unfoldInf ((*)0.5) h0 |> Seq.scan (fun i h -> i*0.5 + h*Seq.sumBy f {a+h..h*2.0..b}) i0

/// Intergral accurate to tol using Richardson extrapolation
let integral tol f a b = if a=b then 0.0 else integralEstimateSeq f a b |> richardsonExtrapolationEven |> richardsonStoppingCriteria tol

(**
## This is a section
*)