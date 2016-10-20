(**
\---
layout: post
title: "Get MAD with Outliers with an Improved Median Function"
tags: [MAD,median,outlier,statistics]
description: ""
keywords: median absolute deviation, MAD, median, outlier, statistics
\---
*)
(*** hide ***)
#r @"..\packages\MathNet.Numerics\lib\net40\MathNet.Numerics.dll"
#r @"..\packages\xunit.runner.visualstudio.2.1\build\net20\..\_common\xunit.abstractions.dll"
#r @"..\packages\xunit.assert\lib\portable-net45+win8+wp8+wpa81\xunit.assert.dll"
#r @"..\packages\xunit.extensibility.core\lib\portable-net45+win8+wp8+wpa81\xunit.core.dll"
#r @"..\packages\xunit.extensibility.execution\lib\net45\xunit.execution.desktop.dll"
#r @"..\packages\FsCheck\lib\net45\FsCheck.dll"
#r @"..\FsCheck.Xunit\lib\net45\FsCheck.Xunit.dll"
namespace Main

open System
open System.Diagnostics
open Xunit
open FsCheck

type PropertyAttribute = FactAttribute

[<AutoOpen>]
module Functions =
    let inline sqr x = x*x

    type MinValueGen = MinValueGen with
        static member inline (=>) (_:MinValueGen,_:int) = Int32.MinValue
        static member inline (=>) (_:MinValueGen,_:int64) = Int64.MinValue
        static member inline (=>) (_:MinValueGen,_:float) = Double.MinValue
        static member inline (=>) (_:MinValueGen,_:MinValueGen) = failwith "MinValueGen"
    /// Returns the minimum value for a generic type.
    let inline minValue() : 'a = MinValueGen => LanguagePrimitives.GenericZero<'a>

    type MaxValueGen = MaxValueGen with
        static member inline (=>) (_:MaxValueGen,_:int) = Int32.MaxValue
        static member inline (=>) (_:MaxValueGen,_:int64) = Int64.MaxValue
        static member inline (=>) (_:MaxValueGen,_:float) = Double.MaxValue
        static member inline (=>) (_:MaxValueGen,_:MaxValueGen) = failwith "MaxValueGen"
    /// Returns the maximum value for a generic type.
    let inline maxValue() : 'a = MaxValueGen => LanguagePrimitives.GenericZero<'a>

    type HalfPositiveGen = HalfPositiveGen with
        static member inline (=>) (_:HalfPositiveGen,x:int) = x>>>1
        static member inline (=>) (_:HalfPositiveGen,x:int64) = x>>>1
        static member inline (=>) (_:HalfPositiveGen,x:float) = x*0.5
        static member inline (=>) (_:HalfPositiveGen,_:HalfPositiveGen) = failwith "HalfValueGen"
    /// Returns half of the positive generic input value.
    let inline halfPositive(x:'a) : 'a = HalfPositiveGen => x

    /// Returns the middle point of two ordered input values.
    let inline middleOrdered a b =
        a+halfPositive(b-a)
    /// Returns the middle point of two input values.
    let inline middle a b =
        if a<=b then middleOrdered a b 
        else middleOrdered b a
    
type SampleStatistics = {N:int;Mean:float;Variance:float}
                        member s.StandardDeviation = sqrt s.Variance
                        member s.MeanStandardError = sqrt(s.Variance/float s.N)

type WelchStatistic = {T:float;DF:int}

module StatisticsPerf =
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

open StatisticsPerf

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
        if f1 id<>f2 id then failwith "function results are not the same"
        let ic = targetIterationCount metric metricTarget f2
        let stats f = Seq.initInfinite (fun _ -> metric ic f)
                      |> sampleStatistics
                      |> Seq.map (singleIteration ic)
        Seq.map2 welchStatistic (stats f1) (stats f2)
        |> Seq.pick (fun w ->
            let maxDF = 10000
            if w.DF>maxDF then Some 0 else match welchTest w with |0->None |c->Some c)

    /// Measure the given function for the iteration count using the start and end metric.
    let inline private measureMetric startMetric endMetric ic f =
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
        let mutable total = LanguagePrimitives.GenericZero
        let measurer toMeasure =
            fun args ->
                let s = startMetric()
                let ret = toMeasure args
                let m = endMetric s
                total<-total+m
                ret
        let rec loop i = if i>0 then f measurer |> ignore; loop (i-1)
        loop ic
        total
    
    /// Measure the time metric for the given function and iteration count.
    let private timeMetric ic f = 
        measureMetric Stopwatch.GetTimestamp (fun t -> Stopwatch.GetTimestamp()-t) ic f
        
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
        measureMetric count (fun s -> count()-s) ic f

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
This post presents a more robust method of detecting outliers in sample data than commonly used.
The method is based on the median and an optimised F# median function is provided. 

## Background

[Researchers](https://www.researchgate.net/publication/256752600_Detecting_outliers_Do_not_use_standard_deviation_around_the_mean_use_absolute_deviation_around_the_median) most commonly use standard deviation around the mean to detect outliers.
This is a problem because the mean and standard deviation give greater weight to extreme data points.

The mean is the point that minimises the sum of the square deviations, whereas the median is the point that minimises the sum of the absolute deviations.
The median and [median absolute deviation](https://en.wikipedia.org/wiki/Median_absolute_deviation) give a more robust measure of statistical dispersion and are more resilient to outliers.   

When a politician says average wages are increasing be sure to check the median is being reported.

## Median Absolute Deviation

The [median](https://en.wikipedia.org/wiki/Median) is the value separating the higher half of the sample from the lower half of the sample.


The [median absolute deviation](https://en.wikipedia.org/wiki/Median_absolute_deviation) is defined as

$$$
\operatorname{MAD} = \operatorname{median}\left(\left|x_i - \operatorname{median}(x_i)\right|\right)

Outliers can be identified as points that are outside a fixed multiple of the median absolute deviation from the median. Recommended values for this multiple are 2.5 or 3.

## Median and MAD code

The following median function makes use of the [MODIFIND](http://dhost.info/zabrodskyvlada/algor.html) algorithm by Vladimir Zabrodsky.
It provides a 20-30% performance improvement over the [Quickselect](https://en.wikipedia.org/wiki/Quickselect) algorithm.

The inner array while loops allow equality which improves performance when there is duplication and ordering in the data.
The `selectInPlace` function has also been extended to optionally return the middle of the kth and the k+1 element.

*)
module Statistics =
    /// Returns the median of three input values.
    let inline median3 a b c =
        if a<=b then
            if b<=c then b
            elif c<=a then a
            else c
        else
            if a<=c then a
            elif c<=b then b
            else c
    /// Returns the minimum value of a subsection of an array.
    let inline minSub (a:_[]) lo hi =
        let mutable v = a.[lo]
        for i = lo+1 to hi do
            let nv = a.[i]
            if nv<v then v<-nv
        v
    /// Returns the maximum value of a subsection of an array.
    let inline maxSub (a:_[]) lo hi =
        let mutable v = a.[lo]
        for i = lo+1 to hi do
            let nv = a.[i]
            if nv>v then v<-nv
        v
    /// Returns the middle point of the two smallest values of a subsection of an array.
    let inline min2middleSub (a:_[]) lo hi =
        let mutable v0 = a.[lo]
        let mutable v1 = a.[lo+1]
        if v1<v0 then
            let tmp = v0
            v0<-v1
            v1<-tmp
        for i = lo+2 to hi do
            let nv = a.[i]
            if nv<v0 then
                v1<-v0
                v0<-nv
            elif nv<v1 then
                v1<-nv
        middleOrdered v0 v1
    /// Returns the middle point of the two largest values of a subsection of an array.
    let inline max2middleSub (a:_[]) lo hi =
        let mutable v0 = a.[lo]
        let mutable v1 = a.[lo+1]
        if v1>v0 then
            let tmp = v0
            v0<-v1
            v1<-tmp
        for i = lo+2 to hi do
            let nv = a.[i]
            if nv>v0 then
                v1<-v0
                v0<-nv
            elif nv>v1 then
                v1<-nv
        middleOrdered v1 v0
    /// Swap two elements in an array.
    let inline swap (a:_[]) i j =
        let temp = a.[i]
        a.[i]<-a.[j]
        a.[j]<-temp
    /// Swap two elements in an array if the first is larger than the second.
    let inline swapIf (a:_[]) i j =
        let ai = a.[i]
        let aj = a.[j]
        if ai>aj then
            a.[i]<-aj
            a.[j]<-ai
    /// Returns the kth smallest element in an array and optionally the middle with
    /// the next largest. Elements will be reordered in place and cannot be equal
    /// to the max or min value of the generic type.
    let inline selectInPlace (a:_[]) k middleNext =
        let rec outerLoop lo hi =
            swapIf a lo k
            swapIf a lo hi
            swapIf a k hi
            let pivot = a.[k]
            let resetLo = if a.[lo]=pivot then a.[lo]<-minValue(); true else false
            let resetHi = if a.[hi]=pivot then a.[hi]<-maxValue(); true else false
            let mutable i = lo+1
            let mutable j = hi-1
            while a.[i]<=pivot do i<-i+1
            while a.[j]>=pivot do j<-j-1
            while i<k && k<j do
                swap a i j
                i<-i+1
                j<-j-1
                while a.[i]<=pivot do i<-i+1
                while a.[j]>=pivot do j<-j-1
            if i<j then
                swap a i j
                if k<j then
                    i<-lo
                    j<-j-1
                    while a.[j]>=pivot do j<-j-1
                else
                    j<-hi
                    i<-i+1
                    while a.[i]<=pivot do i<-i+1
            else
                if k<j then i<-lo
                elif k>i then j<-hi
            if resetLo then a.[lo]<-pivot
            if resetHi then a.[hi]<-pivot
            if i>=j then if middleNext then if k+1=i then
                                                minSub a i hi |> middleOrdered pivot
                                            else pivot 
                         else pivot
            elif k=i then if middleNext then min2middleSub a i j else minSub a i j
            elif k=j then if middleNext then max2middleSub a i j else maxSub a i j
            else outerLoop i j
        outerLoop 0 (Array.length a-1)
    /// Returns the median of an array. Elements will be reordered in place and
    /// cannot be equal to the max or min value of the generic type.
    let inline medianInPlace (a:_[]) =
        match Array.length a-1 with
        | 0 -> a.[0]
        | 1 -> middle a.[0] a.[1]
        | 2 -> median3 a.[0] a.[1] a.[2]
        | last -> selectInPlace a (last/2) (last%2=1)
    /// Returns the median and median absolute deviation of an array.
    /// Elements cannot be equal to the max or min value of the generic type.
    let inline medianAndMAD (a:_[]) =
        let a = Array.copy a
        let median  = medianInPlace a
        for i = 0 to Array.length a-1 do
            a.[i]<-abs(a.[i]-median)
        median,medianInPlace a
(**

## Property and Performance testing

A simple FsCheck property test comparing the result with a full sort version ensures no mistakes have been made in the implementation.

The performance against a full sort and the Math.Net C# Quickselect implementation is compared for different degrees of duplication and sorting.

The performance testing library developed in a previous [post]({% post_url 2016-05-20-performance-testing %}) was used after extending it to allow sub function measurement.
This was run from the build script in 64-bit Release mode.


| Duplication |   Sorted   |  Current  |  MathNet  |  FullSort  |  1.000 =  |
|:-----------:|:----------:|:---------:|:---------:|:----------:|:---------:|
|    Low      |    No      |   1.000   |   1.369   |    6.582   |  0.1028s  |
|    Low      |    Part    |   1.000   |   1.350   |    9.269   |  0.0476s  |
|    Low      |    Yes     |   1.000   |   1.516   |   12.964   |  0.0106s  |
|    Medium   |    No      |   1.000   |   1.373   |    6.577   |  0.1018s  |
|    Medium   |    Part    |   1.000   |   1.397   |    9.471   |  0.0478s  |
|    Medium   |    Yes     |   1.000   |   1.840   |   17.519   |  0.0100s  |
|    High     |    No      |   1.000   |   1.341   |    4.745   |  0.1059s  |
|    High     |    Part    |   1.000   |   1.576   |    8.050   |  0.0526s  |
|    High     |    Yes     |   1.000   |   3.193   |   26.390   |  0.0087s  |
*)
module StatisticsTests =
    let inline medianQuickSelect (a:float[]) =
        MathNet.Numerics.Statistics.ArrayStatistics.MedianInplace a

    let inline medianFullSort a =
        Array.sortInPlace a
        let l = Array.length a
        if l%2=0 then
            let i = l/2
            let x = a.[i-1]
            let y = a.[i]
            x+(y-x)*0.5
        else a.[l/2]

    [<Property>]
    let MedianProp (x:int) (xs:int list) =
        let l = x::xs |> List.map float
        let m1 = List.toArray l |> Statistics.medianInPlace
        let m2 = List.toArray l |> medianFullSort 
        m1=m2

    type Duplication =
        | Low | Medium | High
        member i.ToInt = match i with | Low->500000000 | Medium->5000 | High->50
        override i.ToString() = match i with | Low->"Low" | Medium->"Medium" | High->"High"

    type Sorted =
        | No | Part | Yes
        override i.ToString() = match i with | No->"No" | Part->"Part" | Yes->"Yes"

    let list (duplication:Duplication) (sorted:Sorted) =
        let r = System.Random 123
        let next() = r.Next(0,duplication.ToInt) |> float
        Seq.init 5000 (fun i ->
            let l = List.init (i+1) (fun _ -> next())
            match sorted with
            | No -> l
            | Yes -> List.sort l
            | Part ->
                let a = List.sort l |> List.toArray
                let len = Array.length a
                Seq.iter (fun _ -> Statistics.swap a (r.Next len) (r.Next len)) {1..len/4}
                List.ofArray a
            )

    [<Fact>]
    let MedianPerfTest() =
        printfn
            "| Duplication |   Sorted   |  Current  |  MathNet  |  FullSort  |  1.000 =  |"
        printfn
            "|:-----------:|:----------:|:---------:|:---------:|:----------:|:---------:|"
        Seq.collect (fun d -> Seq.map (fun s -> (d,s),list d s) [No;Part;Yes])
            [Low;Medium;High]
        |> Seq.iter (fun ((d,s),lists) ->
            let timeStatistics f =
                Performance.timeStatistics
                    (fun timer -> Seq.iter (List.toArray >> timer f >> ignore) lists)
            let p1 = timeStatistics Statistics.medianInPlace
            let p2 = timeStatistics medianQuickSelect
            let p3 = timeStatistics medianFullSort
            printfn
                "|    %-6s   |    %-4s    |   1.000   |  %6.3f   |   %6.3f   |  %.4fs  |"
                (string d) (string s) (p2.Mean/p1.Mean) (p3.Mean/p1.Mean) p1.Mean
        )
(**
## Conclusion

The post provides optimised generic select, median and median absolute deviation functions. 

The performance results show a good improvement over Quickselect which is already an optimised algorithm.
The performance of the code is also more predictable due to its handling of duplication and partially sorted data.

The post demonstrates how simple property based testing and a performance testing library can be used together to optimise algorithms. 
*)