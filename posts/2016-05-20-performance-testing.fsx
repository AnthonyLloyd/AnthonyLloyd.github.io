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

When performance testing code a number of iterations have to be performed so a more stable average can be compared.
The number of iterations is usually an input to the performance testing code and chosen arbitrarily.

This post will cover how we can use statistical tests to remove the need for this input.
This simplifies performance testing and makes the results more robust and useful.

## Statistics

The [standard error](https://en.wikipedia.org/wiki/Standard_error) of a sample mean is given by

$$$
SE_{\bar{x}} = \frac{s}{\sqrt{n}}

[Welch's t-test](https://en.wikipedia.org/wiki/Welch%27s_t-test) for comparing the mean of two samples is given by

$$$
t = \frac{\bar{x}_1-\bar{x}_2}{\sqrt{f_1+f_2}}

$$$
df = \frac{\left(f_1+f_2\right)^2}{\frac{f_1^2}{n_1-1}+\frac{f_2^2}{n_2-1}}

$$$
f_i = \frac{s_i^2}{n_i}

where $n$ is the sample size, $\bar{x}$ is the sample mean, $s^2$ is the sample variance, $t$ is Welch's t statistic, and $df$ is the degrees of freedom of the statistic.

Welch's t statistic can then be compared using the inverse Student's t-distribution for a given confidence interval to test if sample means are different and which one is greater. 

Lazy evaluation can be used to produce an [online](https://en.wikipedia.org/wiki/Online_algorithm) sample statistics sequence.
A statistics sequence can then be iterated until a given mean standard error level of accuracy is achieved.
Two sample statistics sequences can be iterated and compared to decide if one mean is larger than the other.

## Statistics code
*)
type SampleStatistics = {N:int;Mean:float;Variance:float}
                        member s.StandardDeviation = sqrt s.Variance
                        member s.MeanStandardError = sqrt(s.Variance/float s.N)

type WelchStatistic = {T:float;DF:int}

module Statistics =
    /// Online statistics sequence for a given sample sequence.
    let inline sampleStatistics s =
        let calc (n,t,t2) =
            let m = float t/float n
            {N=n;Mean=m;Variance=(float t2-m*float t)/float(n-1)}
        Seq.scan (fun (n,t,t2) x -> n+1,t+x,t2+x*x) (0,0G,0G) s |> Seq.skip 3 |> Seq.map calc

    /// Scale the statistics for the given underlying random variable change of scale.
    let scale f s = {s with Mean=s.Mean*f;Variance=s.Variance*sqr f}

    /// Single iteration statistics for the given iteration count and sum statistics.
    let singleIteration ic s = {N=s.N*ic;Mean=s.Mean/float ic;Variance=s.Variance/float ic}

    /// Student's t-distribution inverse for the 0.1% confidence level by degrees of freedom.
    let private tInv = 
        [|636.6;31.60;12.92;8.610;6.869;5.959;5.408;5.041;4.781;4.587;4.437;4.318;4.221;
          4.140;4.073;4.015;3.965;3.922;3.883;3.850;3.819;3.792;3.768;3.745;3.725;3.707;
          3.690;3.674;3.659;3.646;3.633;3.622;3.611;3.601;3.591;3.582;3.574;3.566;3.558;
          3.551;3.544;3.538;3.532;3.526;3.520;3.515;3.510;3.505;3.500;3.496;3.492;3.488;
          3.484;3.480;3.476;3.473;3.470;3.466;3.463;3.460;3.457;3.454;3.452;3.449;3.447;
          3.444;3.442;3.439;3.437;3.435;3.433;3.431;3.429;3.427;3.425;3.423;3.421;3.420;
          3.418;3.416;3.415;3.413;3.412;3.410;3.409;3.407;3.406;3.405;3.403;3.402;3.401;
          3.399;3.398;3.397;3.396;3.395;3.394;3.393;3.392;3.390|]

    /// Welch's t-test statistic for two given sample statistics.
    let welchStatistic s1 s2 =
        let f1 = s1.Variance/float s1.N
        let f2 = s2.Variance/float s2.N
        {
            T = (s1.Mean-s2.Mean)/sqrt(f1+f2)
            DF= if f1=0.0 && f2=0.0 then 1
                else sqr(f1+f2)/(sqr f1/float(s1.N-1)+sqr f2/float(s2.N-1)) |> int
        }

    /// Welch's t-test for a given Welch statistic using a 0.1% confidence level.
    let welchTest w =
        if abs w.T < Array.get tInv (min w.DF (Array.length tInv) - 1) then 0 else sign w.T
(*** hide ***)
open Statistics
(**
## Performance


## Performance code
*)
module Performance =
    let inline private targetIterationCount metric metricTarget f =
        let rec find n =
            let item = metric n f
            if (item<<<3)>=metricTarget then n*int metricTarget/int item+1
            else find (n*10)
        find 1

    let inline private measureStatistics (metric,metricTarget) relativeError f =
        let ic = targetIterationCount metric metricTarget f
        Seq.initInfinite (fun _ -> metric ic f) |> sampleStatistics
        |> Seq.map (singleIteration ic)
        |> Seq.find (fun s -> s.MeanStandardError<=relativeError*s.Mean)

    let inline private measureCompare (metric,metricTarget) f1 f2 =
        if f1()<>f2() then failwith "function results are not the same"
        let ic = targetIterationCount metric metricTarget f2
        let stats f = Seq.initInfinite (fun _ -> metric ic f)
                      |> sampleStatistics
                      |> Seq.map (singleIteration ic)
        Seq.map2 welchStatistic (stats f1) (stats f2)
        |> Seq.pick (fun w ->
            let maximumDF = 10000
            if w.DF>maximumDF then Some 0
            else
                let c = welchTest w
                if c=0 then None else Some c)

    let private measureMetric startMetric endMetric ic f =
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let s = startMetric()
        let rec loop i = if i>0 then f() |> ignore; loop (i-1)
        loop ic
        endMetric s

    let private timeMetric ic f = 
        measureMetric Stopwatch.StartNew (fun sw -> sw.ElapsedTicks) ic f

    let private memoryMetric ic f =
        let inline startMetric() =
            if GC.TryStartNoGCRegion(1L<<<22) |> not then failwith "TryStartNoGCRegion"
            GC.GetTotalMemory false
        let inline endMetric s =
            let t = GC.GetTotalMemory false - s
            GC.EndNoGCRegion()
            t
        measureMetric startMetric endMetric ic f

    let private garbageMetric ic f =
        let count() = GC.CollectionCount 0 + GC.CollectionCount 1 + GC.CollectionCount 2
        measureMetric count (fun s -> count() - s) ic f

    let private oneMillisecond = Stopwatch.Frequency/1000L
    let private timeMeasure = timeMetric, oneMillisecond
    let private memoryMeasure = memoryMetric, 1024L //1KB
    let private garbageMeasure = garbageMetric, 10

    /// Time statistics for a given function accurate to a mean standard error of 1%.
    let timeStatistics f =
        measureStatistics timeMeasure 0.01 f |> scale (1.0/float Stopwatch.Frequency)
    /// Memory statistics for a given function accurate to a mean standard error of 1%.
    let memoryStatistics f = measureStatistics memoryMeasure 0.01 f
    /// GC count statistics for a given function accurate to a mean standard error of 1%.
    let gcCountStatistics f = measureStatistics garbageMeasure 0.01 f
    
    /// Time comparison for two given functions.
    let timeCompare f1 f2 = measureCompare timeMeasure f1 f2
    /// Memory comparison for two given functions.
    let memoryCompare f1 f2 = measureCompare memoryMeasure f1 f2
    /// GC count comparison for two given functions.
    let gcCountCompare f1 f2 = measureCompare garbageMeasure f1 f2
(**
## Conclusion

Very simple API

Statistics function give an overview of a function.

Compare can be used in unit tests because it independent of machine.

*)