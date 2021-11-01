(**
\---
layout: post
title: "Statistic Estimators"
tags: [statistics, pchip, median, quantiles, quartiles, histogram]
description: "Running high performance, fixed low memory estimators for Quantile, Quartiles, Quantiles, Histogram, Central/Standard Moments."
keywords: statistics pchip median quantiles quartiles histogram
\---

The following are a family of fixed low memory, high performance statistic estimators.

- [Moment4Estimator](https://github.com/MKL-NET/MKL.NET/blob/051a853284e5d8f784db989b27e45d64255a402a/MKL.NET.Statistics/MomentEstimator.cs#L20) - Mean, Standard Deviation, Skewness and Kurtosis unbiased estimator.
- [Moment3Estimator](https://github.com/MKL-NET/MKL.NET/blob/051a853284e5d8f784db989b27e45d64255a402a/MKL.NET.Statistics/MomentEstimator.cs#L93) - Mean, Standard Deviation and Skewness unbiased estimator.
- [Moment2Estimator](https://github.com/MKL-NET/MKL.NET/blob/051a853284e5d8f784db989b27e45d64255a402a/MKL.NET.Statistics/MomentEstimator.cs#L157) - Mean and Standard Deviation unbiased estimator.
- [Moment1Estimator](https://github.com/MKL-NET/MKL.NET/blob/051a853284e5d8f784db989b27e45d64255a402a/MKL.NET.Statistics/MomentEstimator.cs#L212) - Mean estimator.
- [QuantileEstimator](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Statistics/QuantileEstimator.cs) - Single quantile p estimator using min, p/2, p, (1+p)/2, max estimation points.
- [QuartileEstimator](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Statistics/QuartileEstimator.cs) - Median, Min, Max, Lower Quartile and Upper Quartile estimator. This is the data required to produce a box plot.
- [QuantilesEstimator](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Statistics/QuantilesEstimator.cs) - Multiple quantile estimator with min and max points.
- [HistogramEstimator](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Statistics/HistogramEstimator.cs) - Histogram estimator by equal quantile range bucketing.

They can incrementally add observations and statistics can be calculated at any point.
All have an Add and + operator overload to combined the summary statistics from two separate estimators.

## Moment Estimators

The moment estimators are a performance optimised set of unbiased central and standard moment calculations.
The algorithms used are detailed [here](https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance).
Nothing radical just performance optimised code to the specific statistics required.

## P² paper

[This](/public/stats/psqr.pdf) paper by Dr Raj Jain and Dr Imrich Chlamtac describes a great way to estimate ordered statistics like median and quantiles without storing all the observations.
The remaining estimators are based on this paper.

Unfortunately there are a some problems with the algorithms put forward in the paper which need correcting:

1. The marker increments added each iteration create a [rounding error](https://aakinshin.net/posts/p2-quantile-estimator-rounding-issue/) which is important since the marker is compared to integer counts.
2. The inequality operators used on the quantile values do not agree with the definition of the [cumulative distribution function](https://en.wikipedia.org/wiki/Cumulative_distribution_function).
3. The Piecewise-Parabolic (P²) interpolation used can produce poor results as the fitted quadratic is not [monotone](https://en.wikipedia.org/wiki/Monotonic_function).

<img style="border:1px solid black" src="/{{site.baseurl}}public/interp/quad_over.png" title="Quadratic"/>

Here the dotted line is the fitted quadratic around the middle point that needs to move from 3 to 4.

To correct 1. we cannot use increments and need to just calculate the desired marker position.
Increments were suggested to reduce CPU overhead, but from performance testing in .NET this is not the case.

To correct 2. we need to compare if the observations are less than **or equal** to the quantile value as per the [CDF](https://en.wikipedia.org/wiki/Cumulative_distribution_function) definition.
We also need to allow the possibility that there are more than one observation of the minimum value.
Some distributions can produce duplicate values and also a large number of the minimum value.

To correct 3. we need to replace the interpolation.
The issue is not that the quadratic can overshoot since the algorithm checks this and falls back to linear interpolation.
The problem is that it can produce results that are arbitrarily close to one of the points it is next to. 

## PCHIP interpolation

Fortunately there is a monotone interpolation algorithm that works well and is quick to calculate.  

[PCHIP](/public/interp/pchip/A_method_for_constructing_local_monotone_picewise_cubic_interpolants_fritsch1984.pdf), or harmonic spline, is a cubic spline interpolation algorithm.
It has been implemented in a number of places such as [scipy](https://docs.scipy.org/doc/scipy/reference/generated/scipy.interpolate.PchipInterpolator.html) and [MathNet](https://github.com/mathnet/mathnet-numerics/blob/67f3675f1fae7d708587204e1312bf7588c39bca/src/Numerics/Interpolation/CubicSpline.cs#L306).

[PCHIP](/public/interp/pchip/A_method_for_constructing_local_monotone_picewise_cubic_interpolants_fritsch1984.pdf) is very useful for modelling [yield curves](/public/finance/Stable Interpolation for the Yield Curve - Fabien Le Floc'h.pdf) as it produces a visually pleasing, stable local interpolation.

## Conclusion

The memory requirement for these estimators is small and fixed, regardless of the number of observations.  

Performance has been optimised with [QuartileEstimator](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Statistics/QuartileEstimator.cs) being around 40% faster than others more faithful implementations.

<img style="border:1px solid black" src="/{{site.baseurl}}public/interp/estimator_perf.png" title="Performance"/>

The efficiency and low memory of these estimators makes them useful for a wide range of uses from performance measurement as above to monitoring and telemetry more generally.

Here are the [code](https://github.com/MKL-NET/MKL.NET/tree/master/MKL.NET.Statistics) and [tests](https://github.com/MKL-NET/MKL.NET/blob/master/Tests/Stats.EstimatorTests.fs).
*)
