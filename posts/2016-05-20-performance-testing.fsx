(**
\---
layout: post
title: "Modularity from Lazy Evaluation - Performance Testing"
tags: [modularity,higher-order,lazy evaluation,performance,testing]
description: ""
keywords: f#, fsharp, functional, higher-order functions, lazy evaluation, modularity, performance, testing
\---
*)
(*** hide ***)
namespace Main

open System
open System.Diagnostics
    
[<AutoOpen>]
module Functions =
    let inline sqr x = x*x
    
(**
This is the second example of how higher-order functions and lazy evaluation can reduce complexity and lead to more modular software.

## Background

To performance test code a number of iterations have to be performed so a more stable average can be compared.
The number of iterations is usually an input and chosen arbitrarily.

This post covers how statistical tests can be used to remove the need for this input.
This simplifies performance testing and makes the results more robust and useful.

## Statistics

Lazy evaluation can be used to produce an [online](https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Online_algorithm) sample statistics sequence.

The [standard error](https://en.wikipedia.org/wiki/Standard_error) of a sample mean is given by

$$$
SE_{\bar{x}} = \frac{s}{\sqrt{n}}

The statistics sequence can be iterated until a given mean standard error level of accuracy is achieved.

[Welch's t-test](https://en.wikipedia.org/wiki/Welch%27s_t-test) for comparing the mean of two samples is given by

$$$
t = \frac{\bar{x}_1-\bar{x}_2}{\sqrt{f_1+f_2}}

$$$
df = \frac{\left(f_1+f_2\right)^2}{\frac{f_1^2}{n_1-1}+\frac{f_2^2}{n_2-1}}

$$$
f_i = \frac{s_i^2}{n_i}

where $n$ is the sample size, $\bar{x}$ is the sample mean, $s^2$ is the sample variance, $t$ is Welch's t statistic, and $df$ is the degrees of freedom of the statistic.

Welch's t statistic can be compared to the inverse Student's t-distribution for a given confidence level to test if the sample means are different. 

Two sample statistics sequences can be iterated together and compared to decide if one mean is larger than the other.

## Statistics code
*)
type SampleStatistics = {N:int;Mean:float;Variance:float}
                        member s.StandardDeviation = sqrt s.Variance
                        member s.MeanStandardError = sqrt(s.Variance/float s.N)

type WelchStatistic = {T:float;DF:int}

module Statistics =
    /// Online statistics sequence for a given sample sequence.
    let inline sampleStatistics s =
        let calc (n,m,s) x =
            let m'=m+(x-m)/float(n+1)
            n+1,m',s+(x-m)*(x-m')
        Seq.map float s |> Seq.scan calc (0,0.0,0.0) |> Seq.skip 3
        |> Seq.map (fun (n,m,s) -> {N=n;Mean=m;Variance=s/float(n-1)})

    /// Scale the statistics for a given underlying random variable change of scale.
    let scale f s = {s with Mean=s.Mean*f;Variance=s.Variance*sqr f}

    /// Single iteration statistics for a given iteration count and total statistics.
    let singleIteration ic s = {N=s.N*ic;Mean=s.Mean/float ic;Variance=s.Variance/float ic}

    /// Student's t-distribution inverse for 0.1% confidence level by degrees of freedom.
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

    /// Welch's t-test for a given Welch statistic to a confidence level of 0.1%.
    let welchTest w =
        if abs w.T < Array.get tInv (min w.DF (Array.length tInv) - 1) then 0 else sign w.T
(*** hide ***)
open Statistics
(**
## Performance testing

Three performance metrics will be created: time, memory allocated, and garbage collections.

Each of these will be repeated until at least the metric target is reached to ensure it is reliably measuring the metric and not any framework overhead.

Statistics functions will be created for each of the metrics to measure a function accurate to a mean standard error of 1%.

Compare functions will be created for each of the metrics to compare two functions to a confidence level of 0.1%.
The functions will be considered equal after 10,000 degrees of freedom have past without a positive test result.

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
        
    /// Create and iterate a statistics sequence for a metric until the given accuracy.
    let inline private measureStatistics (metric,metricTarget) relativeError f =
        let ic = targetIterationCount metric metricTarget f
        Seq.initInfinite (fun _ -> metric ic f) |> sampleStatistics
        |> Seq.map (singleIteration ic)
        |> Seq.find (fun s -> s.MeanStandardError<=relativeError*s.Mean)

    /// Create and iterate two statistics sequences until the metric means can be compared.
    let inline private measureCompare (metric,metricTarget) f1 f2 =
        if f1()<>f2() then failwith "function results are not the same"
        let ic = targetIterationCount metric metricTarget f2
        let stats f = Seq.initInfinite (fun _ -> metric ic f)
                      |> sampleStatistics
                      |> Seq.map (singleIteration ic)
        Seq.map2 welchStatistic (stats f1) (stats f2)
        |> Seq.pick (fun w ->
            let maxDF = 10000
            if w.DF>maxDF then Some 0 else match welchTest w with |0->None |c->Some c)

    /// Measure the given function for the iteration count using the start and end metric.
    let private measureMetric startMetric endMetric ic f =
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let s = startMetric()
        let rec loop i = if i>0 then f() |> ignore; loop (i-1)
        loop ic
        endMetric s
    
    /// Measure the time metric for the given function and iteration count.
    let private timeMetric ic f = 
        measureMetric Stopwatch.StartNew (fun sw -> sw.ElapsedTicks) ic f
        
    /// Measure the memory metric for the given function and iteration count.
    let private memoryMetric ic f =
        let inline startMetric() =
            if GC.TryStartNoGCRegion(1L<<<22) |> not then failwith "TryStartNoGCRegion"
            GC.GetTotalMemory false
        let inline endMetric s =
            let t = GC.GetTotalMemory false - s
            GC.EndNoGCRegion()
            t
        measureMetric startMetric endMetric ic f

    /// Measure the garbage collection metric for the given function and iteration count.
    let private garbageMetric ic f =
        let count() = GC.CollectionCount 0 + GC.CollectionCount 1 + GC.CollectionCount 2
        measureMetric count (fun s -> count() - s) ic f

    /// Measure definitions which are a metric together with a metric target.
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
    
    /// Time comparison for two given functions to a confidence level of 0.1%.
    let timeCompare f1 f2 = measureCompare timeMeasure f1 f2
    /// Memory comparison for two given functions to a confidence level of 0.1%.
    let memoryCompare f1 f2 = measureCompare memoryMeasure f1 f2
    /// GC count comparison for two given functions to a confidence level of 0.1%.
    let gcCountCompare f1 f2 = measureCompare garbageMeasure f1 f2
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