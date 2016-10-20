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
namespace Main

open System
open System.Diagnostics
    
[<AutoOpen>]
module Functions =
    type MinValueGen = MinValueGen with
        static member inline (=>) (_:MinValueGen,_:int) = Int32.MinValue
        static member inline (=>) (_:MinValueGen,_:int64) = Int64.MinValue
        static member inline (=>) (_:MinValueGen,_:float) = Double.MinValue
        static member inline (=>) (_:MinValueGen,_:MinValueGen) = failwith "MinValueGen"
    /// Returnd the minimum value for a generic type.
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
    /// Returns half of the generic input value.
    let inline halfPositive(x:'a) : 'a = HalfPositiveGen => x

    /// Returns the middle point of two ordered input values.
    let inline middleOrdered a b =
        a+halfPositive(b-a)
    /// Returns the middle point of two input values.
    let inline middle a b =
        if a<=b then middleOrdered a b 
        else middleOrdered b a
    
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

The following median function makes use the [MODIFIND](http://dhost.info/zabrodskyvlada/algor.html) algorithm by Vladimir Zabrodsky.
It provides a 20-30% performance improvement over the [Quickselect](https://en.wikipedia.org/wiki/Quickselect) algorithm.
In addition I have added features to improve the performance when the data has a high degree of duplication or sorting.

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

I compared the performance against a full sort and the Math.Net C# Quickselect implimentation.
This uses the performance testing library provided in a previous [post]({% post_url 2016-05-20-performance-testing %}).


| Duplication |   Sorted   |  Current  |  MathNet  |  FullSort  |  1.000 =  |
|:-----------:|:----------:|:---------:|:---------:|:----------:|:---------:|
|    Low      |    No      |   1.000   |   1.367   |    6.568   |  0.1030s  |
|    Low      |    Part    |   1.000   |   1.339   |    9.227   |  0.0478s  |
|    Low      |    Yes     |   1.000   |   1.536   |   13.347   |  0.0105s  |
|    Medium   |    No      |   1.000   |   1.358   |    6.569   |  0.1027s  |
|    Medium   |    Part    |   1.000   |   1.384   |    9.425   |  0.0481s  |
|    Medium   |    Yes     |   1.000   |   1.833   |   17.559   |  0.0100s  |
|    High     |    No      |   1.000   |   1.340   |    4.748   |  0.1062s  |
|    High     |    Part    |   1.000   |   1.590   |    8.021   |  0.0526s  |
|    High     |    Yes     |   1.000   |   3.176   |   26.609   |  0.0087s  |

*)
module StatisticsTests =
    /// Returns the median of an array.
    let medianInplaceTest() = ()
(**
## Conclusion

Todo.

*)