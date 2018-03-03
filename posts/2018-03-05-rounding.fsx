(**
\---
layout: post
title: "Rounding"
tags: [rounding,testing,property-based]
description: "Rounding"
keywords: f#, rounding, testing, property-based
\---
*)

(*** hide ***)
module Rounding

open System
open Expecto
open FsCheck

(**
Steffen Forkmann recently posted a [blog](http://www.navision-blog.de/blog/2018/02/21/rounding-is-a-bitch/) about incorrect rounding in a twitter poll, and how coding a rounding strategy is a hard problem.

In these type of problems it is important to look to what properties the algorithm should hold.
Property-based testing is the ideal tool for this.

I have seen this problem in order management systems where orders for a number of shares are to be allocated across a number of funds.
The buy and sell orders have a number of partial fills, but in the end everything needs to add up and be consistent across the funds.

The key property required for a fair rounding algorithm is that rounded numbers increase with the weights.
It doesn't make sense for a lower weight to have a greater rounded value.
Symmetry in the results for negative weights and negative integer to be round are also important but can easily be achieved by mapping to the positive values.

Forkmann proposes adjusting the largest weight, but in general this can only work when the rounding needs a positive adjustment due to the increasing with weight property.
For negative adjustments the smallest weight would need to be adjusted.

## Error minimisation algorithm
*)
/// Distribute integer n over an array of weights
let distribute n weights =
    let wt = Array.sum weights
    if Array.isEmpty weights
        || Seq.maxBy abs weights * float(abs n+1) |> abs
           >= abs wt * float Int32.MaxValue then None
    else
        let inline round f = if f<0.0 then f-0.5 else f+0.5
        let ns = Array.map ((*)(float n / wt) >> round >> int) weights
        let inline absError wi ni d =
            let wc = 0.5 * float d * wt / float n
            let wni = float ni * wt / float n
            if wc > 0.0 then min (wni-wi) 0.0 + wc
            else min (wi-wni) 0.0 - wc
        let d = n - Array.sum ns
        for __ = 1 to abs d do
            let _,_,_,i =
                Seq.mapi2 (fun i wi ni ->
                    let absErr = absError wi ni (sign d)
                    let relErr = absErr / abs wi
                    let weight = if wt > 0.0 then -wi else wi
                    absErr, relErr, weight, i
                ) weights ns |> Seq.min
            ns.[i] <- ns.[i] + sign d
        Some ns
(**
*)
(*** hide ***)
module Gen =
    type RationalFloat = RationalFloat of float
    let rationalFloat =
        let fraction a b c = float a + float b / (abs (float c) + 1.0)
        Gen.map3 fraction Arb.generate Arb.generate Arb.generate
        |> Arb.fromGen
        |> Arb.convert RationalFloat (fun (RationalFloat f) -> f)
    let rationalFloats (w:_ NonEmptyArray) =
        Array.map (fun (RationalFloat f) -> f) w.Get
let private config = {
    FsCheckConfig.defaultConfig with
        arbitrary = typeof<Gen.RationalFloat>.DeclaringType::FsCheckConfig.defaultConfig.arbitrary
}
let testProp name = testPropertyWithConfig config name
let ptestProp name = ptestPropertyWithConfig config name
let ftestProp stdgen name = ftestPropertyWithConfig stdgen config name
(**
## Tests
*)
let roundingTests =
    testList "rounding" [
        test "empty" {
            let r = distribute 1 [||]
            Expect.equal r None "empty"
        }
        test "n zero" {
            let r = distribute 0 [|406.0;348.0;246.0;0.0|]
            Expect.equal r (Some [|0;0;0;0|]) "zero"
        }
        test "twitter" {
            let r = distribute 100 [|406.0;348.0;246.0;0.0|]
            Expect.equal r (Some [|40;35;25;0|]) "40 etc"
        }
        test "twitter n negative" {
            let r = distribute -100 [|406.0;348.0;246.0;0.0|]
            Expect.equal r (Some [|-40;-35;-25;0|]) "-40 etc"
        }
        test "twitter weights negative" {
            let r = distribute 100 [|-406.0;-348.0;-246.0;-0.0|]
            Expect.equal r (Some [|40;35;25;0|]) "40 etc"
        }
        test "twitter both negative" {
            let r = distribute -100 [|-406.0;-348.0;-246.0;-0.0|]
            Expect.equal r (Some [|-40;-35;-25;0|]) "-40 etc"
        }
        test "twitter tricky" {
            let r = distribute 100 [|404.0;397.0;47.0;47.0;47.0;58.0|]
            Expect.equal r (Some [|40;39;5;5;5;6|]) "o no"
        }
        test "negative example" {
            let r1 = distribute 42 [|1.5;1.0;39.5;-1.0;1.0|]
            Expect.equal r1 (Some [|2;1;39;-1;1|]) "2 etc"
            let r2 = distribute -42 [|1.5;1.0;39.5;-1.0;1.0|]
            Expect.equal r2 (Some [|-2;-1;-39;1;-1|]) "-2 etc"
        }
        testProp "ni total correctly" (fun n ws ->
            Gen.rationalFloats ws
            |> distribute n
            |> Option.iter (fun ns -> Expect.equal (Array.sum ns) n "sum ns = n")
        )
        testProp "negative n returns opposite of positive n" (fun n ws ->
            let ws = Gen.rationalFloats ws
            let r1 = distribute -n ws |> Option.map (Array.map (~-))
            let r2 = distribute n ws
            Expect.equal r1 r2 "r1 = r2"
        )
        testProp "increase with weight" (fun n ws ->
            let ws = Gen.rationalFloats ws
            let d = if Seq.sum ws > 0.0 <> (n>0) then -1 else 1
            distribute n ws
            |> Option.iter (
                   Seq.map ((*)d)
                >> Seq.zip ws
                >> Seq.sort
                >> Seq.pairwise
                >> Seq.iter (fun (ni1,ni2) ->
                    Expect.isLessThanOrEqual ni1 ni2 "ni1 <= ni2")
            )
        )
    ]

(**
## Conclusion

*)