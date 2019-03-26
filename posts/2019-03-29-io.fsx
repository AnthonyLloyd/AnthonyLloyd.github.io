(**
\---
layout: post
title: "F# Implementation of Scala ZIO"
tags: [io,zio,async,reader,result]
description: "F# Implementation of Scala ZIO"
keywords: F#, io, zio, async, result
\---
*)

(**

This is a prototype implementation of [Scala ZIO](https://github.com/scalaz/scalaz-zio) in F#.

## Background

- IO = Reader + Async + Result
- Reader - effect dependencies are inferred
- Error - error type is inferred and auto lifted into Either if needed
- Async - efficient use of OS thread without blocking
- Result - Simple timeout and retry based on Result.Error
- Cancel - integrated automatic cancelling of operations in cases such as race or upon an error

- Pics:
- IO = Reader + Async  Result
- retry program
- pic of type
- race

## Reader

## Async

## Result

## Conclusion

## References

[ZIO Overview](https://scalaz.github.io/scalaz-zio/overview/)  
[ZIO Data Types](https://scalaz.github.io/scalaz-zio/datatypes/)  

*)