(**
\---
layout: post
title: "Causal Profiling in .NET"
tags: [causal,profiling,performance]
description: "Causal Profiling in .NET"
keywords: causal, profiling, performance
exclude: true
\---

Recently there was an interesting talk on a multithreading profiling technique called causal profiling.
The idea is by slowing everything else down running concurrently with a region, you can infer what the effect would be of speeding up that region.

<iframe width="560" height="315" src="https://www.youtube.com/embed/r-TLSBdHe1A" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

This post explores if this can be done in .NET.

To achieve this code to define the start and end of regions will have to be added to the codebase directly.
This fits well with using a debug library in preference to step through debugging discussed in a previous post [Kicking the Debugger habit]({% post_url 2017-04-30-kicking-the-debugger %}).

## Simple implementation

There is a simpler way that could possibly achieve the same aim.
Similar to many other physical systems there is a scale invariance in multithreaded systems.
If all regions are proportially slowed down then the causal threading picture will not change. It is just like enlarging a photo.
So if all regions apart from one are proportially slowed down, and the run time is compared with a run where everything is slowed down, this virtual speed up can be deduced.
This can be implemented by simply recording the region start and end time and spinning in the end region call for a given percentage of the region time span.

This has been done in [PerfSimple.fs](https://github.com/Genbox/CSharpFastPFOR).

Causal profiling...                                                                                                             
Iterations: 300                                                                                                                 

| Region         |  Count  |  Time%  |     +10%     |      +5%     |      -5%     |     -10%     |     -15%     |     -20%     |
|:---------------|--------:|--------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|
| rnds           |    1629 |    87.7 |  -2.4 ± 0.5  |  -1.3 ± 0.5  |   1.9 ± 0.6  |   1.7 ± 0.6  |   3.7 ± 0.5  |   4.3 ± 0.6  |
| bytes          |    1627 |   340.3 |  -8.1 ± 0.5  |  -4.4 ± 0.5  |   4.1 ± 0.5  |   7.2 ± 0.6  |  11.7 ± 0.5  |  14.8 ± 0.6  |
| one            |       1 |     5.4 |  -0.3 ± 0.5  |  -0.4 ± 0.5  |   0.8 ± 0.6  |   0.3 ± 0.6  |   0.3 ± 0.6  |   0.6 ± 0.6  |
| write          |    1630 |    15.1 |  -1.3 ± 0.5  |  -0.8 ± 0.5  |   0.5 ± 0.6  |  -0.4 ± 0.6  |   1.1 ± 0.6  |   1.0 ± 0.6  |


## Full implementation

The full implementation is more involved due to two main issues.

Firstly, the slow down for regions will depend on measurements of the overlap with some other region.
This region may be being run on multiple threads and the slow down should not be double counted.
It is the overlap with one or more running.

Secondly, this interesting bookeeping will inevitably lead to efficient locking code being needed.
`Interlocked` low locking will not work as there are multiple variables to track (region thread count, region on since and total delay).
This is going to need `SpinLock` as little code as possible.

This has been done in [Perf.fs](https://github.com/Genbox/CSharpFastPFOR).

Causal profiling...                                                                                                             
Iterations: 300      

| Region         |  Count  |  Time%  |     +10%     |      +5%     |      -5%     |     -10%     |     -15%     |     -20%     |
|:---------------|--------:|--------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|
| rnds           |    1629 |    87.7 |  -2.7 ± 0.6  |  -1.1 ± 0.5  |   0.1 ± 0.5  |   1.6 ± 0.5  |   2.7 ± 0.5  |   3.9 ± 0.5  |
| bytes          |    1627 |   286.6 |  -7.9 ± 0.5  |  -4.3 ± 0.5  |   2.7 ± 0.5  |   6.7 ± 0.5  |   9.3 ± 0.5  |  12.8 ± 0.5  |
| one            |       1 |     6.7 |  -0.5 ± 0.5  |  -0.2 ± 0.5  |  -0.3 ± 0.5  |   0.4 ± 0.5  |   0.2 ± 0.5  |   0.3 ± 0.5  |
| write          |    1630 |    16.9 |  -1.0 ± 0.5  |  -0.7 ± 0.5  |  -0.3 ± 0.5  |   0.3 ± 0.5  |   0.5 ± 0.5  |   0.8 ± 0.5  |

## Statistics

[Get MAD with Outliers]({% post_url 2016-10-21-MAD-Outliers %})

## Conclusion

The talk above discusses how this technique can be extended to profile throughput and latency.
This would be a fairly simple extension to the existing implementation.

It always amazes me what can be achieved with a good idea (stolen!), statistics and 200 lines of code.
This technique similarly to a previous post on [Performance Testing]({% post_url 2016-05-20-performance-testing %}) produces a simple statistically robust performance test. 

*)