(**
\---
layout: post
title: "DAG - An Immutable Spreadsheet Data Structure"
tags: [dag,immutable,spreadsheet,cell]
description: "DAG - An Immutable Spreadsheet Data Structure"
keywords: f#, dag, immutable, spreadsheet, cell
\---
*)

(*** hide ***)
namespace DagBlog

open System
open System.Threading.Tasks
open Expecto

(**

In finance data grids can be be defined as a set of input fields and function fields that take other field values as parameters.
Spreadsheets are often used to do this, but they have a number of [limitations](https://www.cio.com/article/2438188/enterprise-software/eight-of-the-worst-spreadsheet-blunders.html).

Recently I've been working on ways of describing calculations, so they can just as easily be viewed in a desktop application, web report or spreadsheet.

One of the components required to do this is a functional calculation graph much like how a spreadsheet works.
This blog aims to construct a functional [directed acyclic graph (DAG)](https://en.wikipedia.org/wiki/Directed_acyclic_graph) calculation data structure.

## DAG code

We don't have to work very hard to ensure the graph is not circular or keep the cells in topological order.
The API can be designed such that it is only possible to add function cells when the parameter cells already exist in the DAG.
All tasks can be performed with a single pass of the cells in the order they were added.

The DAG data structure is made immutable by cloning any internal arrays when they need to be changed.
Grids can keep the old version of the calculations or compare and switch to the new version when needed.

The DAG is fully type safe by use of an applicative functor builder in constructing function cells.

Cells are evaluated as `lazy` `Task` so calulations run in the most parallel way possible.
Calculations are run only once, and results are reused even after further updates to the DAG.

*)

type Dag = private {
    InputKeys : obj array
    InputValues : obj array
    FunctionKeys : obj array
    FunctionInputs : (Set<int> * Set<int>) array
    FunctionFunctions : (Dag -> Task<obj>) array
    FunctionValues : Lazy<Task<obj>> array
}

module Dag =
    let private append a v =
        let mutable a = a
        Array.Resize(&a, Array.length a + 1)
        a.[Array.length a - 1] <- v
        a

    let inline private taskMap f (t:Task<_>) =
        t.ContinueWith(fun (r:Task<_>) -> f r.Result)

    type Input = private | CellInput
    type Function = private | CellFunction

    type Cell<'a,'b> = private | Cell of obj

    let empty = {
        InputKeys = [||]
        InputValues = [||]
        FunctionKeys = [||]
        FunctionInputs = [||]
        FunctionFunctions = [||]
        FunctionValues = [||]
    }

    let addInput (v:'a) (d:Dag) : Dag * Cell<'a, Input> =
        let key = obj()
        { d with
            InputKeys = append d.InputKeys key
            InputValues = box v |> append d.InputValues
        }, Cell key

    let getValue (Cell key:Cell<'a,Input>) (d:Dag) : 'a =
        let i = Array.findIndex ((=)key) d.InputKeys
        downcast d.InputValues.[i]

    let setInput (Cell key:Cell<'a,Input>) (a:'a) (d:Dag) : Dag =
        let i = Array.findIndex ((=)key) d.InputKeys
        if downcast d.InputValues.[i] = a then d
        else
            let dirtyCalcs =
                Seq.fold (fun (j,s) (inputs,calcInputs) ->
                    if Set.contains i inputs ||
                       Set.intersect s calcInputs |> Set.isEmpty |> not then
                        j+1, Set.add j s
                    else
                        j+1, s
                ) (0,Set.empty) d.FunctionInputs
                |> snd

            let inputValues = Array.copy d.InputValues
            inputValues.[i] <- box a

            if Set.isEmpty dirtyCalcs then { d with InputValues = inputValues }
            else
                let functionValues = Array.copy d.FunctionValues
                let dag = {
                    d with
                        InputValues = inputValues
                        FunctionValues = functionValues
                }
                Set.iter (fun i ->
                    functionValues.[i] <- lazy d.FunctionFunctions.[i] dag
                ) dirtyCalcs
                dag

    let getValueTask (Cell key:Cell<'a,Function>) (d:Dag) : Task<'a> =
        let i = Array.findIndex ((=)key) d.FunctionKeys
        d.FunctionValues.[i].Value |> taskMap (fun o -> downcast o)

    let changed (Cell key:Cell<'a,'t>) (before:Dag) (after:Dag) : bool =
        if typeof<'t> = typeof<Function> then
            let i = Array.findIndex ((=)key) before.FunctionKeys
            before.FunctionValues.[i] <> after.FunctionValues.[i]
        else
            let i = Array.findIndex ((=)key) before.InputKeys
            downcast before.InputValues.[i] <> downcast after.InputValues.[i]

    type 'a Builder = private {
        Dag : Dag
        Inputs : Set<int> * Set<int>
        Function : Dag -> Task<'a>
    }

    let buildFunction (d:Dag) f = {
        Dag = d
        Inputs = Set.empty, Set.empty
        Function = fun _ -> Task.FromResult f
    }

    let applyCell (Cell key:Cell<'a,'t>) {Dag=dag;Inputs=inI,inC;Function=bFn} =
        let isFunctionCell = typeof<'t> = typeof<Function>
        let i =
            if isFunctionCell then dag.FunctionKeys else dag.InputKeys
            |> Array.findIndex ((=)key)
        {
            Dag = dag
            Inputs =
                if isFunctionCell then inI, Set.add i inC
                              else Set.add i inI, inC
            Function =
                if isFunctionCell then
                    fun d ->
                        let fTask = bFn d
                        ( d.FunctionValues.[i].Value |> taskMap (fun o ->
                            taskMap (fun f -> downcast o |> f) fTask  )
                        ).Unwrap()
                else
                    fun d ->
                        bFn d |> taskMap (fun f ->
                            downcast d.InputValues.[i] |> f  )
        }

    let addFunction ({Dag=dag;Inputs=ips;Function=fn}:'a Builder) =
        let key = obj()
        let calc = fn >> taskMap box
        let d = {
            dag with
                FunctionKeys = append dag.FunctionKeys key
                FunctionInputs = append dag.FunctionInputs ips
                FunctionFunctions = append dag.FunctionFunctions calc
                FunctionValues = append dag.FunctionValues null
        }
        d.FunctionValues.[d.FunctionValues.Length-1] <- lazy calc d
        let cell : Cell<'a,Function> = Cell key
        d, cell
(**
*)
(*** hide ***)
module Gen =
    let x = 1
(**
## Testing

The following tests demonstrate use of the DAG.
*)
let tests =
    testList "dag tests" [

        testAsync "one cell" {
            let dag, cell1 = Dag.addInput 7 Dag.empty
            Expect.equal 7 (Dag.getValue cell1 dag) "one cell"
        }

        testAsync "two cell" {
            let dag, cell1 = Dag.addInput 8 Dag.empty
            let dag, cell2 = Dag.addInput 9 dag
            Expect.equal 8 (Dag.getValue cell1 dag) "first 8"
            Expect.equal 9 (Dag.getValue cell2 dag) "second 9"
        }

        testAsync "one function" {
            let dag, cell1 = Dag.addInput 42 Dag.empty
            let dag, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let! result = Dag.getValueTask cell2 dag |> Async.AwaitTask
            Expect.equal 420 result "42 * 10 = 420"
        }

        testAsync "one function with set" {
            let dag, cell1 = Dag.addInput 13 Dag.empty
            let dag, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let dag = Dag.setInput cell1 43 dag
            let! result = Dag.getValueTask cell2 dag |> Async.AwaitTask
            Expect.equal 430 result "43 * 10 = 430"
        }

        testAsync "one function with set twice" {
            let dag, cell1 = Dag.addInput 15 Dag.empty
            let dag, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let dag = Dag.setInput cell1 43 dag
            let dag = Dag.setInput cell1 44 dag
            let! result = Dag.getValueTask cell2 dag |> Async.AwaitTask
            Expect.equal 440 result "44 * 10 = 440"
        }

        testAsync "not changed input" {
            let dagBefore, cell1 = Dag.addInput 42 Dag.empty
            let dagAfter,_ = Dag.addInput 45 dagBefore
            Expect.isFalse (Dag.changed cell1 dagBefore dagAfter) "no change"
        }

        testAsync "changed input" {
            let dagBefore, cell1 = Dag.addInput 42 Dag.empty
            let dagAfter = Dag.setInput cell1 45 dagBefore
            Expect.isTrue (Dag.changed cell1 dagBefore dagAfter) "changed"
        }

        testAsync "not changed function" {
            let dag, cell1 = Dag.addInput 42 Dag.empty
            let dagBefore, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let dagAfter,_ = Dag.addInput 45 dagBefore
            Expect.isFalse (Dag.changed cell2 dagBefore dagAfter) "no change"
        }

        testAsync "changed function" {
            let dag, cell1 = Dag.addInput 17 Dag.empty
            let dagBefore, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let dagAfter = Dag.setInput cell1 45 dagBefore
            Expect.isTrue (Dag.changed cell2 dagBefore dagAfter) "changed"
            let! result = Dag.getValueTask cell2 dagAfter |> Async.AwaitTask
            Expect.equal 450 result "45 * 10 = 450"
            let! result = Dag.getValueTask cell2 dagBefore |> Async.AwaitTask
            Expect.equal 170 result "17 * 10 = 170"
        }

        testAsync "chained functions" {
            let dag, cell1 = Dag.addInput 18 Dag.empty
            let dag, cell2 =
                Dag.buildFunction dag (fun x -> x * 10)
                |> Dag.applyCell cell1
                |> Dag.addFunction
            let dagBefore, cell3 =
                Dag.buildFunction dag (fun x -> x + 1)
                |> Dag.applyCell cell2
                |> Dag.addFunction
            let dagAfter = Dag.setInput cell1 23 dagBefore
            let! result = Dag.getValueTask cell3 dagAfter |> Async.AwaitTask
            Expect.equal 231 result "231"
            let! result = Dag.getValueTask cell3 dagBefore |> Async.AwaitTask
            Expect.equal 181 result "181"
        }

        testAsync "two function" {
            let dag, cell1 = Dag.addInput "z" Dag.empty
            let dag, cell2 = Dag.addInput 7 dag
            let dag, cell3 =
                Dag.buildFunction dag (fun s (n:int) -> s + string n)
                |> Dag.applyCell cell1
                |> Dag.applyCell cell2
                |> Dag.addFunction
            let! result = Dag.getValueTask cell3 dag |> Async.AwaitTask
            Expect.equal "z7" result "z7"
        }

        testAsync "three function with set" {
            let dag, cell1 = Dag.addInput "f" Dag.empty
            let dag, cell2 = Dag.addInput 8 dag
            let dag, cell3 = Dag.addInput 1.5 dag
            let dag, cell4 =
                Dag.buildFunction dag
                    (fun s (n:int) (f:float) -> s + string n + "-" + string f)
                |> Dag.applyCell cell1
                |> Dag.applyCell cell2
                |> Dag.applyCell cell3
                |> Dag.addFunction
            let! result = Dag.getValueTask cell4 dag |> Async.AwaitTask
            Expect.equal "f8-1.5" result "f8-1.5"
            let dag = Dag.setInput cell1 "w" dag
            let! result = Dag.getValueTask cell4 dag |> Async.AwaitTask
            Expect.equal "w8-1.5" result "w8-1.5"
        }
        
        testAsync "chained functions multi" {
            let dag, cell1 = Dag.addInput "a" Dag.empty
            let dag, cell2 = Dag.addInput 1 dag
            let dag, cell3 =
                Dag.buildFunction dag (fun s (n:int) -> "x:" + s + string n)
                |> Dag.applyCell cell1
                |> Dag.applyCell cell2
                |> Dag.addFunction
            let dag, cell4 = Dag.addInput "b" dag
            let dag, cell5 = Dag.addInput 2 dag
            let dag, cell6 =
                Dag.buildFunction dag (fun s (n:int) -> "y:" + s + string n)
                |> Dag.applyCell cell4
                |> Dag.applyCell cell5
                |> Dag.addFunction
            let dag, cell7 = Dag.addInput "c" dag
            let dag, cell8 = Dag.addInput 3 dag
            let dag, cell9 =
                Dag.buildFunction dag (fun s (n:int) -> "z:" + s + string n)
                |> Dag.applyCell cell7
                |> Dag.applyCell cell8
                |> Dag.addFunction
            let dagBefore, cell10 =
                Dag.buildFunction dag
                    (fun s1 s2 s3 -> String.concat "|" [s1;s2;s3])
                |> Dag.applyCell cell3
                |> Dag.applyCell cell6
                |> Dag.applyCell cell9
                |> Dag.addFunction
            let dagAfter = Dag.setInput cell5 4 dagBefore
            let! result = Dag.getValueTask cell10 dagAfter |> Async.AwaitTask
            Expect.equal "x:a1|y:b4|z:c3" result "x:a1|y:b4|z:c3"
            let! result = Dag.getValueTask cell10 dagBefore |> Async.AwaitTask
            Expect.equal "x:a1|y:b2|z:c3" result "x:a1|y:b2|z:c3"
        }
    ]

(**
## Conclusion

This has been a very successful experiment.
The DAG has some nice features as well as keeping type safety.

The way the immutability has been implemented means it is probably not best suited to fast realtime updates or very fine-grained calculations.
For more coarse-grained calculations like grids of dependent fields, where each cell represents a column of values and summaries, I think it could be ideal.

*)