(**
\---
layout: post
title: "Causal Profiling in .NET"
tags: [causal,profiling,performance]
description: "Causal Profiling in .NET"
keywords: causal, profiling, performance
exclude: true
\---


<iframe width="560" height="315" src="https://www.youtube.com/embed/r-TLSBdHe1A" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Simple model

## Faithful model

Causal profiling                                                                                                          
Iterations: 499                                                                                                           
| Region         |  Count  |  Time%  |    +10%     |     +5%     |     -5%     |    -10%     |    -15%     |    -20%     |
|:--------------:|:-------:|:-------:|:-----------:|:-----------:|:-----------:|:-----------:|:-----------:|:-----------:|
| rnds           |    1629 |    94.9 |  -2.4 ± 0.5 |  -1.2 ± 0.6 |   0.3 ± 0.5 |   1.7 ± 0.5 |   2.2 ± 0.5 |   4.9 ± 0.6 |
| bytes          |    1627 |   356.9 |  -7.2 ± 0.6 |  -3.2 ± 0.6 |   3.5 ± 0.6 |   6.5 ± 0.5 |  10.5 ± 0.5 |  13.4 ± 0.5 |
| one            |       1 |     6.8 |  -0.4 ± 0.5 |  -0.0 ± 0.5 |  -0.4 ± 0.5 |  -0.3 ± 0.5 |  -0.0 ± 0.5 |   0.4 ± 0.5 |
| write          |    1630 |    18.9 |  -0.9 ± 0.5 |  -0.4 ± 0.5 |  -0.3 ± 0.5 |  -0.3 ± 0.5 |  -0.1 ± 0.5 |   0.2 ± 0.5 |


## Conclusion

Well not so much of a conclusion as more of a question.
What do we think simple or faithful?
I may not be fully appreciating why the faithful model represents reducing a region.

*)