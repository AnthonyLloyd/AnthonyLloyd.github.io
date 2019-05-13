(**
\---
layout: post
title: "Fsion - Size"
tags: [f#,fsion,database,timeseries]
description: "Fsion - Size"
keywords: F#, fsion, database, timeseries
exclude: true
\---

The sample data set is around 10 months of daily position files for the 280
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

DataSeries represent an ordered table of Date, Transaction Id and `int64` Encoded Values
with the latest values at the the top.

This is encoded as a byte array.
Each row is stored as a difference to the above field values as [varints](https://developers.google.com/protocol-buffers/docs/encoding).

Since the table is ordered, and the values in each row are very likely to be close to the ones above, very high compression ratios are possible.

## Data details

Text values are stored in a `SetSlim<Text>` collection, numeric values are encoded directly to `int64`.
The DataSeries are stored in a `MapSlim<EntityAttribute,DataSeries>`.

Text: Count = 59,099 Max length = 50

| Count<br/>Bytes | transaction | entitytype | attribute | instrument | position |
|:----------:|:----------:|:----------:|:----------:|:----------:|:----------:|
| uri | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 |
| name | 0<br/>0 | 5<br/>15 | 20<br/>60 | 38,036<br/>279,391 | 0<br/>0 |
| time | 71,262<br/>909,886 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 |
| attribute_type | 0<br/>0 | 0<br/>0 | 20<br/>60 | 0<br/>0 | 0<br/>0 |
| attribute_isset | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 |
| isin | 0<br/>0 | 0<br/>0 | 0<br/>0 | 36,476<br/>273,162 | 0<br/>0 |
| ticker | 0<br/>0 | 0<br/>0 | 0<br/>0 | 38,036<br/>275,050 | 0<br/>0 |
| currency | 0<br/>0 | 0<br/>0 | 0<br/>0 | 38,036<br/>240,802 | 0<br/>0 |
| assetclass | 0<br/>0 | 0<br/>0 | 0<br/>0 | 38,023<br/>261,032 | 0<br/>0 |
| sector | 0<br/>0 | 0<br/>0 | 0<br/>0 | 37,927<br/>258,075 | 0<br/>0 |
| exchange | 0<br/>0 | 0<br/>0 | 0<br/>0 | 12,046<br/>79,467 | 0<br/>0 |
| country | 0<br/>0 | 0<br/>0 | 0<br/>0 | 38,036<br/>267,549 | 0<br/>0 |
| coupon | 0<br/>0 | 0<br/>0 | 0<br/>0 | 25,552<br/>193,815 | 0<br/>0 |
| maturity | 0<br/>0 | 0<br/>0 | 0<br/>0 | 25,546<br/>205,849 | 0<br/>0 |
| price | 0<br/>0 | 0<br/>0 | 0<br/>0 | 38,036<br/>15,846,249 | 0<br/>0 |
| duration | 0<br/>0 | 0<br/>0 | 0<br/>0 | 25,552<br/>5,740,430 | 0<br/>0 |
| fund | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 147,323<br/>1,184,564 |
| instrument | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 147,323<br/>1,096,271 |
| nominal | 0<br/>0 | 0<br/>0 | 0<br/>0 | 0<br/>0 | 147,323<br/>8,306,493 |

## Size Estimates

Memory 32-bit: Text = 3,028,394 Data = 64,364,789 Total = 67,393,183  
Memory 64-bit: Text = 3,974,002 Data = 78,838,077 Total = 82,812,079  
Size on disk = 42,348,255  

<img src="/{{site.baseurl}}public/fsion/size-by-files.png" title="size by files" />

Extrapolating these curves to 10 years of files would give total memory usage of around 650 MB.
The files contain the key changing attributes.
A database with a number of additional attributes would be expected to comfortably fit in 1 to 5 GB.

## Conclusion

This is very handy as the database file size is small enough to commit to github and can be used going forward for testing and performance benchmarking.

Looking at the size on disk compared to 32-bit and 64-bit in memory estimates shows that the objects and pointers contribute a large amount to the size.
If the DataSeries were not in a time series compressed format this object and pointer overhead would be a lot higher.
This agrees with what is often found in server caches. Holding and tracking fine grained subsets of the database can actually use a lot of memory.

It also shows the estimates in the [Data-First Architecture](http://anthonylloyd.github.io/blog/2018/02/01/architecture-data-first) blog post are too
high as it doesn't take account of the DataSeries compression that is possible.

Next we will look at the performance characteristics of this database compared to other options.

*)