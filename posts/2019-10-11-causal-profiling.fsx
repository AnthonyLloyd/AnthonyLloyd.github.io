(**
\---
layout: post
title: "Causal Profiling in .NET"
tags: [causal,profiling,performance]
description: "Causal Profiling in .NET"
keywords: causal, profiling, performance
exclude: true
\---

Recently there was an interesting talk by Emery Berger on a multithreading profiling technique called causal profiling.
The idea is by slowing everything else down running concurrently with a region of code, you can infer what the effect would be of speeding up that code.

<iframe width="560" height="315" src="https://www.youtube.com/embed/r-TLSBdHe1A" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

The talk covers a C++ library called coz. This post explores if this can be done in .NET.

To achieve this code to define the start and end of each region will have to be added to the codebase directly.
This fits well with using a debug library in preference to step through debugging discussed in a previous [post]({% post_url 2017-04-30-kicking-the-debugger %}).

## Simple implementation

There is a simpler way that could possibly achieve the same aim.
Like many physical systems there is a scale invariance in multithreaded systems.
If all regions are proportionally slowed down, then the causal threading picture will not change. It is just like enlarging a photo.

So, if all regions apart from one are proportionally slowed down, and the run time is compared with a run where everything is slowed down, this virtual speed up can be deduced.

This can be implemented by simply recording the region start and end time and spinning in the end region call for a given percentage of the region time span.
Spinning is necessary rather than sleeping as it needs to simulate work and not encourage a context switch.

<img src="/{{site.baseurl}}public/perf/CausalSimple.png" title="PerfSimple.fs"/>

This code can be found [here](https://github.com/AnthonyLloyd/Causal/blob/master/dbg/PerfSimple.fs).

## Full implementation

The full implementation is more involved due to two main issues.

Firstly, the slow down for regions will depend on measurements of the overlap with some other region.
This region may be being run on multiple threads and the slow down should not be double counted.
It is the overlap with one or more running.

Secondly, this interesting bookkeeping will inevitably lead to efficient locking code being needed.
`Interlocked` low locking will not work as there are multiple variables to track (region thread count, region on since and total delay).
This is going to need `SpinLock`, again to discourage a context switch, and as little code as possible.

<img src="/{{site.baseurl}}public/perf/CausalFull.png" title="Perf.fs"/>

This code can be found [here](https://github.com/AnthonyLloyd/Causal/blob/master/dbg/Perf.fs).

## Statistics

These measurements need to be run for an array of code delay percentages for each region defined.
This defines an iteration.
This has to be repeated a number of times and the results are summarised after each iteration.

The summary statistics are the median and standard error after outliers are removed.
Outliers are defined as measurements outside of 3 times MAD as described in a previous [post]({% post_url 2016-10-21-MAD-Outliers %}).

Causal profiling...
Iterations: 300

| Region         |  Count  |  Time%  |     +10%     |      +5%     |      -5%     |     -10%     |     -15%     |     -20%     |
|:---------------|--------:|--------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|
| rnds           |    1629 |    20.4 |  -2.8 ± 0.6  |  -0.6 ± 0.5  |   2.2 ± 0.5  |   2.0 ± 0.5  |   3.2 ± 0.6  |   3.9 ± 0.6  |
| bytes          |    1627 |    78.3 |  -7.7 ± 0.6  |  -4.5 ± 0.6  |   4.2 ± 0.5  |   7.6 ± 0.6  |  12.3 ± 0.6  |  13.8 ± 0.6  |
| one            |       1 |     1.9 |  -0.8 ± 0.6  |  -0.5 ± 0.5  |   0.6 ± 0.5  |  -0.7 ± 0.5  |   0.3 ± 0.6  |  -0.1 ± 0.6  |
| write          |    1630 |     5.8 |  -1.2 ± 0.6  |  -0.8 ± 0.5  |   0.6 ± 0.5  |  -0.3 ± 0.6  |   1.4 ± 0.6  |   1.2 ± 0.6  |

The summary table show:

- Region - the region name defined in `regionStart`.
- Count - the number of times the region code is called.
- Time% - the total time elapsed in the region divided by the total elasped time times number of cores.
- +n% - summary median and error when the region itself is slowed down.
- -n% - summary median and error when other regions are slowed down.

## Conclusion

The full implementation is probably what will be used going forward but it is good to keep the simple version around to compare.

The talk above discusses how this technique can be extended to profile throughput and latency.
This would be a simple extension to the existing implementation.

It's amazing what can be achieved with a good idea (stolen!), some statistics and 200 lines of code.
This technique and a previous [post]({% post_url 2016-05-20-performance-testing %}) used in [Expecto](https://github.com/haf/expecto) produce fast, simple, statistically robust performance tools.

*)