(**
\---
layout: post
title: "F# Implementation of The Elm Architecture - Part 2"
tags: [Elm,UI,GUI,WPF,Xamarin]
description: ""
keywords: elm, ui, gui, WPF, Xamarin
\---
*)

(*** hide ***)
#r "PresentationFramework.dll"
#r "PresentationCore.dll"
#r "WindowsBase.dll"

namespace Main

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
    let removei i l =
        let p,t = remove i l
        List.tail t |> add p
    let replacei i item l =
        let p,t = remove i l
        item::List.tail t |> add p
    let revMap mapping list =
        let rec work input output = match input with | [] -> output | x::xs -> work xs (mapping x::output)
        work list []
    let mapAt i mapping list =
        let removed,tail = remove i list
        add removed ((List.head tail |> mapping)::(List.tail tail))

/// Message event used on the primative UI components.
type 'msg Event = ('msg->unit) ref ref

/// Layout for a section of UI components.
type Layout = Horizontal | Vertical

/// Primative UI components.
type UI =
    | Text of string
    | Input of string * string Event
    | Button of string * unit Event
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

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UI =
    /// Memoize view generation from model object references.
    let memoize<'model ,'msg  when 'model : not struct and 'msg : not struct> =
        let d = System.Runtime.CompilerServices.ConditionalWeakTable<'model,'msg UI>()
        fun view model ->
            match d.TryGetValue model with
            |true,ui -> ui
            |false,_ ->
                let ui = view model
                d.Add(model,ui)
                ui

    /// Returns a Text display UI component.
    let text text = {UI=Text text;Event=ignore}
    
    /// Returns a text Input UI component.
    let input text = 
        let ev = ref ignore |> ref
        let ui = {UI=Input(text,ev);Event=ignore}
        let raise a = ui.Event a
        (!ev):=raise
        ui

    /// Returns a Button UI component.
    let button text msg =
        let ev = ref ignore |> ref
        let ui = {UI=Button(text,ev);Event=ignore}
        (!ev):=fun () -> ui.Event msg
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
            | _,_ when LanguagePrimitives.PhysicalEquality ui1 ui2 -> diffs
            |Text t1,Text t2 -> if t1=t2 then diffs else UpdateUI(path,ui2)::diffs
            |Button (t1,e1),Button (t2,e2) -> if t1=t2 then EventUI(update e1 e2)::diffs else EventUI(update e1 e2)::UpdateUI(path,ui2)::diffs
            |Input (t1,e1),Input (t2,e2) -> if t1=t2 then EventUI(update e1 e2)::diffs else EventUI(update e1 e2)::UpdateUI(path,ui2)::diffs
            |Button _,Button _ |Input _,Input _ -> UpdateUI(path,ui2)::diffs
            |Div (l1,_),Div (l2,_) when l1<>l2 -> ReplaceUI(index::path,ui2)::diffs
            |Div (_,[]),Div (_,[]) -> diffs
            |Div (_,[]),Div (_,l) -> List.fold (fun (i,diffs) ui -> i+1,InsertUI(i::path,ui)::diffs) (index,diffs) l |> snd
            |Div (_,l),Div (_,[]) -> List.fold (fun (i,diffs) _ -> i+1,RemoveUI(i::path)::diffs) (index,diffs) l |> snd
            |Div (l,(h1::t1)),Div (_,(h2::t2)) when LanguagePrimitives.PhysicalEquality h1 h2 -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) diffs
            |Div (l,(h1::t1)),Div (_,(h2::h3::t2)) when LanguagePrimitives.PhysicalEquality h1 h3 -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) (InsertUI(index::path,h2)::diffs)
            |Div (l,(_::h2::t1)),Div (_,(h3::t2)) when LanguagePrimitives.PhysicalEquality h2 h3 -> diff (Div(l,t1)) (Div(l,t2)) path (index+1) (RemoveUI(index::path)::diffs)
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
            let diff = diff ui newUI
            List.iter (function |EventUI f -> f() |_-> ()) diff
            nativeUI.Send diff
        let ui = app.View app.Model
        ui.Event<-handle app.Model ui
        nativeUI.Send [InsertUI([],ui.UI)]

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
            UI.button "+" Increment
            UI.button "-" Decrement
            UI.text (string model)
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
        |Remove -> {model with Counters=match model.Counters with |[]->[]|_::xs->xs}
        |Modify (i,msg) -> {model with Counters=List.mapAt i (Counter.update msg) model.Counters}

    let view (model:Model) =
        UI.button "Add" Insert ::
        UI.button "Remove" Remove ::
        List.mapi (fun i c -> Counter.view c |> UI.map (fun v -> Modify(i,v)))
            model.Counters
        |> UI.div Vertical

    let app =
        UI.app init update view

