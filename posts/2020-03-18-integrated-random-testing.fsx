(**
\---
layout: post
title: "Integrated Random Testing"
tags: [testing,perfomance,fsharp]
description: "Integrated Random Testing"
keywords: f#, fsharp, performance, testing
\---

For a while I've been bothered by the performance of testing libraries in general, but also with how random testing and performance testing can't be multithreaded and integrated better.
[Expecto](https://github.com/haf/expecto) does a great job of improving performance by running unit tests in parallel which also opens up useful functionality like stress testing.
I want a testing library where making a test random is a simple change to a unit test where shrinking just works.
I want random tests to run and shrink across multiple threads.
I want random performance tests that runs a range of inputs.

## Design

The goal is a simpler, more lightweight testing library with faster, more integrated parallel random testing with automatic shrinking.
The library should encourage the shift from a number of unit and regression tests with input and output data to fewer more general random tests.

The prototype I've come up with has the following features:

- Asserts no longer exception based and all are evaluated. => More than one Assert per test are accepetable. Simpler setup and faster for multi part testing.
- Integrate random testing more closely. => Simpler syntax. Easier more general testing. Reduce emphasis on the term "property based test".
- No sizing or number of runs for random tests. Instead use distributions. => More realistics examples.
- Automatic random shrinking giving a reproducible seed. Smaller candidates found using fast PCG loop. => Simpler reproducible examples.
- Stress testing in parallel across unit and random tests using PCG streams. => Low sync, high performance, fine grained parallel testing.

## Random testing with random shrinking



## Conclusion

- John Hughes don't write tests! - call API randomly with state machine
    - look for interaction bug tweet! feature interaction bugs
- John Hughes https://youtu.be/NcJOiQlzlXQ - one prop test - label test cases would have built, check all results and cover label %.

  
*)