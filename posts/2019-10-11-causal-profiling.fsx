(**
\---
layout: post
title: "Causal Profiling in .NET"
tags: [causal,profiling,performance]
description: "Causal Profiling in .NET"
keywords: causal, profiling, performance
exclude: true
\---

Recently I saw a great multithreading profiling technique called causal profiling.

<iframe width="560" height="315" src="https://www.youtube.com/embed/r-TLSBdHe1A" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

I decided to see if I could do this in .NET.

It fits well in a debug library that I use as a preference to step through debugging [Kicking the Debugger habit]({% post_url 2017-04-30-kicking-the-debugger %}).


## Simple model

Causal profiling...                                                                                                             
Iterations: 300                                                                                                                 

| Region         |  Count  |  Time%  |     +10%     |      +5%     |      -5%     |     -10%     |     -15%     |     -20%     |
|:---------------|--------:|--------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|
| rnds           |    1629 |    87.7 |  -2.4 ± 0.5  |  -1.3 ± 0.5  |   1.9 ± 0.6  |   1.7 ± 0.6  |   3.7 ± 0.5  |   4.3 ± 0.6  |
| bytes          |    1627 |   340.3 |  -8.1 ± 0.5  |  -4.4 ± 0.5  |   4.1 ± 0.5  |   7.2 ± 0.6  |  11.7 ± 0.5  |  14.8 ± 0.6  |
| one            |       1 |     5.4 |  -0.3 ± 0.5  |  -0.4 ± 0.5  |   0.8 ± 0.6  |   0.3 ± 0.6  |   0.3 ± 0.6  |   0.6 ± 0.6  |
| write          |    1630 |    15.1 |  -1.3 ± 0.5  |  -0.8 ± 0.5  |   0.5 ± 0.6  |  -0.4 ± 0.6  |   1.1 ± 0.6  |   1.0 ± 0.6  |


## Faithful model

Causal profiling...                                                                                                             
Iterations: 300      

| Region         |  Count  |  Time%  |     +10%     |      +5%     |      -5%     |     -10%     |     -15%     |     -20%     |
|:---------------|--------:|--------:|-------------:|-------------:|-------------:|-------------:|-------------:|-------------:|
| rnds           |    1629 |    87.7 |  -2.7 ± 0.6  |  -1.1 ± 0.5  |   0.1 ± 0.5  |   1.6 ± 0.5  |   2.7 ± 0.5  |   3.9 ± 0.5  |
| bytes          |    1627 |   286.6 |  -7.9 ± 0.5  |  -4.3 ± 0.5  |   2.7 ± 0.5  |   6.7 ± 0.5  |   9.3 ± 0.5  |  12.8 ± 0.5  |
| one            |       1 |     6.7 |  -0.5 ± 0.5  |  -0.2 ± 0.5  |  -0.3 ± 0.5  |   0.4 ± 0.5  |   0.2 ± 0.5  |   0.3 ± 0.5  |
| write          |    1630 |    16.9 |  -1.0 ± 0.5  |  -0.7 ± 0.5  |  -0.3 ± 0.5  |   0.3 ± 0.5  |   0.5 ± 0.5  |   0.8 ± 0.5  |

## Conclusion

Well not so much of a conclusion as more of a question.  
What do we think simple or faithful?  
I may not be fully appreciating why the faithful model represents reducing a region.  

*)