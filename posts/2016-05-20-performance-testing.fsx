(**
\---
layout: post
title: "Modularity from Lazy Evaluation - Performance Testing"
tags: [modularity,higher-order,lazy evaluation]
description: ""
keywords: f#, fsharp, functional, higher-order functions, lazy evaluation, modularity
\---
*)

(*** hide ***)

namespace Main

module NumericLiteralG =
    let inline FromZero() = LanguagePrimitives.GenericZero
    let inline FromOne() = LanguagePrimitives.GenericOne
    
open System
open System.Diagnostics
    
[<AutoOpen>]
module Functions =
    let sqr x = x*x
    
(**
This is the second example of how higher-order functions and lazy evaluation can reduce complexity and lead to more modular software.

## Background

When performance testing code a number of runs have to be performed so a more stable average can be compared.
The number of runs is usually an input to the performance testing code and chosen arbitrarily.

This post will cover how we can use statistical tests to remove the need for this input.
This simplifies performance testing and makes the results more robust and useful.

## Statistics

The [standard error](https://en.wikipedia.org/wiki/Standard_error) of the sample mean is given by

$$$
SE_{\bar{x}} = \frac{s}{\sqrt{n}}

[Welch's t-test](https://en.wikipedia.org/wiki/Welch%27s_t-test) is given by

$$$
t = \frac{\bar{x}_1-\bar{x}_2}{\sqrt{f_1+f_2}}

$$$
df = \frac{\left(f_1+f_2\right)^2}{\frac{f_1^2}{n_1-1}+\frac{f_2^2}{n_2-1}}

$$$
f_i = \frac{s_i^2}{n_i}

where $n$ is the sample size, $\bar{x}$ is the sample mean and $s^2$ is the sample variance.

Lazy evaluation can be used to produce an [online](https://en.wikipedia.org/wiki/Online_algorithm) sample statistics sequence.
This sequence can then be iterated until a given precision or confidence level is satisfied.

## Statistics code
*)

type SampleStatistics = {N:int;Mean:float;Variance:float}
                        member x.StandardDeviation = sqrt x.Variance
                        member x.MeanStandardError = sqrt(x.Variance/float x.N)
                        static member (*)(f,s) = {s with Mean=s.Mean*f;Variance=s.Variance*f*f}

type StudentTStatistic = {T:float;DF:int}

module Statistics =
    let inline sampleStatistics n s =
        let calc (i,t,t2) =
            let m = float t/float i
            let v = (float t2-m*float t)/float(i-1)
            {N=i*n;Mean=m/float n;Variance=v/float n}
        Seq.scan (fun (i,t,t2) x -> i+1,t+x,t2+x*x) (0,0G,0G) s |> Seq.skip 3 |> Seq.map calc

    let twoSampleTStatistic x1 x2 =
        let f1 = x1.Variance/float x1.N
        let f2 = x2.Variance/float x2.N
        let df = if f1=0.0&&f2=0.0 then 1.0 else sqr(f1+f2)/(f1*f1/float(x1.N-1)+f2*f2/(float(x2.N-1)))
        {T=(x1.Mean-x2.Mean)/sqrt(f1+f2);DF=int df}
 
    //T.INV(0.999,i) ie to 0.1% level
    let tInv = [|318.309;22.327;10.215;7.173;5.893;5.208;4.785;4.501;4.297;4.144;4.025;3.93;
                 3.852;3.787;3.733;3.686;3.646;3.61;3.579;3.552;3.527;3.505;3.485;3.467;3.45;
                 3.435;3.421;3.408;3.396;3.385;3.375;3.365;3.356;3.348;3.34;3.333;3.326;3.319;
                 3.313;3.307;3.301;3.296;3.291;3.286;3.281;3.277;3.273;3.269;3.265;3.261;3.258;
                 3.255;3.251;3.248;3.245;3.242;3.239;3.237;3.234;3.232;3.229;3.227;3.225;3.223;
                 3.22;3.218;3.216;3.214;3.213;3.211;3.209;3.207;3.206;3.204;3.202;3.201;3.199;
                 3.198;3.197;3.195;3.194;3.193;3.191;3.19;3.189;3.188;3.187;3.185;3.184;3.183;
                 3.182;3.181;3.18;3.179;3.178;3.177;3.176;3.175;3.175;3.174|]
    let maxDF = 10000

    let twoSampleTTest s1 s2 =
        let t = twoSampleTStatistic s1 s2
        if t.DF>maxDF then Some 0
        elif abs t.T>=Array.get tInv (t.DF-1 |> min (Array.length tInv-1)) then Some(sign t.T)  
        else None

(**

## Performance code
*)

module Performance =
    open Statistics

    let inline private findIterationTarget metric metricTarget f =
        let rec findN n =
            let item = metric n f
            if (item<<<3)>=metricTarget then n*int metricTarget/int item+1
            else findN (n*10)
        findN 1

    let inline private meanMeasure (metric,metricTarget) relativeStandardError f =
        let n = findIterationTarget metric metricTarget f
        Seq.initInfinite (fun _ -> metric n f) |> sampleStatistics n
        |> Seq.where (fun s -> s.MeanStandardError<=relativeStandardError*s.Mean)
        |> Seq.head

    let inline private compareMeasure (metric,metricTarget) (f1:unit->'a) (f2:unit->'a) =
        if f1()<>f2() then failwith "function results are not the same"
        let n = findIterationTarget metric metricTarget f2
        let stats f = Seq.initInfinite (fun _ -> metric n f) |> sampleStatistics n
        Seq.map2 twoSampleTTest (stats f1) (stats f2) |> Seq.pick id

    let private measureMetric startMetric endMetric n f =
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let s = startMetric()
        let rec loop n = if n>0 then f() |> ignore; loop (n-1)
        loop n
        endMetric s

    let private timeMetric n f = 
        measureMetric Stopwatch.StartNew (fun sw -> sw.ElapsedTicks) n f

    let private memoryMetric n f =
        let inline startMetric() =
            let succeeded = GC.TryStartNoGCRegion(1L<<<22)
            if not succeeded then failwith "TryStartNoGCRegion failed"
            GC.GetTotalMemory false
        let inline endMetric s =
            let t = GC.GetTotalMemory false - s
            GC.EndNoGCRegion()
            t
        measureMetric startMetric endMetric n f

    let private garbageMetric n f =
        let gcCount() = GC.CollectionCount 0 + GC.CollectionCount 1 + GC.CollectionCount 2
        measureMetric gcCount (fun s -> gcCount() - s) n f

    let private oneMillisecond = Stopwatch.Frequency/1000L
    let private oneKB = 1024L
    let private timeMeasure = timeMetric, oneMillisecond
    let private memoryMeasure = memoryMetric, oneKB
    let private garbageMeasure = garbageMetric, 10

    let private relativeStandardError = 0.01

    let timeStatistics f = meanMeasure timeMeasure relativeStandardError f
                           |> (*) (1.0/float Stopwatch.Frequency)
    let memoryStatistics f = meanMeasure memoryMeasure relativeStandardError f
    let gcCountStatistics f = meanMeasure garbageMeasure relativeStandardError f
    
    let timeCompare f1 f2 = compareMeasure timeMeasure f1 f2
    let memoryCompare f1 f2 = compareMeasure memoryMeasure f1 f2
    let gcCountCompare f1 f2 = compareMeasure garbageMeasure f1 f2