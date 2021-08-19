module App

type State = list<int>

type Action =
    | Enqueue of int
    | Dequeue

let nextState (action: Action) (state: State) =
    match action with
    | Enqueue x -> List.append state [x]
    | Dequeue -> state.Tail

open Elmish
open Elmish.React
open Fable.React.Props
open Fable.React.Helpers
open Fable.React.Standard

let init () = []

let rnd = System.Random()

let view (state: State) (dispatch: Action -> unit) =
    div [] [
        button [ OnClick (fun _ -> rnd.Next(100) |> Enqueue |> dispatch) ] [ str "Enqueue" ]
        state |> List.rev |> List.map string |> String.concat " - " |> str
        if not state.IsEmpty then button [ OnClick (fun _ -> Dequeue |> dispatch) ] [ str "Dequeue" ]
    ]

Program.mkSimple init nextState view
|> Program.withReactSynchronous "elmish-app"
|> Program.run

