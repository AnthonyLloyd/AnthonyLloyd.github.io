(**
\---
layout: post
title: "Median and MAD Revisited"
tags: [testing,perfomance,fsharp,MAD,median,outlier,statistics]
description: "Median and MAD Revisited"
keywords: median absolute deviation, MAD, median, outlier, statistics
\---

In a previous [post]({% post_url 2016-10-21-MAD-Outliers %}).

## Revist
*)
module Statistics =
    let medianInPlace (a:float[]) (index:int) (length:int) =
        let inline swap (a:float[]) i j =
            let temp = a.[i]
            a.[i]<-a.[j]
            a.[j]<-temp
        let inline swapIf (a:float[]) i j =
            let ai = a.[i]
            let aj = a.[j]
            if ai>aj then
                a.[i]<-aj
                a.[j]<-ai
        let k = length >>> 1
        let rec outerLoop lo hi =
            swapIf a lo k
            swapIf a lo hi
            swapIf a k hi
            let pivot = a.[k]
            let resetLo = if a.[lo]=pivot then a.[lo]<-Double.MinValue; true else false
            let resetHi = if a.[hi]=pivot then a.[hi]<-Double.MaxValue; true else false
            let mutable i = lo+1
            let mutable j = hi-1
            while a.[i]<=pivot do i<-i+1
            while a.[j]>=pivot do j<-j-1
            while i<k && k<j do
                swap a i j
                i<-i+1
                j<-j-1
                while a.[i]<=pivot do i<-i+1
                while a.[j]>=pivot do j<-j-1
            if i<j then
                swap a i j
                if k<j then
                    i<-lo
                    j<-j-1
                    while a.[j]>=pivot do j<-j-1
                else
                    j<-hi
                    i<-i+1
                    while a.[i]<=pivot do i<-i+1
            else
                if k<j then i<-lo
                elif k>i then j<-hi
            if resetLo then a.[lo]<-pivot
            if resetHi then a.[hi]<-pivot
            if i>=j then
                if length &&& 1 = 0 && j+1=k then
                    let mutable v = a.[lo]
                    for i = lo+1 to j do
                        if a.[i]>v then v<-a.[i]
                    (v + pivot) * 0.5
                else pivot
            else outerLoop i j
        outerLoop index (index+length-1)
(**

Performance

<img src="/{{site.baseurl}}public/test/median_tests.png" title="median tests"/>
<img src="/{{site.baseurl}}public/test/median.png" title="median"/>

## Estimator

*)
type MedianEstimator =
    val mutable private A : float array
    val mutable private Median : float
    val mutable private MAD : float
    val private F : float
    val mutable N : int
    new(n:int,f:float) =
        {A = Array.zeroCreate n; Median = 0.0; MAD = 0.0; F = f; N = 0}
    member m.MedianAndMAD =
        if isNull m.A then m.Median, m.MAD
        else
            let median = Statistics.medianInPlace m.A 0 m.N
            let mad = Array.sub m.A 0 m.N
            for i = 0 to mad.Length-1 do
                mad.[i] <- abs(mad.[i]-median)
            median, Statistics.medianInPlace mad 0 mad.Length
    member m.Add (s:float) =
        if isNull m.A then
            m.Median <- m.Median +  m.MAD * m.F * float(sign(s-m.Median))
            m.MAD <- m.MAD +  m.MAD * m.F * float(sign(abs(s-m.Median)-m.MAD))
            m.N <- m.N + 1
        else
            m.A.[m.N] <- s
            m.N <- m.N + 1
            if m.N = m.A.Length then
                let median, mad = m.MedianAndMAD
                m.Median <- median
                m.MAD <- mad
                m.A <- null
(**

## Conclusion

Hello.

*)