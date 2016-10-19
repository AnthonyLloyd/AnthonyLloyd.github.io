(**
\---
layout: post
title: "Get MAD with Outliers with an improved Median function"
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
    let inline sqr x = x*x
    
(**
This post presents a more robust method of detecting outliers in sample data than commonly used.
The method is based on the median and an optimised F# median function is provided. 

## Background

[Researchers](https://www.researchgate.net/publication/256752600_Detecting_outliers_Do_not_use_standard_deviation_around_the_mean_use_absolute_deviation_around_the_median) most commonly use standard deviation around the mean to detect outliers.
This is a problem because the mean and standard deviation give greater weight to extreme data points.

The mean is the point that minimises the sum of the square deviations, whereas the median is the point that minimises the sum of the absolute deviations.
The median and [median absolute deviation](https://en.wikipedia.org/wiki/Median_absolute_deviation) give a more robust measure of statistical dispersion and are more resilient to outliers.   

When a politician says average wages are increasing be sure to check the median is being reported.

## Statistics

The [median](https://en.wikipedia.org/wiki/Median) is the value separating the higher half of the sample from the lower half of the sample.


The [median absolute deviation](https://en.wikipedia.org/wiki/Median_absolute_deviation) is defined as

$$$
\operatorname{MAD} = \operatorname{median}\left(\left|x_i - \operatorname{median}(x_i)\right|\right)

Outliers can be identified as points that are outside a fixed multiple of median absolute deviation from the median. Recommended values for this multiple are 2.5 or 3.

## Median and MAD functions

The following median function makes use the [MODIFIND](http://dhost.info/zabrodskyvlada/algor.html) algorithm by Vladimir Zabrodsky.
It provides a 20-30% performance improvement over the [Quickselect](https://en.wikipedia.org/wiki/Quickselect) algorithm.
In addition I have added features to improve the performance when the data has a high degree of duplicate or sorting.

*)
module Statistics =
    /// Returns the median of the sample array.
    let medianInplace (a:_[]) = 1.0
(**

## Property and Performance testing

I compared the performance against a full sort and the Math.Net C# Quickselect implimentation.
This uses the performance testing library provided in a previous [post]({% post_url 2016-05-20-performance-testing %}).

*)
module StatisticsTests =
    /// Returns the median of an array.
    let medianInplaceTest() = ()
(**


Table.

## Conclusion

Todo.

*)