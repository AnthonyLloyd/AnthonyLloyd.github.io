(**
\---
layout: post
title: "F# Implementation of The Elm Architecture"
tags: [Elm,UI]
description: ""
keywords: elm, ui
\---
*)

(*** hide ***)
namespace Main

type text (t:string) = class end  

module List =
    let remove n l =
        let rec pop n l p =
            if n=0 then p,l
            else
                match l with
                | [] -> p,[]
                | x::xs -> pop (n-1) xs (x::p) 
        pop n l [] 
    let rec add p l =
        match p with
        | [] -> l
        | x::xs -> add xs (x::l)
    let mapAt i mapping list =
        let removed,tail = remove i list
        add removed ((List.head tail |> mapping)::(List.tail tail))

(**
This is a prototype implementation of [The Elm Architecture](http://guide.elm-lang.org/architecture/index.html) in F#.
This post will cover the model and a follow up post will cover using it with WPF and Xamarin.

## Background

[The Elm Architecture](http://guide.elm-lang.org/architecture/index.html) is a simple pattern for creating functional user interfaces.
Due to its modularity and composability it makes UI code easier to write, understand, test and reuse.

Model of the UI and produce a list of changes. Interact with the UI as little as possible.

## UI types

Events...
Memoise...
*)
/// Message event used on the primative UI components.
type 'msg Event = ('msg->unit) ref ref

/// Layout for a section of UI components.
type Layout = Horizontal | Vertical

/// Primative UI components.
type UI =
    | Text of text
    | Input of text * text Event
    | Button of text * unit Event
    | Div of Layout * UI list

/// UI component update and event redirection.
type UIUpdate =
    | InsertUI of int list * UI
    | UpdateUI of int list * UI
    | ReplaceUI of int list * UI
    | RemoveUI of int list
    | EventUI of (unit->unit)

/// UI component including a message event.
type 'msg UI = {UI:UI;mutable Event:'msg->unit}

/// UI application.
type App<'msg,'model> = {Model:'model;Update:'msg->'model->'model;View:'model->'msg UI}

/// Native UI interface.
type INativeUI =
    abstract member Send : UIUpdate list -> unit
