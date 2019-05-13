(**
\---
layout: post
title: "Fsion - Size"
tags: [f#,fsion,database,timeseries]
description: "Fsion - Size"
keywords: F#, fsion, database, timeseries
exclude: true
\---

The sample data set is around 10 months of daily position files for all the
[iShares](https://www.ishares.com/uk/intermediaries/en/products/etf-product-list#!type=emeaIshares&tab=overview&view=list)
funds available online.
All fields in the files are loaded apart from any that can be calculated.

Funds: 280
Days: 206
Position files: 71,261
Size unzipped: 4.4GB
Size .zip normal: 1.1GB
Size .7z ultra: 199MB


## DataSeries compression


### Data details

Text: Count = 59,099 Max length = 50

| Count / Bytes | transaction | entitytype | attribute | instrument | position |
|:-----------|:----------:|:----------:|:----------:|:----------:|:----------:|
| uri | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| name | 0 / 0 | 5 / 15 | 20 / 60 | 38,036 / 279,391 | 0 / 0 |
| time | 71,262 / 909,886 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| attribute_type | 0 / 0 | 0 / 0 | 20 / 60 | 0 / 0 | 0 / 0 |
| attribute_isset | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| transaction_based_on | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| isin | 0 / 0 | 0 / 0 | 0 / 0 | 36,476 / 273,162 | 0 / 0 |
| ticker | 0 / 0 | 0 / 0 | 0 / 0 | 38,036 / 275,050 | 0 / 0 |
| currency | 0 / 0 | 0 / 0 | 0 / 0 | 38,036 / 240,802 | 0 / 0 |
| assetclass | 0 / 0 | 0 / 0 | 0 / 0 | 38,023 / 261,032 | 0 / 0 |
| sector | 0 / 0 | 0 / 0 | 0 / 0 | 37,927 / 258,075 | 0 / 0 |
| exchange | 0 / 0 | 0 / 0 | 0 / 0 | 12,046 / 79,467 | 0 / 0 |
| country | 0 / 0 | 0 / 0 | 0 / 0 | 38,036 / 267,549 | 0 / 0 |
| coupon | 0 / 0 | 0 / 0 | 0 / 0 | 25,552 / 193,815 | 0 / 0 |
| maturity | 0 / 0 | 0 / 0 | 0 / 0 | 25,546 / 205,849 | 0 / 0 |
| price | 0 / 0 | 0 / 0 | 0 / 0 | 38,036 / 15,846,249 | 0 / 0 |
| duration | 0 / 0 | 0 / 0 | 0 / 0 | 25,552 / 5,740,430 | 0 / 0 |
| fund | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 147,323 / 1,184,564 |
| instrument | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 147,323 / 1,096,271 |
| nominal | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 | 147,323 / 8,306,493 |

### Size Estimates

32-bit: Text = 3,028,394 Data = 64,364,789 Total = 67,393,183  
64-bit: Text = 3,974,002 Data = 78,838,077 Total = 82,812,079  
Size on disk = 42,348,255  

<img style="margin-left:20px" src="/{{site.baseurl}}public/fsion/size-by-files.png" title="size by files" />

## Conclusion

This is very handy as the database file size is small enough to commit to github and can be used going forward for testing and performance benchmarking.

Looking at the size on disk compared to 32-bit and 64-bit in memory shows that the objects and pointers contribute quite a lot to the size.
If the DataSeries were not in a time series compressed format this would be a lot higher.
This agrees with what I have often found in server caches. Tracking and holding fine grained subsets of the database can actually use a lot of memory.

It also shows my estimates in the [Data-First Architecture](http://anthonylloyd.github.io/blog/2018/02/01/architecture-data-first) blog post are too
high as it doesn't take account of the DataSeries compression that is possible.

Extrapolating these curves to 10 years of files would give total memory usage of around 650 MB.
The files contain the key changing attributes.
A database with a number of additional attributes would be expected to comfortably fit in 1 to 5 GB.

Next we will look at the perfomance characteristics of this data set compared to other options.

*)