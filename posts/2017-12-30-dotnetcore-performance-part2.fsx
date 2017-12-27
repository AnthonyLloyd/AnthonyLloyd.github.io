(**
\---
layout: post
title: ".Net Core 2.0 Performance Notes Revisited"
tags: [dotnetcore,dotnet,fsharp,performance,benchmarks]
description: "Benchmarks Game .Net Core 2.0 Performance Notes Revisited"
keywords: C#, F#, dotnet, dotnetcore, performance, benchmarks
\---

This post is part of the [F# Advent Calendar 2017](https://sergeytihon.com/2017/10/22/f-advent-calendar-in-english-2017/) series.
Many thanks to Sergey Tihon for organizing this.

Over the past few weeks I've been submitting improvements to some of the F# programs in the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/).

In a previous [post]({% post_url 2017-08-15-dotnetcore-performance %}) I did this for the C# programs.  

Since that post things have moved on and C# is currently faster than Java for 8 out of 10 of the programs.
Java is faster for `regex-redux` as .Net Core doesn't yet have a compiled regex implementation.
For `k-nucleotide` Java makes use of a dictionary well suited to the program not available to C#.

Most of the submissions to the F# programs were ports of the C# code that had recently been optimised.
For `fasta` and `k-nucleotide` further optimisations were discovered.
`ArrayPool` is very useful in the case of `fasta`.
For `k-nucleotide` the largest dictionary can be constructed more efficiently in four parallel parts.

Another tempting optimisation was that in F# there is a one to one replacement to use native pointers for array access e.g. `Array.get a i` becomes `NativePtr.get a i`.
This actually only provided a small improvement in most cases.

## Results

[C# vs Java](http://benchmarksgame.alioth.debian.org/u64q/csharp.html)
[F# vs C#](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=csharpcore)
[F# vs Java](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=java)
[F# vs Haskell](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=ghc)
[F# vs OCaml](http://benchmarksgame.alioth.debian.org/u64q/fsharp.html)
[F# vs Python](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=python3)

| Program            |   F#    |   C#    |  Java   | Haskell |  OCaml  | Python  |
|:-------------------|--------:|--------:|--------:|--------:|--------:|--------:|
| fasta              |   **1.67**  |   2.09  |   2.33  |   9.36  |   6.00  |  59.47  |
| pidigits           |   3.05  |   **3.03**  |   3.12  |  Error  |  Error  |   3.43  |
| reverse-complement |   0.82  |   **0.78**  |   1.03  |   1.40  |   0.79  |   3.26  |
| n-body             |  22.86  |  **21.37**  |  22.10  |  21.43  |  21.67  | 838.39  |
| mandelbrot         |   6.66  |   **5.83**  |   6.04  |  11.69  |  55.18  | 225.24  |
| fannkuch-redux     |  16.65  |  **14.44**  |  17.26  |  15.40  |  16.12  | 565.97  |
| binary-trees       |   8.54  |   **8.26**  |   8.34  |  23.66  |  10.03  |  93.55  |
| k-nucleotide       |  10.43  |  11.47  |   **8.70**  |  35.01  |  21.63  |  77.65  |
| regex-redux        |  31.02  |  30.74  |  **10.34**  |  Error  |  24.66  |  15.22  |
| spectral-norm      |   4.22  |   4.07  |   4.23  |   **4.04**  |   4.31  | 180.97  |

## Conclusion

![isFasterThan](/{{site.baseurl}}public/perf/half-is-faster.png)

*)