(**
## UI module

Some ...

*)
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UI =
    /// Memoize view generation from model object references.
    let memoize<'model ,'msg  when 'model : not struct and 'msg : not struct> =
        let d = System.Runtime.CompilerServices.ConditionalWeakTable<'model,'msg UI>()
        fun f k ->
            match d.TryGetValue k with
            |true,v -> v
            |false,_ ->
                let v = f k
                d.Add(k,v)
                v

    /// Returns a Text display UI component.
    let text text = {UI=Text text;Event=ignore}
    
    /// Returns a Text input UI component.
    let input text = 
        let ev = ref ignore |> ref
        let ui = {UI=Input(text,ev);Event=ignore}
        let raise a = ui.Event a
        (!ev):=raise
        ui

    /// Returns a Button UI component.
    let button text a =
        let ev = ref ignore |> ref
        let ui = {UI=Button(text,ev);Event=ignore}
        (!ev):=fun () -> ui.Event a
        ui

    /// Returns a section of UI components given a layout.
    /// The name div comes from HTML and represents a division (or section) of UI components.
    let div layout list =
        let ui = {UI=Div(layout,List.map (fun ui -> ui.UI) list);Event=ignore}
        let raise a = ui.Event a
        List.iter (fun i -> i.Event<-raise) list
        ui
    
    /// Returns a new UI component mapping the message event using the given function.
    let rec map f ui =
        let ui2 = {UI=ui.UI;Event=ignore}
        let raise e = f e |> ui2.Event
        ui.Event<-raise
        ui2

    /// Returns a list of UI updates from two UI components.
    let diff ui1 ui2 =
        let inline update e1 e2 = fun () -> let ev = !e1 in ev:=!(!e2); e2:=ev
        let rec diff ui1 ui2 path index diffs =
            match ui1,ui2 with
            | _,_ when obj.ReferenceEquals(ui1,ui2) -> diffs
            |Text t1,Text t2 -> if t1=t2 then diffs else UpdateUI(path,ui2)::diffs
            |Button (t1,e1),Button (t2,e2) -> if t1=t2 then EventUI(update e1 e2)::diffs else EventUI(update e1 e2)::UpdateUI(path,ui2)::diffs
            |Input (t1,e1),Input (t2,e2) -> if t1=t2 then EventUI(update e1 e2)::diffs else EventUI(update e1 e2)::UpdateUI(path,ui2)::diffs
            |Button _,Button _ |Input _,Input _ -> UpdateUI(path,ui2)::diffs
            |Div (l1,_),Div (l2,_) when l1<>l2 -> ReplaceUI(index::path,ui2)::diffs
            |Div (_,[]),Div (_,[]) -> diffs
            |Div (_,[]),Div (_,l) -> List.fold (fun (i,diffs) ui -> i+1,InsertUI(i::path,ui)::diffs) (index,diffs) l |> snd
            |Div (_,l),Div (_,[]) -> List.fold (fun (i,diffs) _ -> i+1,RemoveUI(i::path)::diffs) (index,diffs) l |> snd
            |Div (l,(h1::t1)),Div (_,(h2::t2)) when obj.ReferenceEquals(h1,h2) -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) diffs
            |Div (l,(h1::t1)),Div (_,(h2::h3::t2)) when obj.ReferenceEquals(h1,h3) -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) (InsertUI(index::path,h2)::diffs)
            |Div (l,(_::h2::t1)),Div (_,(h3::t2)) when obj.ReferenceEquals(h2,h3) -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) (RemoveUI(index::path)::diffs)
            |Div (l,(h1::t1)),Div (_,(h2::t2)) -> diff h1 h2 (index::path) 0 diffs |> diff (Div(l,t1)) (Div(l,t2)) path (index+1)
            |_,_ -> ReplaceUI(index::path,ui2)::diffs
        diff ui1.UI ui2.UI [] 0 []

    /// Returns a UI application from a UI model, update and view.
    let app model update view = {Model=model;Update=update;View=view}

    /// Runs a UI application given a native UI.
    let run (nativeUI:INativeUI) app =
        let rec handle model ui msg =
            let newModel = app.Update msg model
            let newUI = app.View newModel
            newUI.Event<-handle newModel newUI
            diff ui newUI |> nativeUI.Send
        let ui = app.View app.Model
        ui.Event<-handle app.Model ui
        [InsertUI([],ui.UI)] |> nativeUI.Send
(**
## Example UI applications

The pattern has three parts:

1. **Model** - the state of the application.
2. **Update** - a way to update the state.
3. **View** - a way to view the state as a UI.
*)
module Counter =
    type Msg = Increment | Decrement

    type Model = int

    let init i : Model = i

    let update msg model : Model =
        match msg with
        |Increment -> model+1
        |Decrement -> model-1

    let view (model:Model) :Msg UI =
        UI.div Horizontal [
            UI.button (text "+") Increment
            UI.button (text "-") Decrement
            UI.text (string model |> text)
        ]

    let app i =
        UI.app (init i) update view

module CounterPair =
    type Msg =
        |Reset
        |Top of Counter.Msg
        |Bottom of Counter.Msg

    type Model = {Top:Counter.Model;Bottom:Counter.Model}

    let init top bottom = {Top=Counter.init top;Bottom=Counter.init bottom}

    let update msg model =
        match msg with
        |Reset -> init 0 0
        |Top msg -> {model with Top=Counter.update msg model.Top}
        |Bottom msg -> {model with Bottom=Counter.update msg model.Bottom}

    let view (model:Model) =
        UI.div Vertical [
            Counter.view model.Top |> UI.map Top
            Counter.view model.Bottom |> UI.map Bottom
        ]

    let app top bottom =
        UI.app (init top bottom) update view

module CounterList =
    type Msg =
        |Insert
        |Remove
        |Modify of int * Counter.Msg

    type Model = {Counters:Counter.Model list}

    let init = {Counters=[]}

    let update msg model =
        match msg with
        |Insert -> {model with Counters=Counter.init 0::model.Counters}
        |Remove -> {model with Counters=Counter.init 0::model.Counters}
        |Modify (i,msg) -> {model with Counters=List.mapAt i (Counter.update msg) model.Counters}

    let view (model:Model) =
        List.mapi (fun i c -> Counter.view c |> UI.map (fun v -> Modify(i,v))) model.Counters |> UI.div Vertical

    let app =
        UI.app init update view
(**
## Conclusion

The ...
*)