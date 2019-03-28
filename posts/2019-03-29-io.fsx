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

I recently went to a [talk](https://www.slideshare.net/jdegoes/the-death-of-final-tagless)
on [Scala ZIO](https://github.com/scalaz/scalaz-zio) by [John De Goes](https://twitter.com/jdegoes).
ZIO is a type-safe, composable library for asynchronous and concurrent programming in Scala.

It takes a different approach to other Scala effects libraries in that it does not require the use of Higher-Kinded Types.
Instead it uses a reader monad to provide access to IO effects (called ZIO Environment).

I came away wanting something similar in F#.
A useful library that could be used in the outer IO layer to simplify and test IO dependency code.
I started to play with some reader code but didn't think it would ultimately work out.

## IO
$$$
IO = Reader + Async + Result

The F# equivalent of ZIO type aliases are `UIO<'r,'a>` which represents effects without errors,
and `IO<'r,'a,'e>` which represents effects with a possible error.
[IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) combines reader, async and result into one unified computation expression.
*)
(*** hide ***)
type Result<'a,'e> =
    | Ok of 'a
    | Error of 'e

module Result =
    let map (f:'a->'b) (_:Result<'a,'e>) : Result<'b,'e> = failwith "hi"
    let mapError (f:'e->'f) (_:Result<'a,'e>) : Result<'a,'f> = failwith "hi"

type Time = Time
module Time =
    let now() = Time

module Choice =
    let merge (c:Choice<'a,'a>) =
        match c with
        | Choice1Of2 a -> a
        | Choice2Of2 a -> a

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
    member m.Bind<'b,'e2>(f:'a->IO<'r,'b,'e2>) : IO<'r,'b,Choice<'e,'e2>> =
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
                                let b = Option.map (Result.mapError Choice2Of2) o
                                if Cancel.isSet env then cont None
                                else cont b
                            )
                        | Some(Error e) -> cont (Some(Error (Choice1Of2 e)))
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
    let private retryOrElse (Schedule(initial, update))
                                  (orElse:'e *'s->IO<'r,'b,'e2>)
                                  (io:IO<'r,'a,'e>) : IO<'r,Choice<'a,'b>,'e2> =
        let rec loop (state:'s) : IO<'r,Choice<'a,'b>,'e2> =
            foldM
                (Choice1Of2 >> Ok >> result)
                (fun e ->
                    let u = update(e,state)
                    u.Bind (fun (Decision(cont,delay,state,_)) ->
                        if cont then
                            if delay = 0 then loop state
                            else Clock.sleep(delay).Bind(fun _ -> loop state)
                        else
                            orElse(e,state)
                            |> map Choice2Of2
                    )
                )
                io
        initial.Bind loop
    let retry (policy:Schedule<'r,'s,'e,'sb>) (io:IO<'r,'a,'e>) : IO<'r,'a,'e> =
        retryOrElse policy (fst >> Error >> result) io
        |> map Choice.merge
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

The reader part represents all the environment dependencies required in the computation expression.
It is fully type-safe with types inferred including any library requirements such as Clock for the timeout.
The computation expression can easily be [tested](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/IOTests.fs) by running with a test environment.

<img style="margin-left:20px" src="/{{site.baseurl}}public/io/programType.png" title="program type" />

## Async
At the IO layer thread pool threads need to be used in the most efficient way without any blocking.
This usually means Async in F# or async/await in C# need to be used.
They both join threads without a thread pool thread having to wait.
*)
    let race (UIO run1) (IO run2) : IO<'r,Choice<'a1,'a2>,'e1> =
        IO (fun env cont ->
            if Cancel.isSet env then cont None
            else
                let envChild = Cancel.add env
                let mutable o = 0
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run1 envChild (fun a ->
                        if Interlocked.Exchange(&o,1) = 0 then
                            Cancel.set envChild
                            if Cancel.isSet env then cont None
                            else Option.map (Choice2Of2 >> Ok) a |> cont
                    )
                ) |> ignore
                ThreadPool.QueueUserWorkItem (fun _ ->
                    run2 envChild (fun a ->
                        if Interlocked.Exchange(&o,1) = 0 then
                            Cancel.set envChild
                            if Cancel.isSet env then cont None
                            else Option.map (Result.map Choice1Of2) a |> cont
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
                    | Some(Ok (Choice1Of2 a)) -> Ok a |> Some
                    | Some(Ok (Choice2Of2 ())) -> Error None |> Some
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
(**
With [IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) async is implemented directly using the thread pool.
There are two main reasons for this.
In [IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) exceptions are not part of control flow.
Errors are first class and type-safe. Unrecoverable exceptions output the stack trace and exit the process.
Cancellation is fully integrated into [IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) meaning in race,
parallel and upon an error, computations are automatically cancelled saving resources.

These with the final part dramatically simplify and optimise asynchronous IO code.

## Result

The result part of [IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) represents possible errors in an integrated and type-safe way.
The error type is inferred, and different error types are auto lifted into `Choice<'a,'b>` when combined.
[IO](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs) computations can be timed out and retried based on result using simple functions.
Schedule is a powerful construct that can be combined several ways.
I've replicated the structure from ZIO but not fully explored its uses.

*)
let programRetry noRetry =
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

When type inference worked for the dependencies I was surprised.
When it was also possible to make it work for the errors I was amazed.

Computation expressions do not compose well.
At the IO layer a solution is needed for dependencies in a testable way.
The IO layer also needs to efficiently use the thread pool.
Making errors type-safe and integrated in the IO logic completes this compelling trinity.

## References

[IO.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion/IO.fs)  
[IOTests.fs](https://github.com/AnthonyLloyd/Fsion/blob/master/Fsion.Tests/IOTests.fs)  
[ZIO Overview](https://scalaz.github.io/scalaz-zio/overview/)  
[ZIO Data Types](https://scalaz.github.io/scalaz-zio/datatypes/)  
[The Death Of Final Tagless](https://www.slideshare.net/jdegoes/the-death-of-final-tagless)  

## Thanks

[@jdegoes](https://twitter.com/jdegoes) for ZIO and a great talk that made me want to do this.  
[@NinoFloris](https://twitter.com/NinoFloris) for useful async discussions.  
[@keithtpinson](https://twitter.com/keithtpinson/status/1104071022544932866) for the error auto lift idea.  
*)