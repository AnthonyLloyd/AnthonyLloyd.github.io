(**
\---
layout: post
title: "F# Implementation of Scala ZIO"
tags: [io,zio,async,reader,result]
description: "F# Implementation of Scala ZIO"
keywords: F#, io, zio, async, result
exclude: true
\---

This is a prototype implementation of [Scala ZIO](https://github.com/scalaz/scalaz-zio) in F#.
It aims to be a skeleton of ZIO features such that additional functions can be easily fleshed out.

## Background

$$$
IO = Reader + Async + Result

## IO

[IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs)
*)
(*** hide ***)
namespace Fsion

type Result<'a,'e> =
    | Ok of 'a
    | Error of 'e

module Result =
    let map (f:'a->'b) (_:Result<'a,'e>) : Result<'b,'e> = failwith "hi"
    let mapError (f:'e->'f) (_:Result<'a,'e>) : Result<'a,'f> = failwith "hi"

type Time = Time
module Time =
    let now() = Time

type Either<'a,'b> =
    | Left of left:'a
    | Right of right:'b

module Either =
    let merge (e:Either<'a,'a>) =
        match e with
        | Left a -> a
        | Right a -> a

open System.Threading

type Cancel =
    private
    | Cancel of bool ref * children: Cancel list ref

module internal Cancel =
    let inline isSet (_:'r,Cancel(i,_)) = !i
    let inline create() = Cancel(ref false, ref [])
    let add (r:'r,Cancel(_,c)) =
        let i = create()
        c := i::!c
        r,i
    let rec set (r,Cancel(me,kids)) =
        me := true
        List.iter (fun i -> set(r,i)) !kids

(***)
type UIO<'r,'a> = UIO of ('r * Cancel -> ('a option -> unit) -> unit)
(*** hide ***)

type UIO<'r,'a> with
    member m.Bind(f:'a->UIO<'r,'b>) : UIO<'r,'b> =
        let (UIO run) = m
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match Option.map f o with
                        | None -> cont None
                        | Some(UIO run) ->
                            if Cancel.isSet env then cont None
                            else run env cont
                )
        )

module UIO =
    let result a : UIO<'r,'a> =
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else cont (Some a)
        )
    let effect (f:'r->'a) : UIO<'r,'a> =
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let a = fst env |> f
                if Cancel.isSet env then cont None
                else Some a |> cont
        )
    let map (f:'a->'b) (UIO run) : UIO<'r,'b> =
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else run env (fun o ->
                if Cancel.isSet env then cont None
                else
                    match Option.map f o with
                    | None -> cont None
                    | Some b ->
                        if Cancel.isSet env then cont None
                        else cont (Some b)
            )
        )
    let delay milliseconds : UIO<'r,unit> =
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let mutable t = Unchecked.defaultof<_>
                t <- new Timer((fun _ ->
                    t.Dispose()
                    if Cancel.isSet env then cont None
                    else cont (Some())
                ), null, milliseconds, Timeout.Infinite)
        )
    let flatten f : UIO<'r,'a> =
        UIO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let (UIO run) = fst env |> f
                if Cancel.isSet env then cont None
                else run env (fun t ->
                    if Cancel.isSet env then cont None
                    else cont t
                )
        )
    let toAsync (env:'r) (UIO run) : Async<'a> =
        Async.FromContinuations(fun (cont,_,_) ->
            run (env,Cancel.create()) (fun o ->
                cont o.Value
            )
        )
    let fork (UIO run) : UIO<'r,UIO<'r,'a>> =
        UIO (fun env contFork ->
            if Cancel.isSet env then contFork None
            else
                let mutable o = null
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run env (fun a ->
                        let o = Interlocked.CompareExchange(&o, a, null)
                        if isNull o |> not then
                            let cont = o :?> 'a Option->unit
                            if Cancel.isSet env then cont None
                            else cont a
                    )
                ) |> ignore
                UIO (fun env cont ->
                    let o = Interlocked.CompareExchange(&o, cont, null)
                    if Cancel.isSet env then cont None
                    elif isNull o |> not then
                        if Cancel.isSet env then cont None
                        else cont (o :?> 'a option)
                )
                |> Some
                |> contFork
        )

type ClockService =
    abstract member Time : unit -> UIO<'r,Time>
    abstract member Sleep : int -> UIO<'r,unit>

type Clock =
    abstract member Clock : ClockService

module Clock =
    let time() =
        UIO.flatten (fun (c:#Clock) ->
            c.Clock.Time()
        )
    let sleep milliseconds =
        UIO.flatten (fun (c:#Clock) ->
            c.Clock.Sleep milliseconds
        )
    let liveService =
        { new ClockService with
            member __.Time() = Time.now() |> UIO.result
            member __.Sleep milliseconds = UIO.delay milliseconds
        }

type Decision<'a,'b> =
    | Decision of cont:bool * delay:int * state:'a * (unit -> 'b)

type Schedule<'r,'s,'a,'b> =
    private
    | Schedule of initial:UIO<'r,'s> * update:('a * 's -> UIO<'r,Decision<'s,'b>>)

module Schedule =
    let forever<'r,'a> : Schedule<'r,int,'a,int> =
        Schedule (UIO.result 0, fun (_,s) -> UIO.result (Decision(true,0,s+1,(fun () -> s+1))))
    let private updated (update:('a * 's -> UIO<'r,Decision<'s,'b>>) -> 'a * 's -> UIO<'r,Decision<'s,'b2>>) (Schedule(i,u)) =
        Schedule (i,update u)
    let private check (test:'a * 'b -> UIO<'r,bool>) m =
        updated (fun update (a,s) ->
            update(a,s).Bind(fun (Decision(cont,dur,a1,fb) as d) ->
                if cont then test(a,fb()) |> UIO.map (fun b -> Decision(b,dur,a1,fb))
                else UIO.result d
            )
        ) m
    let whileOutput (f:'b->bool) m = check (fun (_,b) -> UIO.result (f b)) m
    let recurs n = whileOutput (fun i -> i <= n) forever


(***)
type IO<'r,'a,'e> = IO of ('r * Cancel -> (Result<'a,'e> option -> unit) -> unit)
(*** hide ***)
type IO<'r,'a,'e> with
    member m.Bind(f:'a->UIO<'r,'b>) : IO<'r,'b,'e> =
        let (IO run) = m
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match o with
                        | None -> cont None
                        | Some(Ok a) ->
                            let (UIO run) = f a
                            if Cancel.isSet env then cont None
                            else run env (fun o ->
                                if Cancel.isSet env then cont None
                                else Option.map Ok o |> cont
                            )
                        | Some(Error e) -> cont (Some(Error e))
                )
        )
    member m.Bind(f:'a->IO<'r,'b,'e>) : IO<'r,'b,'e> =
        let (IO run) = m
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match o with
                        | None -> cont None
                        | Some(Ok a) ->
                            let (IO run) = f a
                            if Cancel.isSet env then cont None
                            else run env cont
                        | Some(Error e) -> cont (Some(Error e))
                )
        )
    member m.Bind<'b,'e2>(f:'a->IO<'r,'b,'e2>) : IO<'r,'b,Either<'e,'e2>> =
        let (IO run) = m
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match o with
                        | None -> cont None
                        | Some(Ok a) ->
                            let (IO bind) = f a
                            if Cancel.isSet env then cont None
                            else bind env (fun o ->
                                let b = Option.map (Result.mapError Right) o
                                if Cancel.isSet env then cont None
                                else cont b
                            )
                        | Some(Error e) -> cont (Some(Error (Left e)))
                )
        )

