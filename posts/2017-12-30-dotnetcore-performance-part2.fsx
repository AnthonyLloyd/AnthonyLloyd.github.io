(**
\---
layout: post
title: ".Net Core 2.0 Performance Notes Revisited"
tags: [dotnetcore,dotnet,fsharp,performance,benchmarks]
description: "Benchmarks Game .Net Core 2.0 Performance Notes Revisited"
keywords: C#, F#, dotnet, dotnetcore, performance, benchmarks
\---

This post is part of the [F# Advent Calendar 2017](https://sergeytihon.com/2017/10/22/f-advent-calendar-in-english-2017/) series.
Many thanks to Sergey Tihon for organizing these.

Over the past few weeks I've been submitting improvements to some of the F# programs in the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/).
In a previous [post]({% post_url 2017-08-15-dotnetcore-performance %}) I did this for the C# programs.

Since that post things have moved on and C# is currently faster than Java for 8 out of 10 of the programs.
Java is faster for `regex-redux` as .Net Core doesn't yet have a compiled regex implementation.
For `k-nucleotide` Java makes use of a dictionary well suited to the program not available to C#.

Most of the submissions to the F# programs were ports of the C# code that had recently been optimised.
For `fasta` and `k-nucleotide` further optimisations were discovered.
`ArrayPool` is very useful in the case of `fasta`.
For `k-nucleotide` the largest dictionary can be constructed more efficiently in four parallel parts.

Another tempting optimisation was in F# there is a one to one replacement to use native pointers for array access e.g. `Array.get a i` becomes `NativePtr.get a i`.
This actually only provided a small improvement in most cases and wasn't always done.

I feel I have to plug [Expect.isFasterThan](https://github.com/haf/expecto#performance-module) in Expecto.
It's a quick way of checking that one implementation is truely faster than another and has proven invaluable.

![isFasterThan](/{{site.baseurl}}public/perf/half-is-faster.png)

## Results

[C# vs Java](http://benchmarksgame.alioth.debian.org/u64q/csharp.html), [F# vs C#](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=csharpcore), [F# vs Java](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=java), [F# vs Haskell](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=ghc), [F# vs OCaml](http://benchmarksgame.alioth.debian.org/u64q/fsharp.html), [F# vs Python](http://benchmarksgame.alioth.debian.org/u64q/compare.php?lang=fsharpcore&lang2=python3)

| Program            |   C#    |   F#    |  Java   | Haskell |  OCaml  | Python  |
|:-------------------|--------:|--------:|--------:|--------:|--------:|--------:|
| pidigits           |   **3.03**  |   3.05  |   3.12  |  Error  |  Error  |   3.43  |
| reverse-complement |   **0.78**  |   0.82  |   1.03  |   1.40  |   0.79  |   3.26  |
| fannkuch-redux     |  **14.44**  |  16.65  |  17.26  |  15.40  |  16.12  | 565.97  |
| binary-trees       |   **8.26**  |   8.54  |   8.34  |  23.66  |  10.03  |  93.55  |
| n-body             |  **21.37**  |  22.86  |  22.10  |  21.43  |  21.67  | 838.39  |
| mandelbrot         |   **5.83**  |   6.66  |   6.04  |  11.69  |  55.18  | 225.24  |
| fasta              |   2.09  |   **1.67**  |   2.33  |   9.36  |   6.00  |  59.47  |
| k-nucleotide       |  11.47  |  10.43  |   **8.70**  |  35.01  |  21.63  |  77.65  |
| regex-redux        |  30.74  |  31.02  |  **10.34**  |  Error  |  24.66  |  15.22  |
| spectral-norm      |   4.07  |   4.22  |   4.23  |   **4.04**  |   4.31  | 180.97  |

## Conclusion

As said in the previous [post]({% post_url 2017-08-15-dotnetcore-performance %}) there are many caveats to these results.
They represent the current state of a set of programs on a specific test machine.
That being said, I think there is enough evidence for some general conclusions.

First of all, the overall results for .Net Core 2.0 are very impressive compared to other managed platforms.

F# performance in the worst case is only 15% behind C#. F# is a higher level language that results in simpler and shorter code.
It's good that even in the extreme of a low-level performance benchmark it is not too far behind C#.

F# in fact shows very good performance against Java resulting in a 5 all draw.
This means F# would be expected to perform better than Scala or Kotlin if they were to participate in the benchmarks.

F# looks to have the best performance among the functional languages.
This is due to the efficiency of .Net Core 2.0 and being able to write F# in a functional-first style.

Hopefully 2018 will see continued adoption of .Net Core 2.0 and F#. Happy new year!

*)