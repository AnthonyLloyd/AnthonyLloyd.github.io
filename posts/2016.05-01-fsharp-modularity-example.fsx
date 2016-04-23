(**
\---
layout: post
title: "F# Modularity Example"
tags: [modularity,higher-order,lazy evaluation]
description: "An F# example of how higher-order functions and lazy evaluation can reduce complexity and lead to more modular reusable software"
keywords: f#, fsharp, functional, higher-order functions, lazy evaluation, modularity
\---
*)

(*** hide ***)
module Main

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
Some test hello.
Hello more.

Hello again again.
*)

/// Derivative approximation using 2 function evaluations at $x \pm h$.    
/// $$$
/// f'(x) = \frac{1}{2h}\left(f(x+h)-f(x-h)\right)
let derivative2 f x h = 0.5*(f(x+h)-f(x-h))/h

/// Richardson extrapolation for sequence with an error term in even powers of $h$ and halves on each sequence iteration.  
let internal richardsonExtrapolationEven s =
    let row prev n0 = Seq.scan (fun (njh,pow4) nj2h -> (pow4*njh-nj2h)/(pow4-1.0),pow4*4.0) (n0,4.0) prev |> Seq.map fst |> Seq.cache
    Seq.scan row Seq.empty s |> Seq.cache |> Seq.tail

let internal richardsonStoppingCriteria acc s = Seq.map Seq.last s |> Seq.triplewise |> Seq.find (fun (a,b,c) -> abs(a-b)<=acc && abs(b-c)<=acc) |> trd

/// Derivative accurate to tol using Richardson extrapolation
let derivative acc h f x = 
    let inline derivativeSeq f x h = Seq.unfoldInf ((*)0.5) h |> Seq.map (derivative2 f x)
    derivativeSeq f x h |> richardsonExtrapolationEven |> richardsonStoppingCriteria acc

/// Intergral accurate to tol using Richardson extrapolation
let integral acc f a b =
    let inline integralSeq f a b =
        let h0 = (b-a)*0.5
        let i0 = (f a+f b)*h0            
        Seq.unfoldInf ((*)0.5) h0 |> Seq.scan (fun i h -> i*0.5 + h*Seq.sumBy f {a+h..h*2.0..b}) i0
    if a=b then 0.0 else integralSeq f a b |> richardsonExtrapolationEven |> richardsonStoppingCriteria acc

(**
## This is a section
*)

// Squares a number: int -> int
let square x = x * x