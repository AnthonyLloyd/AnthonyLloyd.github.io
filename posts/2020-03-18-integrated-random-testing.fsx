(**
\---
layout: post
title: "Integrated Random Testing"
tags: [testing,perfomance,fsharp]
description: "Integrated Random Testing"
keywords: f#, fsharp, performance, testing
\---

For a while I've been bothered by the performance of testing libraries in general, but also with how random testing and performance testing are not multithreaded and integrated better.
Testing libraries like [Expecto](https://github.com/haf/expecto) do a great job of improving performance by running unit tests in parallel while also opening up useful functionality like stress testing.
I want to take this further with a new prototype.

The goal is a simpler, more lightweight testing library with faster, more integrated parallel random testing with automatic parallel shrinking.

The library should encourage the shift from a number of unit and regression tests that have hard coded input and output data to fewer more general random tests.
This idea is covered well by John Hughes in [Don't write tests!](https://youtu.be/DZhbmv8WsYU) and the idea of [One test to rule them all](https://youtu.be/NcJOiQlzlXQ).
Key takeaways are one more general random test can provide more coverage for less test code, and larger test cases have a higher probability of finding a failure for a given run time.

## Prototype Design Features

1. Asserts no longer exception based and all are evaluated - More than one Assert per test are acceptable. Simpler setup and faster for multi part testing.
2. Integrate random testing more closely - Simpler syntax. Easier to move to more general random testing.
3. No sizing or number of runs for random tests - Instead use distributions. More realistics large examples.
4. Automatic random shrinking giving a reproducible seed - Smaller candidates found using a fast [PCG](https://www.pcg-random.org/) loop. Simpler reproducible examples.
5. Stress testing in parallel across unit and random tests using [PCG](https://www.pcg-random.org/) streams - Low sync, high performance, fine grained parallel testing.
6. Integrate performance testing - Performance tests can be random and run in parallel.
7. Tests are run fully in parallel using continuations - Fine grained in test asyncronous code is possible to make each test faster. 

## Random testing with random shrinking



## Conclusion

*)