type UIO<'r,'a> with
    member m.Bind(f:'a->IO<'r,'b,'e>) : IO<'r,'b,'e> =
        let (UIO run) = m
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match Option.map f o with
                        | None -> cont None
                        | Some(IO bind) ->
                            if Cancel.isSet env then cont None
                            else bind env cont
                )
        )

module IO =
    let ok a : IO<'r,'a,'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else cont (Some (Ok a))
        )
    let error e : IO<'r,'a,'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else cont (Some (Error e))
        )
    let result a : IO<'r,'a,'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else cont (Some a)
        )
    let effect f : IO<'r,'a,'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let a = fst env |> f
                if Cancel.isSet env then cont None
                else cont (Some a)
        )
    let map (f:'a->'b) (IO run) : IO<'r,'b,'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else run env (fun o ->
                if Cancel.isSet env then cont None
                else
                    let b = Option.map (Result.map f) o
                    if Cancel.isSet env then cont None
                    else cont b
            )
        )
    let mapError (f:'e->'e2) (IO run) : IO<'r,'a,'e2> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else run env (fun o ->
                if Cancel.isSet env then cont None
                else
                    let b = Option.map (Result.mapError f) o
                    if Cancel.isSet env then cont None
                    else cont b
            )
        )
    let mapResult (f:Result<'a,'e>->Result<'b,'e2>) (IO run) : IO<'r,'b,'e2> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else run env (fun o ->
                if Cancel.isSet env then cont None
                else
                    let b = Option.map f o
                    if Cancel.isSet env then cont None
                    else cont b
            )
        )
    let inline private foldM (succ:'a->IO<'r,'b,'e2>)
                             (err:'e->IO<'r,'b,'e2>)
                             (IO run) : IO<'r,'b,'e2> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                run env (fun o ->
                    if Cancel.isSet env then cont None
                    else
                        match o with
                        | None -> cont None
                        | Some(Ok a) ->
                            let (IO run) = succ a
                            if Cancel.isSet env then cont None
                            else run env cont
                        | Some(Error e) ->
                            let (IO run) = err e
                            if Cancel.isSet env then cont None
                            else run env cont
                )
        )
    let private retryOrElseEither (Schedule(initial, update))
                                  (orElse:'e *'s->IO<'r,'b,'e2>)
                                  (io:IO<'r,'a,'e>) : IO<'r,Either<'a,'b>,'e2> =
        let rec loop (state:'s) : IO<'r,Either<'a,'b>,'e2> =
            foldM
                (Left >> Ok >> result)
                (fun e ->
                    let u = update(e,state)
                    u.Bind (fun (Decision(cont,delay,state,_)) ->
                        if cont then
                            if delay = 0 then loop state
                            else Clock.sleep(delay).Bind(fun _ -> loop state)
                        else
                            orElse(e,state)
                            |> map Right
                    )
                )
                io
        initial.Bind loop
    let retry (policy:Schedule<'r,'s,'e,'sb>) (io:IO<'r,'a,'e>) : IO<'r,'a,'e> =
        retryOrElseEither policy (fst >> Error >> result) io
        |> map Either.merge
    let fork (IO run) : UIO<'r,IO<'r,'a,'e>> =
        UIO (fun env contFork ->
            if Cancel.isSet env then contFork None
            else
                let mutable o = null
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run env (fun a ->
                        let o = Interlocked.CompareExchange(&o, a, null)
                        if isNull o |> not then
                            let cont = o :?> Result<'a,'e> option -> unit
                            if Cancel.isSet env then cont None
                            else cont a
                    )
                ) |> ignore
                IO (fun env cont ->
                    let o = Interlocked.CompareExchange(&o, cont, null)
                    if isNull o |> not then
                        if Cancel.isSet env then cont None
                        else
                            let a = o :?> Result<'a,'e> option
                            cont a
                )
                |> Some |> contFork
        )
    let para (ios:IO<'r,'a,'e>[]) : IO<'r,'a[],'e> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let envChild = Cancel.add env
                let results = Array.zeroCreate ios.Length
                let mutable count = ios.Length
                Array.iteri (fun i io ->
                    let (IO run) = io
                    ThreadPool.QueueUserWorkItem (fun _ ->
                        run envChild (fun a ->
                            match a with
                            | Some(Ok a) ->
                                results.[i] <- a
                                if Interlocked.Decrement(&count) = 1 then
                                    if Cancel.isSet env then cont None
                                    else Ok results |> Some |> cont
                            | Some(Error e) ->
                                Cancel.set envChild
                                if Interlocked.Exchange(&count,-1) > 0 then
                                    if Cancel.isSet env then cont None
                                    else Error e |> Some |> cont
                            | None ->
                                if Interlocked.Exchange(&count,-1) > 0 then
                                    cont None
                        )
                    ) |> ignore
                ) ios
        )
(**
## Reader

<img style="padding-left:20px" src="/{{site.baseurl}}public/io/programType.png" title="program type" width="665px" height="75px" />

- effect dependencies are inferred

## Async

- efficient use of OS thread without blocking
- integrated automatic cancelling of operations in cases such as race or upon an error

*)
    let race (UIO run1) (IO run2) : IO<'r,Either<'a1,'a2>,'e1> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let env1 = Cancel.add env
                let env2 = Cancel.add env
                let mutable o = null
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run1 env1 (fun a ->
                        let o = Interlocked.CompareExchange(&o, a, null)
                        if isNull o then
                            Cancel.set env2
                            if Cancel.isSet env1 then cont None
                            else Option.map (Right >> Ok) a |> cont
                    )
                ) |> ignore
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run2 env2 (fun a ->
                        let o = Interlocked.CompareExchange(&o, a, null)
                        if isNull o then
                            Cancel.set env1
                            if Cancel.isSet env2 then cont None
                            else Option.map (Result.map Left) a |> cont
                    )
                ) |> ignore
        )
