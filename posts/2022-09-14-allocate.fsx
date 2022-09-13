(**
\---
layout: post
title: "A robust weighted allocation algorithm thanks to CsCheck."
tags: [cscheck, random, testing, rounding, allocation, weighted, integer]
description: "A robust weighted allocation algorithm thanks to CsCheck."
keywords: cscheck random testing rounding allocation weighted integer
\---

Allocating an integer total over a set of weights is a surprisingly tricky problem.
Consider a poll result where round percentages are required to add up to 100.
Or money or shares to be allocated to a number of accounts.

The most common way of solving this is to round the results and then adjust the largest weights for any residual difference.

This can produce nonsensical results. It can lead to larger weights getting a smaller allocation.
For small totals it can result in the largest weight getting no allocation at all.

With the help of [CsCheck](https://www.nuget.org/packages/CsCheck/) we will [revisit]({% post_url 2018-03-05-rounding %}) this algorithm.

## CsCheck

We can create random tests to check for these kinds of issues.
These guide us to the simplest algorithm that satisfy the requirements.
The random test code not only gives us many failing simple examples but also hint at the solution.

<img style="border:1px solid black" src="/{{site.baseurl}}public/allocate/tests.png" title="Tests"/>

## Error Minimising Algorithm

In this case the solution is an error minimisation algorithm which then guarantees smaller weights never gets a larger allocations.

<img style="border:1px solid black" src="/{{site.baseurl}}public/allocate/allocate.png" title="Allocate"/>

## Conclusion

Writing random tests leads to a robust algorithm that covers all possible inputs including negative values.

There were also some nasty precision rounding issues to solve that would be hard to spot using unit testing and a handful of examples.

[Allocate.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/Allocator.cs)
[AllocatorCheck.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/AllocatorCheck.cs)
[AllocatorTests.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/AllocatorTests.cs)

*)