(**
\---
layout: post
title: ".Net Core 2.0 vs Java Performance Notes"
tags: [dotnetcore,java,performance,benchmarks]
description: "Benchmarks Game C# .Net Core 2.0 vs Java Performance Notes"
keywords: C#, F#, dotnetcore, java, performance, benchmarks
\---

Over the past few weeks I've been submitting improvements to some of the C# programs in the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/).

When I first saw the [C# .Net Core vs Java](http://benchmarksgame.alioth.debian.org/u64q/csharp.html) benchmarks the score was .Net Core 2.0 **4** - Java **6**.
This surprised me as I was under the impression .Net Core 2.0 performance would be very good.

I have concentrated on the 6 programs where Java was the fastest and not looked at the other 4 (binary-trees, spectral-norm, fasta, pidigits).

Below are some notes on each of these programs and some conclusions. 

### reverse-complement

Old Result: C# 1.39s - Java 1.10s  

Changes made: Reversing in place and more efficient parallel processing.  

New Result: C# 0.79s - Java 1.10s  

### mandelbrot

Old Result: C# 7.29s - Java 7.10s  

Changes made: Simplified the parallel processing by using TPL Parallel.For.  

New Result: C# 6.78s - Java 7.10s  

### n-body

Old Result: C# 21.71s - Java 21.54s  

I made many failed attempts to try to improve this result e.g. low level parallel, SIMD.

Dissapointing as it looks like Java has a small edge on these single thread numeric calculations.  

Some I submitted can be found [here](https://alioth.debian.org/tracker/index.php?group_id=100815&atid=413122).  

### fannkuch-redux

Old Result: C# 18.80s - Java 13.74s  

I think Java has some advantage in int array manipulation.  

I tried splitting up into more parallel blocks but the overhead outweighs the better CPU use.  

Changes made: Small array optimisations and more efficient parallel processing.  

New Result: C# 14.45s - Java 13.74s  

### k-nucleotide

Old Result: C# 13.76s - Java 07.93s  

Java code cheats in that they have managed to find a very specific dictionary from an obscure library.  

The dictionary count can be done in parallel but I think the Java dictionary wins it here.  

Changes made: More efficient byte array memory use and parallel processing.   

New Result: C# 12.37s - Java 07.93s  

### regex-redux

Old Result: C# 32.02s - Java 12.31s  

Regex is not great in .Net. I don't think it's even compiled on .Net Core.  

Changes made: Reordered the tasks to run longest to shortest.  

New Result: C# 31.19s - Java 12.31s  

## Conclusion

First some caveats.

The test machine is a single 64-bit quad core machine. This may not represent current servers.
I found optimising for performance on a different machine to be very interesting.
I tended to over optimise algorithm constants and need to look for more universal performance optimisations.  

The performance benchmark testing process seems to be robust. There could be some bias in the configurations but I was not aware of any.  

No toy benchmarks truly represent the performance of a large application. These benchmarks do look to solve larger real world problems than most I have seen. 

I found the [Benchmarks Game](http://benchmarksgame.alioth.debian.org/) to be a very good set of benchmarks.
The benchmarks are well thought through and cover classic single thread calculations through to multi-threaded IO data processing.
Obviously, areas such as network performance are not as easy to test in this kind of benchmark game.
The organisers are tough but fair and the rules make a lot of sense.
The site is efficiently run and overnight was updated to .Net Core 2.0 RTM.

So now the score is .Net Core 2.0 **6** - Java **4**.  

My overall impression is that .Net Core 2.0 and Java perform about the same.
Java possibly has a small edge on some calculations and array manipulation and .Net having better parallel libraries.

So now I am out of ideas on how to improve these further.  If you have any questions or ideas, feel free to get in touch.

*)