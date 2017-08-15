(**
\---
layout: post
title: ".Net Core 2.0 vs Java Performance Notes"
tags: [dotnetcore,java,performance,benchmarks]
description: "Benchmarks Game C# .Net Core 2.0 vs Java Performance Notes"
keywords: C#, F#, dotnetcore, java, performance, benchmarks
\---
*)

(*** hide ***)
module Main
    let x = 1
(**
Over the past few weeks I've been submitting improvements to some of the C# programs in the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/).

When I first saw the [C# .Net Core vs Java](http://benchmarksgame.alioth.debian.org/u64q/csharp.html) benchmarks the score was **.Net Core 2.0** 4 - **Java** 6.
This surprised me as I was under the impression .Net Core 2.0 performance would be very good.

I have concentrated on the 6 programs where Java was faster and not looked at the other 4 (binary-trees, spectral-norm, fasta, pidigits).

Below are some notes on each of these programs and some caveated conclusions. 

### reverse-complement

Old Result: C# 1.39s - Java 1.10s  
Reversing in place and more efficient parallel processing.  
New Result: C# 0.79s - Java 1.10s  

### mandelbrot

Old Result: C# 7.29s - Java 7.10s  
Simplified the parallel processing by using TPL Parallel.For.
New Result: C# 6.78s - Java 7.10s  

### n-body

Old Result: C# 21.71s - Java 21.54s  

I made many failed attempts to try to improve this result including using SMD, rem.

### fannkuch-redux

Old Result: C# 18.80s - Java 13.74s  
New Result: C# 14.98s - Java 13.74s  

### k-nucleotide

Old Result: C# 13.76s - Java 07.93s  
New Result: C# 12.37s - Java 07.93s  

### regex-redux

Old Result: C# 32.02s - Java 12.31s  
New Result: C# 31.19s - Java 12.31s  

## Conclusion

Good benchmark process
Out of ideas


*)