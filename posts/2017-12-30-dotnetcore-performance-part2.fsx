(**
\---
layout: post
title: ".Net Core 2.0 F# - Performance Notes"
tags: [dotnetcore,dotnet,fsharp,performance,benchmarks]
description: "Benchmarks Game F# .Net Core 2.0 Performance Notes"
keywords: C#, F#, dotnet, dotnetcore, performance, benchmarks
\---

This post is part of the [F# Advent Calendar 2017](https://sergeytihon.com/2017/10/22/f-advent-calendar-in-english-2017/) series.
Many thanks to Sergey Tihon for organizing this.

Over the past few weeks I've been submitting improvements to some of the F# programs in the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/).
In a previous [post]({% post_url 2017-08-15-dotnetcore-performance %}) I did this for the C# programs.


| Program            |   F#    |   C#    |  Java   | Haskell |  OCaml  | Python  |
|--------------------|---------|---------|---------|---------|---------|---------|
| fasta              |   1.67  |   2.09  |   2.33  |   9.36  |   6.00  |  59.47  |
| k-nucleotide       |  10.43  |  11.47  |   8.70  |  35.01  |  21.63  |  77.65  |
| pidigits           |   3.05  |   3.03  |   3.12  |  Error  |  Error  |   3.43  |
| regex-redux        |  31.02  |  30.74  |  10.34  |  Error  |  24.66  |  15.22  |
| binary-trees       |   8.54  |   8.26  |   8.34  |  23.66  |  10.03  |  93.55  |
| spectral-norm      |   4.22  |   4.07  |   4.23  |   4.04  |   4.31  | 180.97  |
| reverse-complement |   0.82  |   0.78  |   1.03  |   1.40  |   0.79  |   3.26  |
| n-body             |  22.86  |  21.37  |  22.10  |  21.43  |  21.67  | 838.39  |
| mandelbrot         |   6.66  |   5.83  |   6.04  |  11.69  |  55.18  | 225.24  |
| fannkuch-redux     |  16.65  |  14.44  |  17.26  |  15.40  |  16.12  | 565.97  |


## Conclusion


*)