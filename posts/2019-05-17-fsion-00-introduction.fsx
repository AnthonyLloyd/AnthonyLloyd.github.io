(**
\---
layout: post
title: "Fsion: 0. Introduction"
tags: [f#,fsion,database,timeseries]
description: "Fsion: 0. Introduction"
keywords: F#, fsion, database, timeseries
exclude: true
\---

## SetSlim and MapSlim

[MapSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/MapSlim.fs)
[MapSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/MapSlimTests.fs)  
[SetSlim.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/SetSlim.fs)
[SetSlimTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SetSlimTests.fs)

Smaller and faster.
No lock needed for reads.
A previous [post](http://{{site.baseurl}}blog/2018/12/14/mapslim) covers the ~50% performance improvement over `Dictionary`.

## DataSeries

[DataSeries.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/DataSeries.fs)
[DataSeriesTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/DataSeriesTests.fs)

DataSeries set check order value is sensible.

## Transactor

[Transactor.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Transactor.fs)
[TransactorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/TransactorTests.fs)

## Selector

[Selector.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/Selector.fs)
[SelectorTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/SelectorTests.fs)


## Why F#
## Conclusion

**)