open System.Windows
open System.Windows.Controls
(**
This is the second part of a prototype of [The Elm Architecture](http://guide.elm-lang.org/architecture/index.html) in F#.
The first [post]({% post_url 2016-06-20-fsharp-elm-part1 %}) covered the logical UI and this covers using it with WPF and Xamarin. 

## Native UI Implementation

Below is the WPF implementation of the `INativeUI` interface.
The Xamarin [implementation](https://github.com/AnthonyLloyd/Elm/blob/master/XamarinApp/Xamarin.fs) is very similar.

Threading proves to be simple with all the updates being performed on the UI thread in a single call.
`UIUpdate` maps well to the operations required to locate and update the native UI elements.
*)
module WPF =
    let CreateNaiveUI (root:ContentControl) =
        
        let rec createUI ui : UIElement =
            match ui with
            |Text text ->
                let c = Label(Content=string text)
                upcast c
            |Input (text,event) ->
                let c = TextBox(Text=string text)
                let event = !event
                c.TextChanged.Add(fun _ -> let t = c.Text
                                           async { !event t } |> Async.Start)
                upcast c
            |Button (text,event) ->
                let c = Button(Content=string text)
                let event = !event
                c.Click.Add(fun _ -> async { (!event)() } |> Async.Start)
                upcast c
            |Div (layout,list) ->
                let children = List.map createUI list
                let c = StackPanel(Orientation=
                                    match layout with 
                                    |Vertical->Orientation.Vertical
                                    |Horizontal->Orientation.Horizontal)
                List.iter (c.Children.Add>>ignore) children
                upcast c

        let rec locatePanel loc : Panel =
            match loc with
            |[] -> root.Content :?> _
            |i::xs -> (locatePanel xs).Children.Item i :?> _

        let uiUpdate u =
            match u with
            | InsertUI (loc,ui) ->
                match loc with
                |[] -> root.Content <- createUI ui
                |i::xs -> (locatePanel xs).Children.Insert(i,createUI ui)
            | UpdateUI (loc,ui) ->
                let element = match loc with
                              |[] -> root.Content :?> _
                              |i::xs -> (locatePanel xs).Children.Item i
                match ui with
                | Text text -> (element :?> Label).Content <- string text
                | Input (text,_) -> (element :?> TextBox).Text <- string text
                | Button (text,_) -> (element :?> Button).Content <- string text
                | Div _ -> ()
            | ReplaceUI (loc,ui) ->
                match loc with
                |[] -> root.Content <- createUI ui
                |i::xs ->
                    let c = (locatePanel xs).Children
                    c.RemoveAt i
                    c.Insert(i,createUI ui)
            | RemoveUI loc ->
                match loc with
                |[] -> ()
                |i::xs -> (locatePanel xs).Children.RemoveAt i
            | EventUI _ -> ()

        { new INativeUI with
            member __.Send list =
                root.Dispatcher.Invoke (fun () -> List.iter uiUpdate list)
        }
(**
## Results

The same `CounterList` UI application from the previous [post]({% post_url 2016-06-20-fsharp-elm-part1 %}) has been used across a number of native UIs.
The example [source](https://github.com/AnthonyLloyd/Elm) produces the following mobile and desktop UIs. 

<table style="border:0px">
	<tr>
		<td style="background-color:white;border:0px"><img src="/{{site.baseurl}}public/elm/WinPhone80.png" title="Mobile Application" width="340px" height="500px"/></td>
		<td style="background-color:white;border:0px"><img src="/{{site.baseurl}}public/elm/WinWPF.png" title="WPF Application" width="340px" height="500px"/></td>
	</tr>
	<tr>
		<td style="background-color:white;border:0px"><img src="/{{site.baseurl}}public/elm/WinApp.png" title="Metro Application" width="340px" height="500px"/></td>
		<td style="background-color:white;border:0px"><img src="/{{site.baseurl}}public/elm/WinUWP.png" title="UWP Application" width="340px" height="500px"/></td>
	</tr>
</table>

## Conclusion

[The Elm Architecture](http://guide.elm-lang.org/architecture/index.html) continues to look to be a very promising pattern.

The `INativeUI` implementation is a single place for native UI element creation and is much more DRY than other UI models.
So far, styling has not been considered, but this single place should make it easier with both an Elm and a CSS model being possible.

[The Elm Architecture](http://guide.elm-lang.org/architecture/index.html) moves the view and event logic away from the native UI.
This has the benefit of making the UI more testable and at the same time making migration of the native UI easier.

These benefits, combined with the type safety and composability outlined in the previous [post]({% post_url 2016-06-20-fsharp-elm-part1 %}), make this pattern compelling.  

UPDATED:  

<img style="border:1px solid black" src="/{{site.baseurl}}public/elm/donpraise.png"/>
*)