(*** hide ***)
    let timeout (milliseconds:int) (io:IO<'r,'a,'e>) : IO<'r,'a,'e option> =
        IO (fun env cont ->
            let (IO run) = race (Clock.sleep milliseconds) io
            run env (fun o ->
                if Cancel.isSet env then cont None
                else
                    match o with
                    | None -> None
                    | Some(Ok (Left a)) -> Ok a |> Some
                    | Some(Ok (Right ())) -> Error None |> Some
                    | Some(Error e) -> Error (Some e) |> Some
                    |> cont
            )
        )
    let toAsync (env:'r) (IO run) : Async<Result<'a,'e>> =
        Async.FromContinuations(fun (cont,_,_) ->
            run (env,Cancel.create()) (fun o ->
                cont o.Value
            )
        )

type IOBuilder() =
    member inline __.Bind(io:UIO<'r,'a>, f:'a->UIO<'r,'b>) : UIO<'r,'b> = io.Bind f
    member inline __.Bind(io:IO<'r,'a,'e>, f:'a->UIO<'r,'b>) : IO<'r,'b,'e> = io.Bind f
    member inline __.Bind(io:UIO<'r,'a>, f:'a->IO<'r,'b,'e>) : IO<'r,'b,'e> = io.Bind f
    member inline __.Bind(io:IO<'r,'a,'e>, f:'a->IO<'r,'b,'e>) = io.Bind<'b> f
    member inline __.Bind(io:IO<'r,'a,'e1>, f:'a->IO<'r,'b,'e2>) = io.Bind<'b,'e2> f
    member inline __.Return value = UIO.result value
    member inline __.ReturnFrom value = value

[<AutoOpen>]
module IOAutoOpen =
    let io = IOBuilder()

type ConsoleError = ConsoleError

type ConsoleService =
    abstract member WriteLine : string -> unit
    abstract member ReadLine : unit -> Result<string,ConsoleError>

type Console =
    abstract member Console : ConsoleService

module Console =
    let writeLine s = UIO.effect (fun (c:#Console) -> c.Console.WriteLine s)
    let readLine() = IO.effect (fun (c:#Console) -> c.Console.ReadLine())

type LoggingService =
    abstract member Log : string -> unit

type Logger =
    abstract member Logging : LoggingService

module Logger =
    let log s = UIO.effect (fun (l:#Logger) -> l.Logging.Log s)

type PersistError = PersistError

type PersistenceService =
    abstract member Persist : 'a -> Result<unit,PersistError>

type Persistence =
    abstract member Persistence : PersistenceService

module Persistence =
    let persist a = IO.effect (fun (p:#Persistence) -> p.Persistence.Persist a)
module Test =
(**
## Result

- error type is inferred and auto lifted into Either if needed
- Simple timeout and retry based on Result.Error

*)
    let programRetry noRetry : IO<'a,int,Either<ConsoleError,PersistError option>> =
        io {
            do! Logger.log "started"
            do! Console.writeLine "Please enter your name:"
            let! name = Console.readLine()
            do! Logger.log ("got name = " + name)
            let! thread =
                Persistence.persist name
                |> IO.timeout 1000
                |> IO.retry (Schedule.recurs noRetry)
                |> IO.fork
            do! Console.writeLine ("Hi "+name)
            do! thread
            do! Logger.log "finished"
            return 0
        }
(**

## Conclusion


## References

[ZIO Overview](https://scalaz.github.io/scalaz-zio/overview/)  
[ZIO Data Types](https://scalaz.github.io/scalaz-zio/datatypes/)  

## Thanks

[@jdegoes](https://twitter.com/jdegoes) - for ZIO and a great talk that made me want to do this.  
[@NinoFloris](https://twitter.com/NinoFloris) for async discussions.  
[@keithtpinson](https://twitter.com/keithtpinson/status/1104071022544932866) for the Either auto lift idea.  
*)