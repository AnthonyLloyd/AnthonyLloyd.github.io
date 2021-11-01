(**
\---
layout: post
title: "An improved N-dimensional optimiser"
tags: [minimum, n-dimension, optimiser]
description: "An improved N-dimensional optimiser"
keywords: minimum n-dimension optimiser
\---

I realise I didn't blog about this N-dimensional optimiser when it was released so here is a quick overview.

In one dimension a more efficient Minimum algorithm was developed using a robust cubic interpolation.
This has around 50% fewer function calls than Brent across a number of test functions.

In N dimensions an efficient [BFGS](https://en.wikipedia.org/wiki/Broyden%E2%80%93Fletcher%E2%80%93Goldfarb%E2%80%93Shanno_algorithm) algorithm was developed using in place symmetric MKL rank-k, rank-2k functions such that the main loop has no allocations.
This combined with the above line minimisation results in around 50-70% fewer function calls than other [BFGS](https://en.wikipedia.org/wiki/Broyden%E2%80%93Fletcher%E2%80%93Goldfarb%E2%80%93Shanno_algorithm) algorithms across a number of test functions.
The performance of the algorithm should also scale well with N.

Tolerance parameters across all of the optimisation functions have been made simple, intuative, and consistent.

Here are the [code](https://github.com/MKL-NET/MKL.NET/blob/master/MKL.NET.Optimization/Optimize.Minimum.cs) and [tests](https://github.com/MKL-NET/MKL.NET/blob/master/Tests/Optimize.MinimumTests.fs).

*)
