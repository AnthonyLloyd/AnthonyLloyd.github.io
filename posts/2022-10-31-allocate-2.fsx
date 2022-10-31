(**
\---
layout: post
title: "Balinski-Young weighted allocation algorithm"
tags: [cscheck, random, testing, rounding, allocation, weighted, integer]
description: "Balinski-Young weighted allocation algorithm"
keywords: cscheck random testing rounding allocation weighted integer
\---

We will cover an alternative weighted allocation algorithm to the one in the previous [post]({% post_url 2022-09-14-allocate %}).
These algorithms are used to allocate an integer total quantity over a set of weights.

[Balinski and Young](https://en.wikipedia.org/wiki/Apportionment_paradox) proved that no allocation algorithm can be free of perceived unintuitive observations, or paradoxes.

The algorithm in the previous [post]({% post_url 2022-09-14-allocate %}) is the most fair with minimum error but it does suffer from the Alabama paradox.
This is when for a small increment of the total quantity there can be a decrease in one of the allocated values.

## Balinski-Young Algorithm

Balinski and Young constructed an [algorithm](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC433936/) that follows the quota rule (all values are either the floor or ceiling of the unrounded allocation) and is free of the Alabama paradox.

<img style="border:1px solid black" src="/{{site.baseurl}}public/allocate/balinski_young.png" title="BalinskiYoung"/>

Again this is tested with [CsCheck](https://www.nuget.org/packages/CsCheck/) including a test for the Alabama paradox.

## Conclusion

The Balinski-Young algorithm is said to be slightly biased towards larger weights.
It can be useful in situations where there is the possibility of small incremental increases to the total quantity.

There is little online about this algorithm (or the one in the previous [post]({% post_url 2022-09-14-allocate %})) and many [unanswered](https://stackoverflow.com/questions/1925691/proportionately-distribute-prorate-a-value-across-a-set-of-values) [questions](https://stackoverflow.com/questions/9088403/distributing-integers-using-weights-how-to-calculate).
From twitter polls not looking correct, to algorithms with poor properties such as cascade rounding or adjusting the largest weights, there seem to be common software errors in this area.  

[Allocator.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/Allocator.cs)  
[AllocatorCheck.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/AllocatorCheck.cs)  
[AllocatorTests.cs](https://github.com/AnthonyLloyd/CsCheck/blob/master/Tests/AllocatorTests.cs)  

*)